using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class VehicleDeviceMapService : IVehicleDeviceMapService
{
    private readonly IdentityDbContext _db;
    //private readonly IVtsExternalApiEnqueueService _externalSyncEnqueueService;
    private readonly IExternalMappingApiService _externalApi;
    private readonly ICurrentUserService _currentUser;

    public VehicleDeviceMapService(
        IdentityDbContext db,
        IExternalMappingApiService externalApi,
        ICurrentUserService currentUser)
    {
        _db = db;
        _externalApi = externalApi;
        _currentUser = currentUser;
    }
    public async Task<int> CreateAsync(CreateVehicleDeviceMapDto dto)
    {
        // ✅ Account validation
        var accountExists = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        // ✅ Vehicle validation
        var vehicleExists = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == dto.VehicleId && x.AccountId == dto.AccountId && !x.IsDeleted);

        if (!vehicleExists)
            throw new Exception("Invalid VehicleId");

        // ✅ Device validation
        var deviceExists = await _db.Devices
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == dto.DeviceId && x.AccountId == dto.AccountId && !x.IsDeleted);

        if (!deviceExists)
            throw new Exception("Invalid DeviceId");

        // ✅ Business rule: device already assigned
        await ValidateActiveDeviceAssignmentAsync(dto.DeviceId, excludeMappingId: null);
        await ValidateSimAssignmentAsync(dto.AccountId, dto.SimId, excludeMappingId: null);

        var entity = new map_vehicle_device
        {
            AccountId = dto.AccountId,
            Fk_VehicleId = dto.VehicleId,
            Fk_DeviceId = dto.DeviceId,
            fk_devicetypeid = dto.DeviceTypeId,
            fk_simid = dto.SimId,
            simnno = dto.SimNumber ?? string.Empty,
            Remarks = dto.Remarks,
            InstallationDate = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
            createdBy = dto.CreatedBy,
            createdAt = DateTime.UtcNow
        };

        _db.VehicleDeviceMaps.Add(entity);
        await _db.SaveChangesAsync();

        //await _externalSyncEnqueueService.EnqueueVehicleDeviceMappingAsync(entity);
        await SendExternalMapping(entity);
        return entity.Id;
    }

    private async Task SendExternalMapping(map_vehicle_device entity)
    {
        var vehicle = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == entity.Fk_VehicleId);

        var device = await _db.Devices
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == entity.Fk_DeviceId);

        var account = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == entity.AccountId);

        if (vehicle == null || device == null || account == null)
            return; // safety guard

        var request = new ExternalVehicleMappingRequest
        {
            VehicleId = vehicle.Id.ToString(),
            VehicleNo = vehicle.VehicleNumber,
            DeviceNo = device.DeviceNo,
            Imei = device.DeviceImeiOrSerial,
            DeviceType = entity.fk_devicetypeid.ToString(),
            OrgName = account.AccountName,
            OrgId = entity.AccountId
        };
        var payload = new List<ExternalVehicleMappingRequest> { request };

        var success = false;
        string? error = null;

        try
        {
            success = await _externalApi.SendVehicleMappingAsync(payload);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        var log = new map_vehicle_device_sync_log
        {
            MappingId = entity.Id,
            PayloadJson = JsonSerializer.Serialize(payload),
            IsSynced = success,
            ErrorMessage = error,
            RetryCount = success ? 0 : 1,
            LastTriedAt = DateTime.UtcNow
        };

        _db.map_vehicle_device_sync_logs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateVehicleDeviceMapDto dto)
    {
        var entity = await _db.VehicleDeviceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        // Validate device
        var deviceExists = await _db.Devices
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == dto.DeviceId && x.AccountId == entity.AccountId && !x.IsDeleted);

        if (!deviceExists)
            throw new Exception("Invalid DeviceId");

        await ValidateActiveDeviceAssignmentAsync(dto.DeviceId, id);

        // Validate vehicle
        var vehicleExists = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == dto.VehicleId && x.AccountId == entity.AccountId && !x.IsDeleted);

        if (!vehicleExists)
            throw new Exception("Invalid VehicleId");

        await ValidateSimAssignmentAsync(entity.AccountId, dto.SimId, id);

        entity.Fk_DeviceId = dto.DeviceId;
        entity.Fk_VehicleId = dto.VehicleId;
        entity.fk_devicetypeid = dto.DeviceTypeId;
        entity.fk_simid = dto.SimId;
        entity.simnno = dto.SimNumber;
        entity.Remarks = dto.Remarks;
        entity.IsActive = dto.IsActive;
        entity.updatedBy = dto.UpdatedBy;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task ValidateActiveDeviceAssignmentAsync(int deviceId, int? excludeMappingId)
    {
        var existingDeviceMapping = await _db.VehicleDeviceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.Fk_DeviceId == deviceId && x.IsActive && !x.IsDeleted)
            .Where(x => !excludeMappingId.HasValue || x.Id != excludeMappingId.Value)
            .Select(x => new { x.Id })
            .FirstOrDefaultAsync();

        if (existingDeviceMapping != null)
            throw new InvalidOperationException("Device already assigned.");
    }

    private async Task ValidateSimAssignmentAsync(int accountId, int simId, int? excludeMappingId)
    {
        var simExists = await _db.Sims
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.SimId == simId &&
                x.AccountId == accountId &&
                !x.IsDeleted &&
                x.IsActive);

        if (!simExists)
            throw new Exception("Invalid or inactive SIM.");

        var existingSimMapping = await (
            from map in _db.VehicleDeviceMaps.ApplyAccountHierarchyFilter(_currentUser)
            join device in _db.Devices on map.Fk_DeviceId equals device.Id
            where map.fk_simid == simId
                  && (!excludeMappingId.HasValue || map.Id != excludeMappingId.Value)
                  && map.IsActive
                  && !map.IsDeleted
            select new
            {
                map.Id,
                DeviceId = device.Id,
                DeviceNo = device.DeviceNo,
                DeviceName = device.DeviceImeiOrSerial
            }
        ).FirstOrDefaultAsync();

        if (existingSimMapping != null)
        {
            throw new InvalidOperationException(
                $"SIM already assigned with device: {existingSimMapping.DeviceNo} - {existingSimMapping.DeviceName}");
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isActive)
    {
        var entity = await _db.VehicleDeviceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        entity.IsActive = isActive;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.VehicleDeviceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<VehicleDeviceMapDto?> GetByIdAsync(int id)
    {
        return await _db.VehicleDeviceMaps
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new VehicleDeviceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                VehicleId = x.Fk_VehicleId,
                VehicleNo = x.Vehicle.VehicleNumber,
                DeviceId = x.Fk_DeviceId,
                DeviceNo = x.Device.DeviceNo,
                DeviceTypeId = x.fk_devicetypeid,
                DeviceTypeName = _db.DeviceTypes
                    .Where(dt => dt.Id == x.fk_devicetypeid)
                    .Select(dt => dt.Name)
                    .FirstOrDefault(),
                SimId = x.fk_simid,
                SimNumber = x.simnno,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                InstallationDate = x.InstallationDate,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<VehicleDeviceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.VehicleDeviceMaps
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle.VehicleNumber != null && x.Vehicle.VehicleNumber.ToLower().Contains(s)) ||
                (x.Device.DeviceNo != null && x.Device.DeviceNo.ToLower().Contains(s)) ||
                (x.simnno != null && x.simnno.ToLower().Contains(s)) ||
                (x.Remarks != null && x.Remarks.ToLower().Contains(s)));
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

        var summary = new VehicleDeviceAssignmentSummaryDto
        {
            TotalAssignments = total,
            Active = active,
            WithIssues = total - active
        };
        var items = await query
            .OrderByDescending(x => x.updatedAt ?? x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VehicleDeviceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                VehicleId = x.Fk_VehicleId,
                VehicleNo = x.Vehicle?.VehicleNumber ?? string.Empty,
                DeviceId = x.Fk_DeviceId,
                DeviceNo = x.Device?.DeviceNo ?? string.Empty,
                DeviceTypeId = x.fk_devicetypeid,
                DeviceTypeName = _db.DeviceTypes
                    .Where(dt => dt.Id == x.fk_devicetypeid)
                    .Select(dt => dt.Name)
                    .FirstOrDefault(),
                SimId = x.fk_simid,
                SimNumber = x.simnno,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                InstallationDate = x.InstallationDate,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt
            })
            .ToListAsync();
        return new VehicleDeviceAssignmentListUiResponseDto
        {
            Summary = summary,
            Assignments = new PagedResultDto<VehicleDeviceMapDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public async Task<PagedResultDto<VehicleDeviceMapDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.VehicleDeviceMaps
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle != null && x.Vehicle.VehicleNumber != null && x.Vehicle.VehicleNumber.ToLower().Contains(s)) ||
                (x.Device != null && x.Device.DeviceNo != null && x.Device.DeviceNo.ToLower().Contains(s)) ||
                (x.simnno != null && x.simnno.ToLower().Contains(s)) ||
                (x.Remarks != null && x.Remarks.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.updatedAt ?? x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VehicleDeviceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                VehicleId = x.Fk_VehicleId,
                VehicleNo = x.Vehicle?.VehicleNumber ?? string.Empty,
                DeviceId = x.Fk_DeviceId,
                DeviceNo = x.Device?.DeviceNo ?? string.Empty,
                DeviceTypeId = x.fk_devicetypeid,
                DeviceTypeName = _db.DeviceTypes
                    .Where(dt => dt.Id == x.fk_devicetypeid)
                    .Select(dt => dt.Name)
                    .FirstOrDefault(),
                SimId = x.fk_simid,
                SimNumber = x.simnno,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                InstallationDate = x.InstallationDate,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt
            })
            .ToListAsync();

        return new PagedResultDto<VehicleDeviceMapDto>
        {
            Items = items,
            TotalRecords = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<VehicleDeviceMapDto>> BulkCreateAsync(List<CreateVehicleDeviceMapDto> items)
    {
        var entities = items.Select(dto => new map_vehicle_device
        {
            AccountId = dto.AccountId,
            Fk_VehicleId = dto.VehicleId,
            Fk_DeviceId = dto.DeviceId,
            fk_devicetypeid = dto.DeviceTypeId,
            fk_simid = dto.SimId,
            simnno = dto.SimNumber,
            Remarks = dto.Remarks,
            InstallationDate = DateTime.UtcNow,
            createdBy = dto.CreatedBy,
            createdAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
        }).ToList();

        _db.VehicleDeviceMaps.AddRange(entities);
        await _db.SaveChangesAsync();
        // await _externalSyncEnqueueService.EnqueueVehicleDeviceMappingsAsync(entities);

        var deviceTypeNames = await _db.DeviceTypes
            .Where(x => items.Select(i => i.DeviceTypeId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        return entities.Select(x => new VehicleDeviceMapDto
        {
            Id = x.Id,
            AccountId = x.AccountId,
            VehicleId = x.Fk_VehicleId,
            DeviceId = x.Fk_DeviceId,
            DeviceTypeId = x.fk_devicetypeid,
            DeviceTypeName = deviceTypeNames.GetValueOrDefault(x.fk_devicetypeid),
            SimId = x.fk_simid,
            SimNumber = x.simnno,
            Remarks = x.Remarks,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            InstallationDate = x.InstallationDate,
            CreatedBy = x.createdBy,
            CreatedAt = x.createdAt,
            UpdatedBy = x.updatedBy,
            UpdatedAt = x.updatedAt
        }).ToList();
    }
}
