using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class ConfigurableBulkProcessor : IBulkProcessor
{
    private const int DefaultBatchSize = 100;
    private const int DefaultMaxParallelism = 8;

    private readonly IdentityDbContext _db;
    private readonly IServiceResolver _serviceResolver;
    private readonly IValidationService _validationService;
    private readonly IExternalBulkSyncService _externalSyncService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILookupResolverService _lookupResolverService;
    private readonly IUniqueFieldValidator _uniqueFieldValidator;
    private readonly IReadOnlyCollection<IBulkCustomValidator> _customValidators;
    private readonly ILogger<ConfigurableBulkProcessor> _logger;

    public ConfigurableBulkProcessor(
        IdentityDbContext db,
        IServiceResolver serviceResolver,
        IValidationService validationService,
        IExternalBulkSyncService externalSyncService,
        ICurrentUserService currentUser,
        ILookupResolverService lookupResolverService,
        IUniqueFieldValidator uniqueFieldValidator,
        IEnumerable<IBulkCustomValidator> customValidators,
        ILogger<ConfigurableBulkProcessor> logger)
    {
        _db = db;
        _serviceResolver = serviceResolver;
        _validationService = validationService;
        _externalSyncService = externalSyncService;
        _currentUser = currentUser;
        _lookupResolverService = lookupResolverService;
        _uniqueFieldValidator = uniqueFieldValidator;
        _customValidators = customValidators.ToList();
        _logger = logger;
    }

    public async Task ProcessAsync(BulkUploadWorkItem workItem, CancellationToken ct = default)
    {
        var job = await _db.bulk_jobs.FirstOrDefaultAsync(x => x.Id == workItem.JobId, ct);
        if (job == null)
            return;

        job.Status = "PROCESSING";
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk job started. JobId={JobId}, Module={Module}, Rows={Rows}", workItem.JobId, workItem.ModuleKey, workItem.Rows.Count);

        var config = await _db.BulkUploadConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ModuleKey == workItem.ModuleKey && x.IsActive, ct);

        if (config == null)
            throw new InvalidOperationException($"Config not found for module '{workItem.ModuleKey}'.");

        var columns = BulkUploadColumnDefinitionParser.Parse(config.ColumnsJson);
        var dtoType = _serviceResolver.ResolveDtoType(config.DtoName);
        ValidateColumnConfiguration(dtoType, columns, workItem.ModuleKey);
        var service = _serviceResolver.ResolveService(config.ServiceInterface);
        var method = ResolveMethod(service.GetType(), config.ServiceMethod, dtoType);
        var customValidator = _customValidators.FirstOrDefault(x => string.Equals(x.ModuleKey, workItem.ModuleKey, StringComparison.OrdinalIgnoreCase));

        await _lookupResolverService.PreloadAsync(
            columns
                .Select(x => x.LookupType)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>(),
            ct);

        var candidates = new ConcurrentBag<RowCandidate>();
        var errors = new ConcurrentBag<BulkRowErrorDto>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(),
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, workItem.Rows.Count),
            parallelOptions,
            async (i, token) =>
            {
                var rowNumber = i + 2;
                var rowData = workItem.Rows[i];

                try
                {
                    var dto = await MapRowToDtoAsync(dtoType, rowData, columns, token);
                    var rowErrors = _validationService.ValidateObject(dto);

                    if (customValidator != null)
                        rowErrors.AddRange(await customValidator.ValidateAsync(rowData, dto, token));

                    if (rowErrors.Count > 0)
                    {
                        errors.Add(new BulkRowErrorDto
                        {
                            RowNumber = rowNumber,
                            Message = string.Join("; ", rowErrors.Distinct(StringComparer.OrdinalIgnoreCase))
                        });
                        return;
                    }

                    candidates.Add(new RowCandidate(rowNumber, dto));
                }
                catch (Exception ex)
                {
                    errors.Add(new BulkRowErrorDto
                    {
                        RowNumber = rowNumber,
                        Message = ex.Message
                    });
                }
            });

        var sortedCandidates = candidates.OrderBy(x => x.RowNumber).ToList();

        ApplyInFileDuplicateValidation(sortedCandidates, columns);
        await ApplyDatabaseUniquenessValidationAsync(workItem.ModuleKey, sortedCandidates, columns, ct);

        var validItems = new List<RowCandidate>();
        foreach (var candidate in sortedCandidates)
        {
            if (candidate.Errors.Count == 0)
            {
                validItems.Add(candidate);
                continue;
            }

            errors.Add(new BulkRowErrorDto
            {
                RowNumber = candidate.RowNumber,
                Message = string.Join("; ", candidate.Errors.Distinct(StringComparer.OrdinalIgnoreCase))
            });
        }

        int success = 0;
        int failed = errors.Count;

        foreach (var batch in Chunk(validItems, DefaultBatchSize))
        {
            ct.ThrowIfCancellationRequested();
            var dtoBatch = batch.Select(x => x.Dto).ToList();

            try
            {
                await InvokeConfiguredMethodAsync(service, method, dtoType, dtoBatch, ct);
                success += dtoBatch.Count;

                if (config.ExternalSync)
                    await _externalSyncService.SyncAsync(workItem.ModuleKey, dtoBatch, ct);
            }
            catch (Exception ex)
            {
                foreach (var item in batch)
                {
                    failed++;
                    errors.Add(new BulkRowErrorDto
                    {
                        RowNumber = item.RowNumber,
                        Message = ex.Message
                    });
                }
            }
        }

        job.ProcessedRows = success + failed;
        job.SuccessRows = success;
        job.FailedRows = failed;
        job.Status = failed > 0 ? "COMPLETED_WITH_ERRORS" : "COMPLETED";
        job.CompletedAt = DateTime.UtcNow;

        if (errors.Count > 0)
            job.ErrorFilePath = await GenerateErrorReportAsync(workItem.JobId, errors.OrderBy(x => x.RowNumber).ToList(), ct);

        await SaveErrorRowsAsync(workItem, errors, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk job completed. JobId={JobId}, Status={Status}, Success={Success}, Failed={Failed}", workItem.JobId, job.Status, success, failed);
    }

    private static MethodInfo ResolveMethod(Type serviceType, string methodName, Type dtoType)
    {
        var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name == methodName)
            .ToList();

        var listMethod = methods.FirstOrDefault(m =>
        {
            var p = m.GetParameters();
            if (p.Length != 1) return false;
            if (!p[0].ParameterType.IsGenericType) return false;
            return p[0].ParameterType.GetGenericTypeDefinition() == typeof(List<>) &&
                   p[0].ParameterType.GetGenericArguments()[0] == dtoType;
        });

        if (listMethod != null)
            return listMethod;

        var singleMethod = methods.FirstOrDefault(m =>
        {
            var p = m.GetParameters();
            return p.Length == 1 && p[0].ParameterType == dtoType;
        });

        if (singleMethod != null)
            return singleMethod;

        throw new InvalidOperationException($"Method '{methodName}' not found with supported signature in service '{serviceType.Name}'.");
    }

    private static int GetMaxDegreeOfParallelism()
    {
        return Environment.ProcessorCount <= 2
            ? 2
            : Math.Min(Environment.ProcessorCount, DefaultMaxParallelism);
    }

    private static void ValidateColumnConfiguration(
        Type dtoType,
        IReadOnlyCollection<BulkUploadColumnDefinition> columns,
        string moduleKey)
    {
        var writableProperties = dtoType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanWrite)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalidColumns = columns
            .Where(x => !writableProperties.Contains(x.PropertyName))
            .Select(x => x.PropertyName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (invalidColumns.Count == 0)
            return;

        throw new InvalidOperationException(
            $"Bulk upload config for module '{moduleKey}' contains invalid property mappings: {string.Join(", ", invalidColumns)}.");
    }

    private async Task<object> MapRowToDtoAsync(
        Type dtoType,
        Dictionary<string, string> row,
        IReadOnlyCollection<BulkUploadColumnDefinition> columns,
        CancellationToken ct)
    {
        var dto = Activator.CreateInstance(dtoType)
            ?? throw new InvalidOperationException($"Unable to create DTO instance for '{dtoType.Name}'.");

        foreach (var property in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            var column = columns.FirstOrDefault(x =>
                string.Equals(x.PropertyName, property.Name, StringComparison.OrdinalIgnoreCase));

            if (!TryGetRowValue(row, property.Name, column, out var raw))
            {
                if (column?.Required == true)
                    throw new InvalidOperationException($"Column '{GetColumnLabel(column, property.Name)}' was not found in the uploaded file.");

                TryApplySystemManagedDefault(dto, property);
                continue;
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                if (column?.Required == true)
                    throw new InvalidOperationException($"{GetColumnLabel(column, property.Name)} is required.");

                TryApplySystemManagedDefault(dto, property);
                continue;
            }

            if (column != null)
                ValidateConfiguredRules(column, raw);

            var resolved = await ResolveLookupValueAsync(property, raw, column, ct);
            var converted = ConvertTo(property.PropertyType, resolved, property.Name);
            property.SetValue(dto, converted);
        }

        return dto;
    }

    private static bool TryGetRowValue(
        Dictionary<string, string> row,
        string propertyName,
        BulkUploadColumnDefinition? column,
        out string value)
    {
        if (row.TryGetValue(propertyName, out value!))
            return true;

        if (column != null)
        {
            foreach (var alias in column.Aliases)
            {
                if (row.TryGetValue(alias, out value!))
                    return true;

                var aliasKey = row.Keys.FirstOrDefault(k => string.Equals(k, alias, StringComparison.OrdinalIgnoreCase));
                if (aliasKey != null)
                {
                    value = row[aliasKey];
                    return true;
                }
            }
        }

        var key = row.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
        if (key == null)
        {
            value = "";
            return false;
        }

        value = row[key];
        return true;
    }

    private void TryApplySystemManagedDefault(object dto, PropertyInfo property)
    {
        if (!property.CanWrite)
            return;

        if (string.Equals(property.Name, "CreatedBy", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(property.Name, "UpdatedBy", StringComparison.OrdinalIgnoreCase))
        {
            if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                var actor = _currentUser.AccountId;
                if (actor > 0)
                    property.SetValue(dto, actor);
            }
        }
    }

    private async Task<string> ResolveLookupValueAsync(
        PropertyInfo property,
        string raw,
        BulkUploadColumnDefinition? column,
        CancellationToken ct)
    {
        var trimmed = raw.Trim();
        var underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        var lookupType = column?.LookupType ?? BulkUploadColumnDefinitionParser.InferLookupType(property.Name);
        if (string.IsNullOrWhiteSpace(lookupType))
            return trimmed;

        if (underlying == typeof(int) &&
            int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return trimmed;
        }

        var result = await _lookupResolverService.ResolveAsync(lookupType, trimmed, ct);
        if (!result.Success || result.Value == null)
            throw new InvalidOperationException(result.Error ?? $"Lookup resolution failed for '{trimmed}'.");

        return Convert.ToString(result.Value, CultureInfo.InvariantCulture) ?? trimmed;
    }

    private static void ValidateConfiguredRules(BulkUploadColumnDefinition column, string raw)
    {
        var trimmed = raw.Trim();
        var label = GetColumnLabel(column, column.PropertyName);

        if (column.MinLength.HasValue && trimmed.Length < column.MinLength.Value)
            throw new InvalidOperationException($"{label} must be at least {column.MinLength.Value} characters.");

        if (column.MaxLength.HasValue && trimmed.Length > column.MaxLength.Value)
            throw new InvalidOperationException($"{label} cannot exceed {column.MaxLength.Value} characters.");

        if (!string.IsNullOrWhiteSpace(column.Regex) && !Regex.IsMatch(trimmed, column.Regex))
            throw new InvalidOperationException($"{label} format is invalid.");

        if (column.AllowedValues.Count > 0 &&
            !column.AllowedValues.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"{label} must be one of: {string.Join(", ", column.AllowedValues)}.");
        }
    }

    private async Task ApplyDatabaseUniquenessValidationAsync(
        string moduleKey,
        IReadOnlyCollection<RowCandidate> candidates,
        IReadOnlyCollection<BulkUploadColumnDefinition> columns,
        CancellationToken ct)
    {
        foreach (var candidate in candidates.Where(x => x.Errors.Count == 0))
        {
            foreach (var column in columns.Where(x => x.Unique))
            {
                var value = GetPropertyString(candidate.Dto, column.PropertyName);
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var scopeValues = BuildScopeValues(candidate.Dto, column.UniqueWith);
                var result = await _uniqueFieldValidator.ValidateAsync(moduleKey, column.PropertyName, value, scopeValues, ct);

                if (!string.IsNullOrWhiteSpace(result.Error))
                {
                    candidate.Errors.Add(result.Error);
                    continue;
                }

                if (result.IsDuplicate)
                    candidate.Errors.Add(BuildDuplicateMessage(column, value, scopeValues, "already exists in the database"));
            }
        }
    }

    private static void ApplyInFileDuplicateValidation(
        IReadOnlyCollection<RowCandidate> candidates,
        IReadOnlyCollection<BulkUploadColumnDefinition> columns)
    {
        foreach (var column in columns.Where(x => x.Unique))
        {
            var groups = candidates
                .Where(x => x.Errors.Count == 0)
                .Select(x => new
                {
                    Candidate = x,
                    Value = GetPropertyString(x.Dto, column.PropertyName),
                    Scope = BuildScopeValues(x.Dto, column.UniqueWith)
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .GroupBy(x => BuildUniqueSignature(column.PropertyName, x.Value!, x.Scope), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    item.Candidate.Errors.Add(BuildDuplicateMessage(column, item.Value!, item.Scope, "is duplicated inside the uploaded file"));
                }
            }
        }
    }

    private static string BuildUniqueSignature(string propertyName, string value, Dictionary<string, object> scopeValues)
    {
        var scope = string.Join("|", scopeValues
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.Key}:{NormalizeKeyValue(x.Value)}"));

        return $"{propertyName}:{NormalizeKeyValue(value)}|{scope}";
    }

    private static string BuildDuplicateMessage(
        BulkUploadColumnDefinition column,
        string value,
        Dictionary<string, object> scopeValues,
        string suffix)
    {
        if (scopeValues.Count == 0)
            return $"{GetColumnLabel(column, column.PropertyName)} '{value}' {suffix}.";

        var scope = string.Join(", ", scopeValues.Select(x => $"{x.Key}={x.Value}"));
        return $"{GetColumnLabel(column, column.PropertyName)} '{value}' {suffix} for scope [{scope}].";
    }

    private static Dictionary<string, object> BuildScopeValues(object dto, IReadOnlyCollection<string> scopeProperties)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var type = dto.GetType();

        foreach (var propertyName in scopeProperties.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            var value = property?.GetValue(dto);
            if (value != null)
                result[propertyName] = value;
        }

        return result;
    }

    private static string? GetPropertyString(object dto, string propertyName)
    {
        var property = dto.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        var value = property?.GetValue(dto);
        return value?.ToString()?.Trim();
    }

    private static string NormalizeKeyValue(object? value)
    {
        return value == null ? "" : value.ToString()?.Trim().ToLowerInvariant() ?? "";
    }

    private static string GetColumnLabel(BulkUploadColumnDefinition? column, string fallback)
    {
        return string.IsNullOrWhiteSpace(column?.Header) ? fallback : column.Header;
    }

    private static object ConvertTo(Type targetType, string value, string propertyName)
    {
        var trimmed = value.Trim();
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (underlying == typeof(string))
                return trimmed;

            if (underlying == typeof(int))
                return int.Parse(trimmed, CultureInfo.InvariantCulture);

            if (underlying == typeof(long))
                return long.Parse(trimmed, CultureInfo.InvariantCulture);

            if (underlying == typeof(decimal))
                return decimal.Parse(trimmed, CultureInfo.InvariantCulture);

            if (underlying == typeof(double))
                return double.Parse(trimmed, CultureInfo.InvariantCulture);

            if (underlying == typeof(float))
                return float.Parse(trimmed, CultureInfo.InvariantCulture);

            if (underlying == typeof(bool))
            {
                if (trimmed == "1") return true;
                if (trimmed == "0") return false;
                return bool.Parse(trimmed);
            }

            if (underlying == typeof(DateTime))
                return DateTime.Parse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            if (underlying == typeof(Guid))
                return Guid.Parse(trimmed);

            if (underlying.IsEnum)
                return Enum.Parse(underlying, trimmed, ignoreCase: true);

            var converter = TypeDescriptor.GetConverter(underlying);
            if (converter.CanConvertFrom(typeof(string)))
                return converter.ConvertFromInvariantString(trimmed)!;

            return Convert.ChangeType(trimmed, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Invalid value '{value}' for '{propertyName}': {ex.Message}");
        }
    }

    private static List<List<RowCandidate>> Chunk(List<RowCandidate> source, int size)
    {
        var chunks = new List<List<RowCandidate>>();
        for (int i = 0; i < source.Count; i += size)
            chunks.Add(source.Skip(i).Take(size).ToList());
        return chunks;
    }

    private async Task InvokeConfiguredMethodAsync(object service, MethodInfo method, Type dtoType, List<object> batch, CancellationToken ct)
    {
        var p = method.GetParameters();
        object? result;

        if (p.Length == 1 &&
            p[0].ParameterType.IsGenericType &&
            p[0].ParameterType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var listType = typeof(List<>).MakeGenericType(dtoType);
            var typedList = (System.Collections.IList)Activator.CreateInstance(listType)!;
            foreach (var item in batch) typedList.Add(item);

            result = method.Invoke(service, new object[] { typedList });
            if (result is Task task) await task;
            return;
        }

        foreach (var item in batch)
        {
            ct.ThrowIfCancellationRequested();
            result = method.Invoke(service, new[] { item });
            if (result is Task task) await task;
        }
    }

    private async Task<string> GenerateErrorReportAsync(int jobId, List<BulkRowErrorDto> errors, CancellationToken ct)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "uploads", "bulk-errors");
        Directory.CreateDirectory(folder);

        var filePath = Path.Combine(folder, $"bulk_errors_{jobId}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Errors");

        ws.Cell(1, 1).Value = "RowNumber";
        ws.Cell(1, 2).Value = "ErrorMessage";
        ws.Row(1).Style.Font.Bold = true;

        int row = 2;
        foreach (var err in errors)
        {
            ct.ThrowIfCancellationRequested();
            ws.Cell(row, 1).Value = err.RowNumber;
            ws.Cell(row, 2).Value = err.Message;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        await File.WriteAllBytesAsync(filePath, ms.ToArray(), ct);

        return filePath;
    }

    private async Task SaveErrorRowsAsync(BulkUploadWorkItem workItem, ConcurrentBag<BulkRowErrorDto> errors, CancellationToken ct)
    {
        if (errors.IsEmpty)
            return;

        var rowEntities = errors.Select(x => new bulk_job_row
        {
            JobId = workItem.JobId,
            ModuleName = workItem.ModuleKey,
            RowNumber = x.RowNumber,
            PayloadJson = JsonSerializer.Serialize(workItem.Rows.ElementAtOrDefault(Math.Max(0, x.RowNumber - 2)) ?? new Dictionary<string, string>()),
            Status = "FAILED",
            ErrorMessage = x.Message,
            RetryCount = 0
        }).ToList();

        await _db.bulk_job_rows.AddRangeAsync(rowEntities, ct);
    }

    private sealed class RowCandidate
    {
        public RowCandidate(int rowNumber, object dto)
        {
            RowNumber = rowNumber;
            Dto = dto;
        }

        public int RowNumber { get; }
        public object Dto { get; }
        public List<string> Errors { get; } = new();
    }
}
