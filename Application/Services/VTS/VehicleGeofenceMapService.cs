using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class VehicleGeofenceMapService : IVehicleGeofenceMapService
{
    private readonly IdentityDbContext _db;
    private readonly IExternalMappingApiService _external;
    private readonly ILogger<VehicleGeofenceMapService> _logger;
    private readonly ICurrentUserService _currentUser;

    public VehicleGeofenceMapService(
        IdentityDbContext db,
        IExternalMappingApiService external,
        ILogger<VehicleGeofenceMapService> logger,
        ICurrentUserService currentUser)
    {
        _db = db;
        _external = external;
        _logger = logger;
        _currentUser = currentUser;
    }

    #region CREATE

    public async Task<List<int>> CreateAsync(CreateVehicleGeofenceMapDto dto)
    {
        var accountExists = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        var vehicleIds = NormalizeIds(dto.VehicleIds, dto.VehicleId);
        if (!vehicleIds.Any())
            throw new Exception("VehicleIds are required");

        var geofenceIds = NormalizeIds(dto.GeofenceIds, dto.GeofenceId);
        if (!geofenceIds.Any())
            throw new Exception("GeofenceIds are required");

        var validVehicleIds = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x =>
                vehicleIds.Contains(x.Id) &&
                x.AccountId == dto.AccountId &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync();

        if (validVehicleIds.Count != vehicleIds.Count)
            throw new Exception("One or more VehicleIds are invalid");

        var validGeofenceIds = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x =>
                geofenceIds.Contains(x.Id) &&
                x.AccountId == dto.AccountId &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync();

        if (validGeofenceIds.Count != geofenceIds.Count)
            throw new Exception("One or more GeofenceIds are invalid");

        var requestedPairs = BuildPairs(vehicleIds, geofenceIds);

        var existingMappings = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x =>
                vehicleIds.Contains(x.VehicleId) &&
                geofenceIds.Contains(x.GeofenceId))
            .ToListAsync();

        var existingByPair = existingMappings.ToDictionary(
            x => BuildPairKey(x.VehicleId, x.GeofenceId),
            x => x,
            StringComparer.Ordinal);

        var syncEntities = new List<map_vehicle_geofence>();
        var affectedEntities = new List<map_vehicle_geofence>();

        foreach (var pair in requestedPairs)
        {
            var pairKey = BuildPairKey(pair.VehicleId, pair.GeofenceId);
            if (existingByPair.TryGetValue(pairKey, out var existingEntity))
            {
                if (!existingEntity.IsDeleted)
                    continue;

                existingEntity.IsDeleted = false;
                existingEntity.IsActive = true;
                existingEntity.Remarks = dto.Remarks;
                existingEntity.UpdatedBy = dto.CreatedBy;
                existingEntity.UpdatedByUserId = dto.CreatedByUserId;
                existingEntity.UpdatedAt = DateTime.UtcNow;
                existingEntity.SyncStatus = "PENDING";
                affectedEntities.Add(existingEntity);
                syncEntities.Add(existingEntity);
                continue;
            }

            var newEntity = new map_vehicle_geofence
            {
                AccountId = dto.AccountId,
                VehicleId = pair.VehicleId,
                GeofenceId = pair.GeofenceId,
                Remarks = dto.Remarks,
                CreatedBy = dto.CreatedBy,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                SyncStatus = "PENDING"
            };

            _db.VehicleGeofenceMaps.Add(newEntity);
            affectedEntities.Add(newEntity);
            syncEntities.Add(newEntity);
        }

        if (!affectedEntities.Any())
            throw new InvalidOperationException("All mappings already exist.");

        await _db.SaveChangesAsync();

        _ = Task.Run(async () =>
 {
     var tasks = syncEntities.Select(async entity =>
     {
         try
         {
             await SyncVehicleGeofenceAsync(entity, HttpMethod.Post);
         }
         catch
         {
         }
     });

     await Task.WhenAll(tasks);
 });

        return affectedEntities.Select(x => x.Id).ToList();
    }
    #endregion


    #region UPDATE

    public async Task<bool> UpdateAsync(int id, UpdateVehicleGeofenceMapDto dto)
    {
        var entity = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        var vehicleIds = NormalizeIds(dto.VehicleIds, dto.VehicleId);
        if (!vehicleIds.Any())
            throw new Exception("VehicleIds are required");

        var geofenceIds = NormalizeIds(dto.GeofenceIds, dto.GeofenceId);
        if (!geofenceIds.Any())
            throw new Exception("GeofenceIds are required");

        var validVehicleIds = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x =>
                vehicleIds.Contains(x.Id) &&
                x.AccountId == entity.AccountId &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync();

        if (validVehicleIds.Count != vehicleIds.Count)
            throw new Exception("One or more VehicleIds are invalid");

        var validGeofenceIds = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x =>
                geofenceIds.Contains(x.Id) &&
                x.AccountId == entity.AccountId &&
                !x.IsDeleted)
            .Select(x => x.Id)
            .ToListAsync();

        if (validGeofenceIds.Count != geofenceIds.Count)
            throw new Exception("One or more GeofenceIds are invalid");

        var requestedPairs = BuildPairs(vehicleIds, geofenceIds);
        var existingTargetMappings = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x =>
                vehicleIds.Contains(x.VehicleId) &&
                geofenceIds.Contains(x.GeofenceId))
            .ToListAsync();

        var existingByPair = existingTargetMappings.ToDictionary(
            x => BuildPairKey(x.VehicleId, x.GeofenceId),
            x => x,
            StringComparer.Ordinal);

        var primaryPair = requestedPairs[0];
        var deletedDuplicate = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x =>
                x.VehicleId == primaryPair.VehicleId &&
                x.GeofenceId == primaryPair.GeofenceId &&
                x.Id != id &&
                x.IsDeleted);

        if (deletedDuplicate != null)
            _db.VehicleGeofenceMaps.Remove(deletedDuplicate);

        var duplicate = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.VehicleId == primaryPair.VehicleId &&
                x.GeofenceId == primaryPair.GeofenceId &&
                x.Id != id &&
                !x.IsDeleted);

        if (duplicate)
            throw new Exception("Mapping already exists");

        entity.VehicleId = primaryPair.VehicleId;
        entity.GeofenceId = primaryPair.GeofenceId;
        entity.Remarks = dto.Remarks;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedByUserId = dto.UpdatedByUserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.SyncStatus = "PENDING";

        var syncEntities = new List<map_vehicle_geofence> { entity };

        foreach (var pair in requestedPairs.Skip(1))
        {
            var pairKey = BuildPairKey(pair.VehicleId, pair.GeofenceId);

            if (existingByPair.TryGetValue(pairKey, out var existingEntity))
            {
                if (existingEntity.Id == entity.Id)
                    continue;

                if (existingEntity.IsDeleted)
                {
                    existingEntity.IsDeleted = false;
                    existingEntity.CreatedBy = existingEntity.CreatedBy ?? entity.CreatedBy;
                    existingEntity.CreatedByUserId ??= entity.CreatedByUserId;
                    existingEntity.CreatedAt = existingEntity.CreatedAt == default ? DateTime.UtcNow : existingEntity.CreatedAt;
                }

                existingEntity.Remarks = dto.Remarks;
                existingEntity.IsActive = dto.IsActive;
                existingEntity.UpdatedBy = dto.UpdatedBy;
                existingEntity.UpdatedByUserId = dto.UpdatedByUserId;
                existingEntity.UpdatedAt = DateTime.UtcNow;
                existingEntity.SyncStatus = "PENDING";
                syncEntities.Add(existingEntity);
                continue;
            }

            var newEntity = new map_vehicle_geofence
            {
                AccountId = entity.AccountId,
                VehicleId = pair.VehicleId,
                GeofenceId = pair.GeofenceId,
                Remarks = dto.Remarks,
                IsActive = dto.IsActive,
                IsDeleted = false,
                CreatedBy = entity.CreatedBy,
                CreatedByUserId = entity.CreatedByUserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedBy = dto.UpdatedBy,
                UpdatedByUserId = dto.UpdatedByUserId,
                UpdatedAt = DateTime.UtcNow,
                SyncStatus = "PENDING"
            };

            _db.VehicleGeofenceMaps.Add(newEntity);
            syncEntities.Add(newEntity);
        }

        await _db.SaveChangesAsync();

        foreach (var syncEntity in syncEntities.GroupBy(x => x.Id).Select(g => g.First()))
        {
            await SyncVehicleGeofenceAsync(syncEntity, HttpMethod.Put);
        }

        return true;
    }
    #endregion


    #region STATUS

    public async Task<bool> UpdateStatusAsync(int id, bool isActive)
    {
        var entity = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        entity.IsActive = isActive;
        entity.UpdatedBy = _currentUser.AccountId;
        entity.UpdatedByUserId = _currentUser.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.SyncStatus = "PENDING";

        await _db.SaveChangesAsync();

        await SyncVehicleGeofenceAsync(entity, HttpMethod.Put);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedBy = _currentUser.AccountId;
        entity.DeletedByUserId = _currentUser.UserId;
        entity.UpdatedByUserId = _currentUser.UserId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.SyncStatus = "PENDING";

        await _db.SaveChangesAsync();

        await SyncVehicleGeofenceAsync(entity, HttpMethod.Delete);

        return true;
    }

    #endregion


    #region GET BY ID

    public async Task<VehicleGeofenceMapDto?> GetByIdAsync(int id)
    {
        return await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new VehicleGeofenceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,

                VehicleId = x.VehicleId,
                VehicleNo = x.Vehicle.VehicleNumber,

                GeofenceId = x.GeofenceId,
                GeofenceName = x.Geofence.DisplayName,
                GeometryType = x.Geofence.GeometryType,

                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,

                CreatedBy = x.CreatedBy ?? 0,
                CreatedAt = x.CreatedAt,
                UpdatedBy = x.UpdatedBy,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    #endregion


    #region LIST WITH SUMMARY

    public async Task<VehicleGeofenceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.VehicleGeofenceMaps
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle.VehicleNumber != null && x.Vehicle.VehicleNumber.ToLower().Contains(s)) ||
                (x.Geofence.DisplayName != null && x.Geofence.DisplayName.ToLower().Contains(s)));
        }

        var summaryData = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(x => x.IsActive)
            })
            .FirstOrDefaultAsync();

        var total = summaryData?.Total ?? 0;
        var active = summaryData?.Active ?? 0;

        var summary = new VehicleGeofenceAssignmentSummaryDto
        {
            TotalAssignments = total,
            Active = active,
            Inactive = total - active
        };

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VehicleGeofenceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                VehicleId = x.VehicleId,
                VehicleNo = x.Vehicle.VehicleNumber,
                GeofenceId = x.GeofenceId,
                GeofenceName = x.Geofence.DisplayName,
                GeometryType = x.Geofence.GeometryType,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedBy = x.CreatedBy ?? 0,
                CreatedAt = x.CreatedAt,
                UpdatedBy = x.UpdatedBy,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new VehicleGeofenceAssignmentListUiResponseDto
        {
            Summary = summary,
            Assignments = new PagedResultDto<VehicleGeofenceMapDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    #endregion


    #region PAGED

    public async Task<PagedResultDto<VehicleGeofenceMapDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.VehicleGeofenceMaps
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle.VehicleNumber != null && x.Vehicle.VehicleNumber.ToLower().Contains(s)) ||
                (x.Geofence.DisplayName != null && x.Geofence.DisplayName.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VehicleGeofenceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                VehicleId = x.VehicleId,
                VehicleNo = x.Vehicle.VehicleNumber,
                GeofenceId = x.GeofenceId,
                GeofenceName = x.Geofence.DisplayName,
                GeometryType = x.Geofence.GeometryType,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedBy = x.CreatedBy ?? 0,
                CreatedAt = x.CreatedAt,
                UpdatedBy = x.UpdatedBy,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new PagedResultDto<VehicleGeofenceMapDto>
        {
            Items = items,
            TotalRecords = total,
            Page = page,
            PageSize = pageSize
        };
    }

    #endregion


    #region SYNC EXTERNAL API

    private async Task SyncVehicleGeofenceAsync(
      map_vehicle_geofence entity,
      HttpMethod method)
    {
        bool success = false;
        string payloadJson = string.Empty;
        string? errorMessage = null;

        try
        {
            var vehicle = await _db.Vehicles
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.Id == entity.VehicleId);

            var geofence = await _db.GeofenceZones
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.Id == entity.GeofenceId);

            var deviceMap = await _db.VehicleDeviceMaps
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x =>
                    x.Fk_VehicleId == entity.VehicleId &&
                    x.IsActive &&
                    !x.IsDeleted);

            if (vehicle == null || geofence == null || deviceMap == null)
                return;

            var device = await _db.Devices
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.Id == deviceMap.Fk_DeviceId);

            if (device == null)
                return;

            var payload = new List<ExternalGeofenceMappingRequest>
        {
            new ExternalGeofenceMappingRequest
            {
                vehicleId = vehicle.Id.ToString(),
                vehicleNo = vehicle.VehicleNumber,
                deviceNo = device.DeviceNo,
                geofence = new List<ExternalGeofenceItem>
                {
                    new ExternalGeofenceItem
                    {
                        geoId = geofence.Id,
                        tripNo = "0",
                        geoPoint = "START"
                    }
                }
            }
        };

            // ✅ Serialize payload for logging
            payloadJson = System.Text.Json.JsonSerializer.Serialize(payload);

            success = await _external.SendVehicleGeofenceMappingAsync(
                payload,
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
                errorMessage = "External API failure";
                entity.SyncError = errorMessage;
            }
        }
        catch (Exception ex)
        {
            entity.SyncStatus = "FAILED";
            errorMessage = ex.Message;
            entity.SyncError = errorMessage;

            _logger.LogError(ex, "Vehicle geofence sync failed");
        }

        // ✅ Save main entity update first
        await _db.SaveChangesAsync();

        // ✅ Insert log entry
        var log = new map_vehicle_geofence_sync_log
        {
            MappingId = entity.Id,
            PayloadJson = payloadJson,
            IsSynced = success,
            ErrorMessage = errorMessage,
            RetryCount = 0, // initial attempt
            LastTriedAt = DateTime.UtcNow
        };

        _db.map_vehicle_geofence_sync_logs.Add(log);
        await _db.SaveChangesAsync();
    }

    #endregion

    private static List<int> NormalizeIds(IEnumerable<int>? ids, int? singleId)
    {
        var normalized = (ids ?? Enumerable.Empty<int>())
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (singleId.HasValue && singleId.Value > 0 && !normalized.Contains(singleId.Value))
            normalized.Add(singleId.Value);

        return normalized;
    }

    private static List<(int VehicleId, int GeofenceId)> BuildPairs(
        IReadOnlyCollection<int> vehicleIds,
        IReadOnlyCollection<int> geofenceIds)
    {
        var pairs = new List<(int VehicleId, int GeofenceId)>();

        foreach (var vehicleId in vehicleIds)
        {
            foreach (var geofenceId in geofenceIds)
            {
                pairs.Add((vehicleId, geofenceId));
            }
        }

        return pairs;
    }

    private static string BuildPairKey(int vehicleId, int geofenceId)
    {
        return $"{vehicleId}:{geofenceId}";
    }
}
