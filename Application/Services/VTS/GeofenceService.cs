using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using System.Text;

public class GeofenceService : IGeofenceService
{
    private readonly IdentityDbContext _db;
    private readonly IExternalMappingApiService _external;
    private readonly ICurrentUserService _currentUser;

    public GeofenceService(
        IdentityDbContext db,
        IExternalMappingApiService external,
        ICurrentUserService currentUser)
    {
        _db = db;
        _external = external;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateGeofenceDto dto)
    {
        dto.Coordinates = NormalizeCoordinates(dto.Coordinates, dto.CoordinatesJson);

        var code = dto.UniqueCode?.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(code))
            throw new Exception("Unique code is required");

        var exists = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.UniqueCode == code &&
                           x.AccountId == dto.AccountId &&
                           !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Geofence already exists.");

        var geom = BuildGeometry(dto.GeometryType, dto.Coordinates);

        var entity = new mst_Geofence
        {
            AccountId = dto.AccountId,
            UniqueCode = code,
            DisplayName = dto.DisplayName?.Trim(),
            GeometryType = dto.GeometryType,
            RadiusM = dto.GeometryType == "CIRCLE" ? dto.RadiusM : null,
            Geom = geom,
            CoordinatesJson = JsonDocument.Parse(JsonSerializer.Serialize(dto.Coordinates)),
            Status = dto.IsEnabled ? "ENABLED" : "DISABLED",
            IsActive = dto.IsEnabled,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.GeofenceZones.Add(entity);
        await _db.SaveChangesAsync();

        try
        {
            await SyncGeofenceAsync(entity, dto.Coordinates, HttpMethod.Post);
        }
        catch { }

        return entity.Id;
    }
    public async Task<int> CreateByLocationAsync(CreateGeofenceByLocationDto dto)
    {
        if (dto.Latitude == 0 || dto.Longitude == 0)
            throw new Exception("Invalid latitude/longitude");

        // Default radius
        var radius = 500;

        // Unique code generate (simple)
        var code = $"GF_{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Create coordinate list (center point)
        var coordinates = new List<CoordinateDto>
    {
        new CoordinateDto
        {
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        }
    };

        // Build geometry (circle)
        var geom = BuildGeometry("CIRCLE", coordinates);

        var entity = new mst_Geofence
        {
            AccountId = dto.AccountId,
            UniqueCode = code,
            DisplayName = dto.DisplayName?.Trim() ?? dto.Address,
            Description = dto.Address,

            GeometryType = "CIRCLE",
            RadiusM = radius,
            Geom = geom,

            CoordinatesJson = JsonDocument.Parse(JsonSerializer.Serialize(coordinates)),

            Status = "ENABLED",
            IsActive = true,

            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.GeofenceZones.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }
    public async Task<GeofenceListUiResponseDto> GetZones(
           int page,
           int pageSize,
           int? accountId,
           string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.GeofenceZones
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        // ✅ Account filter (NEW FIX)
        if (accountId.HasValue)
        {
            query = query.Where(x => x.AccountId == accountId.Value);
        }

        // ✅ Search (NULL SAFE)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(x =>
                (x.UniqueCode != null && x.UniqueCode.ToLower().Contains(s)) ||
                (x.DisplayName != null && x.DisplayName.ToLower().Contains(s))
            );
        }

        // ✅ OPTIMIZED SUMMARY (Single DB Call)
        var summaryData = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Enabled = g.Count(x => x.Status == "ENABLED")
            })
            .FirstOrDefaultAsync();

        var total = summaryData?.Total ?? 0;
        var enabled = summaryData?.Enabled ?? 0;
        var disabled = total - enabled;

        var summary = new GeofenceSummaryDto
        {
            TotalZones = total,
            Enabled = enabled,
            Disabled = disabled
        };

        // ✅ DATA
        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new GeofenceDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                UniqueCode = x.UniqueCode,
                DisplayName = x.DisplayName,
                ClassificationCode = x.ClassificationCode,
                GeometryType = x.GeometryType,
                RadiusM = x.RadiusM,
                Status = x.Status,
                ColorTheme = x.ColorTheme,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new GeofenceListUiResponseDto
        {
            Summary = summary,
            Zones = new PagedResultDto<GeofenceDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    // ===============================
    // GET BY ID (SAFE)
    // ===============================
    public async Task<GeofenceDto?> GetByIdAsync(int id)
    {
        var entity = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return null;

        var coordinates = entity.CoordinatesJson == null
            ? new List<CoordinateDto>()
            : entity.CoordinatesJson.Deserialize<List<CoordinateDto>>() ?? new();

        return new GeofenceDto
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            UniqueCode = entity.UniqueCode,
            DisplayName = entity.DisplayName,
            ClassificationCode = entity.ClassificationCode,
            GeometryType = entity.GeometryType,
            RadiusM = entity.RadiusM,
            Status = entity.Status,
            ColorTheme = entity.ColorTheme,
            CreatedAt = entity.CreatedAt,
            Coordinates = coordinates
        };
    }



    public async Task<PagedResultDto<GeofenceDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId,
        string? search = null)
    {
        var result = await GetZones(page, pageSize, accountId, search);
        return result.Zones;
    }



    public async Task<bool> UpdateAsync(int id, UpdateGeofenceDto dto)
    {
        var entity = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        dto.Coordinates = NormalizeCoordinates(dto.Coordinates, dto.CoordinatesJson);

        var code = dto.UniqueCode.Trim().ToUpper();

        var exists = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.UniqueCode == code && x.Id != id && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Geofence already exists.");

        entity.UniqueCode = code;
        entity.DisplayName = dto.DisplayName.Trim();
        entity.Description = dto.Description;
        entity.ClassificationCode = dto.ClassificationCode;
        entity.ClassificationLabel = dto.ClassificationLabel;
        entity.GeometryType = dto.GeometryType;
        entity.RadiusM = dto.RadiusM;
        entity.Geom = BuildGeometry(dto.GeometryType, dto.Coordinates);
        // ⭐ Save coordinates JSON
        entity.CoordinatesJson = JsonDocument.Parse(JsonSerializer.Serialize(dto.Coordinates));
        entity.ColorTheme = dto.ColorTheme;
        entity.Opacity = dto.Opacity;
        entity.Status = dto.IsEnabled ? "ENABLED" : "DISABLED";
        entity.IsActive = dto.IsEnabled;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isEnabled)
    {
        var entity = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.Status = isEnabled ? "ENABLED" : "DISABLED";
        entity.IsActive = isEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedBy = _currentUser.AccountId;
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;


        await _db.SaveChangesAsync();

        List<CoordinateDto> coordinates = new();

        if (entity.CoordinatesJson != null)
        {
            coordinates = entity.CoordinatesJson
                .Deserialize<List<CoordinateDto>>() ?? new List<CoordinateDto>();
        }


        try
        {
            await SyncGeofenceAsync(entity, coordinates, HttpMethod.Delete);
        }
        catch
        {
            // External sync failure should not roll back a successful local delete.
        }

        return true;
    }
    // ===============================
    // Geometry Builder
    // ===============================
    private Geometry BuildGeometry(string geometryType, List<CoordinateDto> coordinates)
    {
        if (coordinates == null || coordinates.Count == 0)
            throw new Exception("Coordinates are required");

        var factory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        if (geometryType == "CIRCLE")
        {
            var c = coordinates.First();

            return factory.CreatePoint(new Coordinate(c.Longitude, c.Latitude));
        }

        if (geometryType == "POLYGON")
        {
            if (coordinates.Count < 3)
                throw new Exception("Polygon requires at least 3 points");

            var coords = coordinates
                .Select(c => new Coordinate(c.Longitude, c.Latitude))
                .Distinct()
                .ToList();

            // Close polygon
            if (!coords.First().Equals2D(coords.Last()))
                coords.Add(coords.First());

            var polygon = factory.CreatePolygon(coords.ToArray());

            // 🔥 IMPORTANT: Fix invalid polygons automatically
            if (!polygon.IsValid)
                polygon = (Polygon)polygon.Buffer(0);

            if (!polygon.IsValid)
                throw new Exception("Invalid polygon geometry");

            return polygon;
        }

        throw new Exception("Invalid geometry type");
    }

    private static List<CoordinateDto> NormalizeCoordinates(
        List<CoordinateDto>? coordinates,
        string? coordinatesJson)
    {
        if (coordinates != null && coordinates.Count > 0)
            return coordinates;

        if (string.IsNullOrWhiteSpace(coordinatesJson))
            return new List<CoordinateDto>();

        try
        {
            return JsonSerializer.Deserialize<List<CoordinateDto>>(coordinatesJson) ?? new List<CoordinateDto>();
        }
        catch
        {
            return new List<CoordinateDto>();
        }
    }

    public async Task<List<GeofenceDto>> BulkCreateAsync(List<CreateGeofenceDto> items)
    {
        if (items == null || items.Count == 0)
            return new List<GeofenceDto>();

        var entities = new List<mst_Geofence>();

        foreach (var dto in items)
        {
            dto.Coordinates = NormalizeCoordinates(dto.Coordinates, dto.CoordinatesJson);

            var code = dto.UniqueCode.Trim().ToUpper();

            var exists = await _db.GeofenceZones
                .ApplyAccountHierarchyFilter(_currentUser)
                .AnyAsync(x => x.UniqueCode == code &&
                               x.AccountId == dto.AccountId &&
                               !x.IsDeleted);

            if (exists)
                continue; // skip duplicates

            var entity = new mst_Geofence
            {
                AccountId = dto.AccountId,
                UniqueCode = code,
                DisplayName = dto.DisplayName.Trim(),
                Description = dto.Description,
                ClassificationCode = dto.ClassificationCode,
                ClassificationLabel = dto.ClassificationLabel,
                GeometryType = dto.GeometryType,
                RadiusM = dto.RadiusM,
                Geom = BuildGeometry(dto.GeometryType, dto.Coordinates),
                ColorTheme = dto.ColorTheme,
                Opacity = dto.Opacity,
                Status = dto.IsEnabled ? "ENABLED" : "DISABLED",
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow
            };

            entities.Add(entity);
        }

        _db.GeofenceZones.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(x => new GeofenceDto
        {
            Id = x.Id,
            AccountId = x.AccountId,
            UniqueCode = x.UniqueCode,
            DisplayName = x.DisplayName,
            ClassificationCode = x.ClassificationCode,
            GeometryType = x.GeometryType,
            RadiusM = x.RadiusM,
            Status = x.Status,
            ColorTheme = x.ColorTheme,
            CreatedAt = x.CreatedAt
        }).ToList();
    }
    private ExternalGeofenceRequest BuildExternalPayload(
    mst_Geofence entity,
    List<CoordinateDto> coordinates)
    {
        var center = coordinates.First();

        return new ExternalGeofenceRequest
        {
            GeoId = entity.Id.ToString(),
            GeoName = entity.DisplayName,
            OrgId = entity.AccountId,
            OrgName = $"Org-{entity.AccountId}",

            Latitude = center.Latitude,
            Longitude = center.Longitude,
            Radius = entity.RadiusM ?? 0,
            GeoType = entity.GeometryType,

            GeoPoints = coordinates.Select(x => new ExternalGeoPoint
            {
                Latitude = x.Latitude,
                Longitude = x.Longitude
            }).ToList()
        };
    }
    private async Task SyncGeofenceAsync(
    mst_Geofence entity,
    List<CoordinateDto> coordinates,
    HttpMethod method)
    {
        ExternalGeofenceRequest? request = null;

        bool success = false;
        string? error = null;

        try
        {
            request = BuildExternalPayload(entity, coordinates);
            success = await _external.SendGeofenceAsync(
                new List<ExternalGeofenceRequest> { request },
                method);

            if (success)
            {
                entity.SyncStatus = "SYNCED";
                entity.LastSyncedAt = DateTime.UtcNow;
                entity.SyncError = null;
            }
            else
            {
                entity.SyncStatus = "FAILED";
                entity.SyncError = "External API returned failure";
            }
        }
        catch (Exception ex)
        {
            success = false;
            error = ex.Message;

            entity.SyncStatus = "FAILED";
            entity.SyncError = ex.Message;
        }

        // ⭐ LOG SYNC REQUEST
        object payloadForLog = request != null
            ? request
            : new
            {
                entity.Id,
                entity.AccountId,
                entity.DisplayName,
                entity.GeometryType,
                CoordinatesCount = coordinates?.Count ?? 0
            };

        try
        {
            var log = new map_geofence_sync_log
            {
                GeofenceId = entity.Id,
                PayloadJson = JsonSerializer.Serialize(payloadForLog),
                IsSynced = success,
                ErrorMessage = error,
                RetryCount = success ? 0 : 1,
                LastTriedAt = DateTime.UtcNow
            };

            _db.map_geofence_sync_logs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Missing sync-log table or sync status persistence issues should not fail the API request.
        }
    }
    public async Task<byte[]> ExportGeofenceCsvAsync(int? accountId, string? search)
    {
        var query = _db.GeofenceZones
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
        {
            query = query.Where(x => x.AccountId == accountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(x =>
                (!string.IsNullOrEmpty(x.UniqueCode) && x.UniqueCode.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.DisplayName) && x.DisplayName.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.GeometryType) && x.GeometryType.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.Status) && x.Status.ToLower().Contains(s))
            );
        }

        var rows = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                x.AccountId,
                x.UniqueCode,
                x.DisplayName,
                x.ClassificationCode,
                x.GeometryType,
                x.RadiusM,
                x.Status,
                x.ColorTheme,
                LastUpdated = x.UpdatedAt ?? x.CreatedAt
            })
            .ToListAsync();

        var accountIds = rows
            .Select(x => x.AccountId)
            .Distinct()
            .ToList();

        var accountNames = await _db.Accounts
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => accountIds.Contains(x.AccountId))
            .ToDictionaryAsync(x => x.AccountId, x => x.AccountName);

        var sb = new StringBuilder();

        sb.AppendLine("Account,Unique Code,Display Name,Classification Code,Geometry Type,Radius (M),Status,Color Theme,Last Updated");

        foreach (var g in rows)
        {
            var accountName = accountNames.ContainsKey(g.AccountId)
                ? accountNames[g.AccountId]
                : "";
            var lastUpdated = g.LastUpdated.HasValue
                ? g.LastUpdated.Value.ToLocalTime().ToString("dd/MM/yyyy, hh:mm tt")
                : "";

            sb.AppendLine(
                $"\"{accountName}\"," +
                $"\"{g.UniqueCode}\"," +
                $"\"{g.DisplayName}\"," +
                $"\"{g.ClassificationCode}\"," +
                $"\"{g.GeometryType}\"," +
                $"\"{g.RadiusM}\"," +
                $"\"{g.Status}\"," +
                $"\"{g.ColorTheme}\"," +
                $"\"{lastUpdated}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportGeofencesXlsxAsync(int? accountId, string? search)
    {
        var query = _db.GeofenceZones
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
        {
            query = query.Where(x => x.AccountId == accountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (!string.IsNullOrEmpty(x.UniqueCode) && x.UniqueCode.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.DisplayName) && x.DisplayName.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.GeometryType) && x.GeometryType.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.Status) && x.Status.ToLower().Contains(s))
            );
        }

        var rows = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                x.AccountId,
                x.UniqueCode,
                x.DisplayName,
                x.ClassificationCode,
                x.GeometryType,
                x.RadiusM,
                x.Status,
                x.ColorTheme,
                LastUpdated = x.UpdatedAt ?? x.CreatedAt
            })
            .ToListAsync();

        var accountIds = rows.Select(x => x.AccountId).Distinct().ToList();
        var accountNames = await _db.Accounts
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => accountIds.Contains(x.AccountId))
            .ToDictionaryAsync(x => x.AccountId, x => x.AccountName);

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Geofences");

            worksheet.Cell(1, 1).Value = "Account";
            worksheet.Cell(1, 2).Value = "Unique Code";
            worksheet.Cell(1, 3).Value = "Display Name";
            worksheet.Cell(1, 4).Value = "Classification Code";
            worksheet.Cell(1, 5).Value = "Geometry Type";
            worksheet.Cell(1, 6).Value = "Radius (M)";
            worksheet.Cell(1, 7).Value = "Status";
            worksheet.Cell(1, 8).Value = "Color Theme";
            worksheet.Cell(1, 9).Value = "Last Updated";

            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            int rowNumber = 2;
            foreach (var g in rows)
            {
                var accountName = accountNames.ContainsKey(g.AccountId) ? accountNames[g.AccountId] : "";
                var lastUpdated = g.LastUpdated.HasValue ? g.LastUpdated.Value.ToLocalTime().ToString("dd/MM/yyyy, hh:mm tt") : "";

                worksheet.Cell(rowNumber, 1).Value = accountName;
                worksheet.Cell(rowNumber, 2).Value = g.UniqueCode;
                worksheet.Cell(rowNumber, 3).Value = g.DisplayName;
                worksheet.Cell(rowNumber, 4).Value = g.ClassificationCode;
                worksheet.Cell(rowNumber, 5).Value = g.GeometryType;
                worksheet.Cell(rowNumber, 6).Value = g.RadiusM;
                worksheet.Cell(rowNumber, 7).Value = g.Status;
                worksheet.Cell(rowNumber, 8).Value = g.ColorTheme;
                worksheet.Cell(rowNumber, 9).Value = lastUpdated;
                rowNumber++;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                stream.Flush();
                return stream.ToArray();
            }
        }
    }
}
