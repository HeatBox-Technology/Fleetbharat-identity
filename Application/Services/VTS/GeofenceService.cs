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

public class GeofenceService : IGeofenceService
{
    private readonly IdentityDbContext _db;
    private readonly IExternalMappingApiService _external;

    public GeofenceService(IdentityDbContext db, IExternalMappingApiService external)
    {
        _db = db;
        _external = external;
    }

    public async Task<int> CreateAsync(CreateGeofenceDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var code = dto.UniqueCode?.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(code))
            throw new Exception("Unique code is required");

        var geometryType = dto.GeometryType?.Trim().ToUpper();

        if (geometryType != "CIRCLE" && geometryType != "POLYGON")
            throw new Exception("GeometryType must be CIRCLE or POLYGON");

        if (geometryType == "CIRCLE" && dto.RadiusM <= 0)
            throw new Exception("Radius must be greater than zero for circle");

        var exists = await _db.GeofenceZones
            .AnyAsync(x => x.UniqueCode == code &&
                           x.AccountId == dto.AccountId &&
                           !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Geofence already exists.");

        Geometry geom;

        try
        {
            geom = BuildGeometry(geometryType, dto.Coordinates);

            if (geom == null)
                throw new Exception("Geometry creation failed");
        }
        catch (Exception ex)
        {
            throw new Exception($"Geometry build failed: {ex.Message}");
        }
        if (geometryType == "POLYGON")
            dto.RadiusM = null;

        var entity = new mst_Geofence
        {
            AccountId = dto.AccountId,
            UniqueCode = code,
            DisplayName = dto.DisplayName?.Trim(),
            Description = dto.Description,
            ClassificationCode = dto.ClassificationCode,
            ClassificationLabel = dto.ClassificationLabel,
            GeometryType = geometryType,
            RadiusM = geometryType == "CIRCLE" ? dto.RadiusM : null,
            Geom = geom,
            CoordinatesJson = dto.Coordinates == null
                ? null
                : JsonDocument.Parse(JsonSerializer.Serialize(dto.Coordinates)),

            ColorTheme = dto.ColorTheme,
            Opacity = dto.Opacity,
            Status = dto.IsEnabled ? "ENABLED" : "DISABLED",
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.GeofenceZones.Add(entity);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"DB SAVE ERROR: {ex.Message} | INNER: {ex.InnerException?.Message}");
        }

        // Sync should not break DB operation
        try
        {
            await SyncGeofenceAsync(entity, dto.Coordinates, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Geofence sync failed");
        }

        return entity.Id;
    }

    public async Task<GeofenceListUiResponseDto> GetZones(
        int page,
        int pageSize,
        string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.GeofenceZones
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(x =>
                x.UniqueCode.ToLower().Contains(s) ||
                x.DisplayName.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var enabled = await query.CountAsync(x => x.Status == "ENABLED");
        var disabled = total - enabled;

        var summary = new GeofenceSummaryDto
        {
            TotalZones = total,
            Enabled = enabled,
            Disabled = disabled
        };

        var items = await query
            .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt : x.CreatedAt)
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
                CreatedAt = x.CreatedAt
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

    public async Task<PagedResultDto<GeofenceDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null)
    {
        var result = await GetZones(page, pageSize, search);
        return result.Zones;
    }

    public async Task<GeofenceDto?> GetByIdAsync(int id)
    {
        var entity = await _db.GeofenceZones
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

    public async Task<bool> UpdateAsync(int id, UpdateGeofenceDto dto)
    {
        var entity = await _db.GeofenceZones
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        var code = dto.UniqueCode.Trim().ToUpper();

        var exists = await _db.GeofenceZones
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
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isEnabled)
    {
        var entity = await _db.GeofenceZones
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.Status = isEnabled ? "ENABLED" : "DISABLED";
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.GeofenceZones
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        List<CoordinateDto> coordinates = new();

        if (entity.CoordinatesJson != null)
        {
            coordinates = entity.CoordinatesJson
                .Deserialize<List<CoordinateDto>>() ?? new List<CoordinateDto>();
        }


        await SyncGeofenceAsync(entity, coordinates, HttpMethod.Delete);

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
    public async Task<List<GeofenceDto>> BulkCreateAsync(List<CreateGeofenceDto> items)
    {
        if (items == null || items.Count == 0)
            return new List<GeofenceDto>();

        var entities = new List<mst_Geofence>();

        foreach (var dto in items)
        {
            var code = dto.UniqueCode.Trim().ToUpper();

            var exists = await _db.GeofenceZones
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
        var request = BuildExternalPayload(entity, coordinates);

        bool success = false;
        string? error = null;

        try
        {
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
        var log = new map_geofence_sync_log
        {
            GeofenceId = entity.Id,
            PayloadJson = JsonSerializer.Serialize(request),
            IsSynced = success,
            ErrorMessage = error,
            RetryCount = success ? 0 : 1,
            LastTriedAt = DateTime.UtcNow
        };

        _db.map_geofence_sync_logs.Add(log);

        await _db.SaveChangesAsync();
    }
}