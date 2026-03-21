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

    public async Task<int> CreateAsync(CreateVehicleGeofenceMapDto dto)
    {
        var accountExists = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        var vehicleExists = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == dto.VehicleId && x.AccountId == dto.AccountId && !x.IsDeleted);

        if (!vehicleExists)
            throw new Exception("Invalid VehicleId");

        var geofenceExists = await _db.GeofenceZones
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == dto.GeofenceId && x.AccountId == dto.AccountId && !x.IsDeleted);

        if (!geofenceExists)
            throw new Exception("Invalid GeofenceId");

        var exists = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.VehicleId == dto.VehicleId &&
                x.GeofenceId == dto.GeofenceId &&
                !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Mapping already exists.");

        var entity = new map_vehicle_geofence
        {
            AccountId = dto.AccountId,
            VehicleId = dto.VehicleId,
            GeofenceId = dto.GeofenceId,
            Remarks = dto.Remarks,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
            SyncStatus = "PENDING"
        };

        _db.VehicleGeofenceMaps.Add(entity);
        await _db.SaveChangesAsync();

        await SyncVehicleGeofenceAsync(entity, HttpMethod.Post);

        return entity.Id;
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

        var duplicate = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.VehicleId == dto.VehicleId &&
                x.GeofenceId == dto.GeofenceId &&
                x.Id != id &&
                !x.IsDeleted);

        if (duplicate)
            throw new Exception("Mapping already exists");

        entity.VehicleId = dto.VehicleId;
        entity.GeofenceId = dto.GeofenceId;
        entity.Remarks = dto.Remarks;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
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
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    #endregion


    #region DELETE

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.VehicleGeofenceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

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
}
