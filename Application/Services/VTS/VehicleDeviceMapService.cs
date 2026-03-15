using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class VehicleDeviceMapService : IVehicleDeviceMapService
{
    private readonly IdentityDbContext _db;
    private readonly IExternalMappingApiService _externalApi;

    public VehicleDeviceMapService(
        IdentityDbContext db,
        IExternalMappingApiService externalApi)
    {
        _db = db;
        _externalApi = externalApi;
    }
    public async Task<int> CreateAsync(CreateVehicleDeviceMapDto dto)
    {
        // ✅ Account validation
        var accountExists = await _db.Accounts
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        // ✅ Vehicle validation
        var vehicleExists = await _db.Vehicles
            .AnyAsync(x => x.Id == dto.VehicleId && !x.IsDeleted);

        if (!vehicleExists)
            throw new Exception("Invalid VehicleId");

        // ✅ Device validation
        var deviceExists = await _db.Devices
            .AnyAsync(x => x.Id == dto.DeviceId && !x.IsDeleted);

        if (!deviceExists)
            throw new Exception("Invalid DeviceId");

        // ✅ Business rule: device already assigned
        var exists = await _db.VehicleDeviceMaps
            .AnyAsync(x => x.Fk_DeviceId == dto.DeviceId && x.IsActive && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device already assigned.");

        var entity = new map_vehicle_device
        {
            AccountId = dto.AccountId,
            Fk_VehicleId = dto.VehicleId,
            Fk_DeviceId = dto.DeviceId,
            fk_devicetypeid = dto.DeviceTypeId,
            fk_simid = dto.SimId,
            simnno = dto.SimNumber,
            Remarks = dto.Remarks,
            InstallationDate = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
            createdBy = dto.CreatedBy,
            createdAt = DateTime.UtcNow
        };

        _db.VehicleDeviceMaps.Add(entity);
        await _db.SaveChangesAsync();

        await SendExternalMapping(entity);

        return entity.Id;
    }

    private async Task SendExternalMapping(map_vehicle_device entity)
    {
        var vehicle = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == entity.Fk_VehicleId);

        var device = await _db.Devices
            .FirstOrDefaultAsync(x => x.Id == entity.Fk_DeviceId);

        var account = await _db.Accounts
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
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            throw new Exception("Mapping not found");

        // Validate device
        var deviceExists = await _db.Devices
            .AnyAsync(x => x.Id == dto.DeviceId && !x.IsDeleted);

        if (!deviceExists)
            throw new Exception("Invalid DeviceId");

        // Validate vehicle
        var vehicleExists = await _db.Vehicles
            .AnyAsync(x => x.Id == dto.VehicleId && !x.IsDeleted);

        if (!vehicleExists)
            throw new Exception("Invalid VehicleId");

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

    public async Task<bool> UpdateStatusAsync(int id, bool isActive)
    {
        var entity = await _db.VehicleDeviceMaps
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
                InstallationDate = x.InstallationDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<VehicleDeviceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        var query = _db.VehicleDeviceMaps
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle.VehicleNumber ?? "").ToLower().Contains(s) ||
                (x.Device.DeviceNo ?? "").ToLower().Contains(s) ||
                (x.simnno ?? "").ToLower().Contains(s) ||
                (x.Remarks ?? "").ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var active = await query.CountAsync(x => x.IsActive);

        var summary = new VehicleDeviceAssignmentSummaryDto
        {
            TotalAssignments = total,
            Active = active,
            WithIssues = total - active
        };
        var items = await query
    .OrderByDescending(x => x.Id)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
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
        InstallationDate = x.InstallationDate
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
        var query = _db.VehicleDeviceMaps
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Vehicle.VehicleNumber ?? "").ToLower().Contains(s) ||
                (x.Device.DeviceNo ?? "").ToLower().Contains(s) ||
                (x.simnno ?? "").ToLower().Contains(s) ||
                (x.Remarks ?? "").ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                InstallationDate = x.InstallationDate
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
        }).ToList();

        _db.VehicleDeviceMaps.AddRange(entities);
        await _db.SaveChangesAsync();

        var deviceTypeNames = await _db.DeviceTypes
            .Where(x => items.Select(i => i.DeviceTypeId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);

        return entities.Select(x => new VehicleDeviceMapDto
        {
            Id = x.Id,
            VehicleId = x.Fk_VehicleId,
            DeviceId = x.Fk_DeviceId,
            DeviceTypeId = x.fk_devicetypeid,
            DeviceTypeName = deviceTypeNames.GetValueOrDefault(x.fk_devicetypeid)
        }).ToList();
    }
}
