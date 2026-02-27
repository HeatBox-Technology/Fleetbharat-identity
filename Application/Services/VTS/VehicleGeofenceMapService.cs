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

    public VehicleGeofenceMapService(
        IdentityDbContext db,
        IExternalMappingApiService external,
        ILogger<VehicleGeofenceMapService> logger)
    {
        _db = db;
        _external = external;
        _logger = logger;
    }

    #region CREATE

    public async Task<int> CreateAsync(CreateVehicleGeofenceMapDto dto)
    {
        var accountExists = await _db.Accounts
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        var vehicleExists = await _db.Vehicles
            .AnyAsync(x => x.Id == dto.VehicleId && !x.IsDeleted);

        if (!vehicleExists)
            throw new Exception("Invalid VehicleId");

        var geofenceExists = await _db.GeofenceZones
            .AnyAsync(x => x.Id == dto.GeofenceId && !x.IsDeleted);

        if (!geofenceExists)
            throw new Exception("Invalid GeofenceId");

        var exists = await _db.VehicleGeofenceMaps
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
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        var duplicate = await _db.VehicleGeofenceMaps.AnyAsync(x =>
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
        var query = _db.VehicleGeofenceMaps
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.Vehicle.VehicleNumber, $"%{search}%") ||
                EF.Functions.ILike(x.Geofence.DisplayName, $"%{search}%"));
        }

        var total = await query.CountAsync();
        var active = await query.CountAsync(x => x.IsActive);

        var summary = new VehicleGeofenceAssignmentSummaryDto
        {
            TotalAssignments = total,
            Active = active,
            Inactive = total - active
        };

        var items = await query
            .OrderByDescending(x => x.Id)
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
        var query = _db.VehicleGeofenceMaps
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.Vehicle.VehicleNumber, $"%{search}%") ||
                EF.Functions.ILike(x.Geofence.DisplayName, $"%{search}%"));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
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

        try
        {
            var vehicle = await _db.Vehicles
                .FirstOrDefaultAsync(x => x.Id == entity.VehicleId);

            var geofence = await _db.GeofenceZones
                .FirstOrDefaultAsync(x => x.Id == entity.GeofenceId);

            var deviceMap = await _db.VehicleDeviceMaps
                .FirstOrDefaultAsync(x =>
                    x.Fk_VehicleId == entity.VehicleId &&
                    x.IsActive &&
                    !x.IsDeleted);

            if (vehicle == null || geofence == null || deviceMap == null)
                return;

            var device = await _db.Devices
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
                entity.SyncError = "External API failure";
            }
        }
        catch (Exception ex)
        {
            entity.SyncStatus = "FAILED";
            entity.SyncError = ex.Message;

            _logger.LogError(ex, "Vehicle geofence sync failed");
        }

        await _db.SaveChangesAsync();
    }

    #endregion
}