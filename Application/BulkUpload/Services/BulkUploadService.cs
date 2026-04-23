using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class BulkUploadService : IBulkUploadService
{
    private readonly IdentityDbContext _db;
    private readonly IExcelParser _excelParser;
    private readonly ICsvParser _csvParser;
    private readonly IBulkUploadQueue _queue;
    private readonly ITemplateService _templateService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<BulkUploadService> _logger;

    public BulkUploadService(
        IdentityDbContext db,
        IExcelParser excelParser,
        ICsvParser csvParser,
        IBulkUploadQueue queue,
        ITemplateService templateService,
        ICurrentUserService currentUser,
        ILogger<BulkUploadService> logger)
    {
        _db = db;
        _excelParser = excelParser;
        _csvParser = csvParser;
        _queue = queue;
        _templateService = templateService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<BulkUploadStartResultDto> EnqueueUploadAsync(string moduleKey, IFormFile file, CancellationToken ct = default)
    {
        var config = await ResolveConfigAsync(moduleKey, ct);

        if (config == null)
            throw new KeyNotFoundException($"Bulk upload config not found for module '{moduleKey}'.");

        if (file == null || file.Length == 0)
            throw new BadHttpRequestException("Upload file is required.");

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".csv")
            throw new BadHttpRequestException("Only .xlsx and .csv are supported.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        var rows = ext == ".xlsx"
            ? await _excelParser.ParseAsync(ms, ct)
            : await _csvParser.ParseAsync(ms, ct);

        rows = NormalizeRowsForModule(moduleKey, rows);
        ValidateUploadedHeaders(moduleKey, rows, config.ColumnsJson);

        var job = new bulk_job
        {
            ModuleName = moduleKey,
            FileName = file.FileName,
            TotalRows = rows.Count,
            ProcessedRows = 0,
            SuccessRows = 0,
            FailedRows = 0,
            Status = "PENDING",
            CreatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null
        };

        _db.bulk_jobs.Add(job);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk upload job created. JobId={JobId}, Module={Module}, Rows={Rows}", job.Id, moduleKey, rows.Count);

        await _queue.EnqueueAsync(new BulkUploadWorkItem
        {
            JobId = job.Id,
            ModuleKey = config.ModuleKey,
            Rows = rows,
            CreatedBy = job.CreatedBy,
            UserId = _currentUser.UserId,
            AccountId = _currentUser.AccountId,
            RoleId = _currentUser.RoleId,
            Role = _currentUser.Role,
            HierarchyPath = _currentUser.HierarchyPath,
            IsSystemRole = _currentUser.IsSystemRole,
            IsAuthenticated = _currentUser.IsAuthenticated,
            AccessibleAccountIds = _currentUser.AccessibleAccountIds.ToList()
        }, ct);

        return new BulkUploadStartResultDto
        {
            JobId = job.Id,
            ModuleKey = config.ModuleKey,
            TotalRows = rows.Count,
            Status = "PENDING"
        };
    }

    public async Task<BulkUploadStatusDto?> GetStatusAsync(int jobId, CancellationToken ct = default)
    {
        return await _db.bulk_jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => new BulkUploadStatusDto
            {
                JobId = x.Id,
                ModuleKey = x.ModuleName,
                Status = x.Status,
                TotalRows = x.TotalRows,
                ProcessedRows = x.ProcessedRows,
                SuccessRows = x.SuccessRows,
                FailedRows = x.FailedRows,
                ErrorFilePath = x.ErrorFilePath,
                CompletedAt = x.CompletedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public Task<(byte[] Content, string ContentType, string FileName)> GetTemplateAsync(string moduleKey, string format, CancellationToken ct = default)
    {
        return _templateService.GenerateTemplateAsync(moduleKey, format, ct);
    }

    public async Task<(byte[] Content, string FileName)?> GetErrorReportAsync(int jobId, CancellationToken ct = default)
    {
        var path = await _db.bulk_jobs
            .AsNoTracking()
            .Where(x => x.Id == jobId)
            .Select(x => x.ErrorFilePath)
            .FirstOrDefaultAsync(ct);

        var resolvedPath = ResolveExistingErrorReportPath(path);
        if (string.IsNullOrWhiteSpace(resolvedPath))
            return null;

        return (await File.ReadAllBytesAsync(resolvedPath, ct), Path.GetFileName(resolvedPath));
    }

    private static void ValidateUploadedHeaders(
        string moduleKey,
        IReadOnlyCollection<Dictionary<string, string>> rows,
        string? columnsJson)
    {
        if (rows == null || rows.Count == 0)
            throw new BadHttpRequestException("Uploaded file is empty.");

        var uploadedHeaders = rows
            .SelectMany(x => x.Keys)
            .Select(NormalizeHeader)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (uploadedHeaders.Count == 0)
            throw new BadHttpRequestException("Uploaded file is empty.");

        var columns = BulkUploadColumnDefinitionParser.Parse(columnsJson)
            .Where(x => !BulkUploadColumnDefinitionParser.IsSystemManagedFieldName(x.PropertyName))
            .ToList();

        var expectedColumns = columns
            .Select(x => new
            {
                Column = x,
                HeaderName = string.IsNullOrWhiteSpace(x.Header) ? x.PropertyName : x.Header,
                NormalizedNames = BuildExpectedHeaderNames(x)
            })
            .ToList();

        var allExpectedHeaders = expectedColumns
            .SelectMany(x => x.NormalizedNames)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matchedHeaders = uploadedHeaders
            .Where(x => allExpectedHeaders.Contains(x))
            .ToList();

        if (matchedHeaders.Count == 0)
        {
            throw new BadHttpRequestException(
                $"The uploaded file does not belong to module '{moduleKey}'. Please use the correct template.");
        }

        var missingRequiredHeaders = expectedColumns
            .Where(x => x.Column.Required)
            .Where(x => !x.NormalizedNames.Any(uploadedHeaders.Contains))
            .Select(x => x.HeaderName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missingRequiredHeaders.Count > 0)
        {
            throw new BadHttpRequestException(
                $"Invalid file uploaded for module '{moduleKey}'. Missing columns: {string.Join(", ", missingRequiredHeaders)}");
        }
    }

    private static HashSet<string> BuildExpectedHeaderNames(BulkUploadColumnDefinition column)
    {
        var names = new List<string>
        {
            column.PropertyName,
            column.Header
        };

        if (column.Aliases.Count > 0)
            names.AddRange(column.Aliases);

        return names
            .Select(NormalizeHeader)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ");

        return string.Join(
            " ",
            normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private static List<Dictionary<string, string>> NormalizeRowsForModule(
        string moduleKey,
        List<Dictionary<string, string>> rows)
    {
        if (!IsGeofenceBulkModule(moduleKey))
            return rows;

        return rows
            .Select(NormalizeGeofenceRow)
            .ToList();
    }

    private static Dictionary<string, string> NormalizeGeofenceRow(Dictionary<string, string> row)
    {
        var normalized = new Dictionary<string, string>(row, StringComparer.OrdinalIgnoreCase);

        if (TryGetFirstValue(normalized, out var geometryType, "GeometryType"))
            normalized["GeometryType"] = geometryType.Trim().ToUpperInvariant();

        if (!TryGetFirstValue(normalized, out var coordinatesJson, "CoordinatesJson") ||
            string.IsNullOrWhiteSpace(coordinatesJson))
        {
            coordinatesJson = BuildGeofenceCoordinatesJson(normalized);
            if (!string.IsNullOrWhiteSpace(coordinatesJson))
                normalized["CoordinatesJson"] = coordinatesJson;
        }

        return normalized;
    }

    private static string BuildGeofenceCoordinatesJson(Dictionary<string, string> row)
    {
        var geometryType = TryGetFirstValue(row, out var rawGeometryType, "GeometryType")
            ? rawGeometryType.Trim().ToUpperInvariant()
            : string.Empty;

        if (string.IsNullOrWhiteSpace(geometryType))
            return string.Empty;

        if (string.Equals(geometryType, "CIRCLE", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryGetFirstValue(row, out var latitudeRaw, "Latitude"))
                return string.Empty;

            if (!TryGetFirstValue(row, out var longitudeRaw, "Longitude"))
                return string.Empty;

            if (!TryParseCoordinate(latitudeRaw, out var latitude) ||
                !TryParseCoordinate(longitudeRaw, out var longitude))
            {
                return string.Empty;
            }

            return SerializeCoordinates(new[]
            {
                new CoordinateDto
                {
                    Latitude = latitude,
                    Longitude = longitude
                }
            });
        }

        if (string.Equals(geometryType, "POLYGON", StringComparison.OrdinalIgnoreCase) &&
            TryGetFirstValue(row, out var geoPointRaw, "GeoPoint") &&
            !string.IsNullOrWhiteSpace(geoPointRaw))
        {
            var points = geoPointRaw
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(ParseGeoPoint)
                .Where(x => x != null)
                .Cast<CoordinateDto>()
                .ToList();

            if (points.Count >= 3)
                return SerializeCoordinates(points);
        }

        return string.Empty;
    }

    private static CoordinateDto? ParseGeoPoint(string rawPoint)
    {
        var parts = rawPoint
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToArray();

        if (parts.Length != 2)
            return null;

        if (!TryParseCoordinate(parts[0], out var latitude) ||
            !TryParseCoordinate(parts[1], out var longitude))
        {
            return null;
        }

        return new CoordinateDto
        {
            Latitude = latitude,
            Longitude = longitude
        };
    }

    private static bool TryParseCoordinate(string raw, out double value)
    {
        return double.TryParse(
            raw?.Trim(),
            NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static string SerializeCoordinates(IEnumerable<CoordinateDto> coordinates)
    {
        return JsonSerializer.Serialize(coordinates);
    }

    private static bool TryGetFirstValue(
        IReadOnlyDictionary<string, string> row,
        out string value,
        params string[] keys)
    {
        foreach (var key in keys)
        {
            if (row.TryGetValue(key, out value!) && !string.IsNullOrWhiteSpace(value))
                return true;
        }

        value = string.Empty;
        return false;
    }

    private static string? ResolveExistingErrorReportPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (File.Exists(path))
            return path;

        var fileName = Path.GetFileName(path);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, path),
            Path.Combine(AppContext.BaseDirectory, "uploads", "bulk-errors", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), path),
            Path.Combine(Directory.GetCurrentDirectory(), "uploads", "bulk-errors", fileName)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static bool IsGeofenceBulkModule(string? moduleKey)
    {
        return string.Equals(moduleKey, "geofence-master", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(moduleKey, "geofence", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<BulkUploadConfig?> ResolveConfigAsync(string moduleKey, CancellationToken ct)
    {
        var candidateKeys = GetConfigLookupKeys(moduleKey);

        return await _db.BulkUploadConfigs
            .AsNoTracking()
            .Where(x => x.IsActive && candidateKeys.Contains(x.ModuleKey))
            .OrderBy(x => x.ModuleKey == moduleKey ? 0 : 1)
            .FirstOrDefaultAsync(ct);
    }

    private static List<string> GetConfigLookupKeys(string moduleKey)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { moduleKey };

        if (string.Equals(moduleKey, "geofence", StringComparison.OrdinalIgnoreCase))
            keys.Add("geofence-master");
        else if (string.Equals(moduleKey, "geofence-master", StringComparison.OrdinalIgnoreCase))
            keys.Add("geofence");

        return keys.ToList();
    }
}
