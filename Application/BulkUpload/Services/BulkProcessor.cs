using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class BulkProcessor : IBulkProcessor
{
    private const int DefaultBatchSize = 100;
    private const int DefaultMaxParallelism = 8;

    private readonly IdentityDbContext _db;
    private readonly IServiceResolver _serviceResolver;
    private readonly IValidationService _validationService;
    private readonly IExternalBulkSyncService _externalSyncService;
    private readonly ILogger<BulkProcessor> _logger;

    public BulkProcessor(
        IdentityDbContext db,
        IServiceResolver serviceResolver,
        IValidationService validationService,
        IExternalBulkSyncService externalSyncService,
        ILogger<BulkProcessor> logger)
    {
        _db = db;
        _serviceResolver = serviceResolver;
        _validationService = validationService;
        _externalSyncService = externalSyncService;
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

        var dtoType = _serviceResolver.ResolveDtoType(config.DtoName);
        var service = _serviceResolver.ResolveService(config.ServiceInterface);
        var method = ResolveMethod(service.GetType(), config.ServiceMethod, dtoType);

        var validItems = new ConcurrentBag<(int RowNumber, object Dto)>();
        var errors = new ConcurrentBag<BulkRowErrorDto>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(),
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, workItem.Rows.Count),
            parallelOptions,
            (i, token) =>
            {
                var rowNumber = i + 2;
                var rowData = workItem.Rows[i];

                try
                {
                    var dto = MapRowToDto(dtoType, rowData);
                    var validationErrors = _validationService.ValidateObject(dto);

                    if (validationErrors.Count > 0)
                    {
                        errors.Add(new BulkRowErrorDto
                        {
                            RowNumber = rowNumber,
                            Message = string.Join("; ", validationErrors)
                        });
                    }
                    else
                    {
                        validItems.Add((rowNumber, dto));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new BulkRowErrorDto
                    {
                        RowNumber = rowNumber,
                        Message = ex.Message
                    });
                }

                return ValueTask.CompletedTask;
            });

        var sortedValid = validItems.OrderBy(x => x.RowNumber).ToList();
        int success = 0;
        int failed = errors.Count;

        foreach (var batch in Chunk(sortedValid, DefaultBatchSize))
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

    private object MapRowToDto(Type dtoType, Dictionary<string, string> row)
    {
        var dto = Activator.CreateInstance(dtoType)
            ?? throw new InvalidOperationException($"Unable to create DTO instance for '{dtoType.Name}'.");

        foreach (var property in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            if (!TryGetRowValue(row, property.Name, out var raw))
                continue;

            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var converted = ConvertTo(property.PropertyType, raw, property.Name);
            property.SetValue(dto, converted);
        }

        return dto;
    }

    private static bool TryGetRowValue(Dictionary<string, string> row, string propertyName, out string value)
    {
        if (row.TryGetValue(propertyName, out value!))
            return true;

        var key = row.Keys.FirstOrDefault(k => string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
        if (key == null)
        {
            value = "";
            return false;
        }

        value = row[key];
        return true;
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

    private static List<List<(int RowNumber, object Dto)>> Chunk(List<(int RowNumber, object Dto)> source, int size)
    {
        var chunks = new List<List<(int RowNumber, object Dto)>>();
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
        var folder = Path.Combine("uploads", "bulk-errors");
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
}
