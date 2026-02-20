using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class VehicleDeviceMapService : IVehicleDeviceMapService
{
    private readonly IdentityDbContext _db;

    public VehicleDeviceMapService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(VehicleDeviceMapDto dto)
    {
        var entity = new map_vehicle_device
        {
            AccountId = dto.AccountId,
            Fk_VehicleId = dto.Fk_VehicleId,
            fk_devicetypeid = dto.fk_devicetypeid,
            Fk_DeviceId = dto.Fk_DeviceId,
            fk_simid = dto.fk_simid,
            simnno = dto.simnno,
            Remarks = dto.Remarks,
            IsActive = dto.IsActive,
            IsDeleted = 0,
            InstallationDate = dto.InstallationDate == default
                ? DateTime.UtcNow
                : dto.InstallationDate,
            createdBy = dto.createdBy,
            createdAt = DateTime.UtcNow
        };

        _db.VehicleDeviceMaps.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<VehicleDeviceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        long? accountId,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.VehicleDeviceMaps
            .AsNoTracking()
            .Where(x => x.IsDeleted == 0)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        // 🔹 Summary
        var totalAssignments = await query.CountAsync();
        var active = await query.CountAsync(x => x.IsActive == 1);
        var issues = await query.CountAsync(x => x.IsActive == 0);

        var summary = new VehicleDeviceAssignmentSummaryDto
        {
            TotalAssignments = totalAssignments,
            Active = active,
            WithIssues = issues
        };

        // 🔹 Pagination
        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VehicleDeviceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Fk_VehicleId = x.Fk_VehicleId,
                fk_devicetypeid = x.fk_devicetypeid,
                Fk_DeviceId = x.Fk_DeviceId,
                fk_simid = x.fk_simid,
                simnno = x.simnno,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                InstallationDate = x.InstallationDate,
                createdBy = x.createdBy,
                createdAt = x.createdAt,
                updatedBy = x.updatedBy,
                updatedAt = x.updatedAt
            })
            .ToListAsync();

        return new VehicleDeviceAssignmentListUiResponseDto
        {
            Summary = summary,
            Assignments = new PagedResultDto<VehicleDeviceMapDto>
            {
                Items = items,
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public async Task<VehicleDeviceMapDto?> GetByIdAsync(int id)
    {
        return await _db.VehicleDeviceMaps
            .Where(x => x.Id == id && x.IsDeleted == 0)
            .Select(x => new VehicleDeviceMapDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Fk_VehicleId = x.Fk_VehicleId,
                fk_devicetypeid = x.fk_devicetypeid,
                Fk_DeviceId = x.Fk_DeviceId,
                fk_simid = x.fk_simid,
                simnno = x.simnno,
                Remarks = x.Remarks,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                InstallationDate = x.InstallationDate,
                createdBy = x.createdBy,
                createdAt = x.createdAt,
                updatedBy = x.updatedBy,
                updatedAt = x.updatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, VehicleDeviceMapDto dto)
    {
        var entity = await _db.VehicleDeviceMaps
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == 0);

        if (entity == null) return false;

        entity.AccountId = dto.AccountId;
        entity.Fk_VehicleId = dto.Fk_VehicleId;
        entity.fk_devicetypeid = dto.fk_devicetypeid;
        entity.Fk_DeviceId = dto.Fk_DeviceId;
        entity.fk_simid = dto.fk_simid;
        entity.simnno = dto.simnno;
        entity.Remarks = dto.Remarks;
        entity.IsActive = dto.IsActive;
        entity.updatedBy = dto.updatedBy;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, int isActive)
    {
        var entity = await _db.VehicleDeviceMaps
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted == 0);

        if (entity == null) return false;

        entity.IsActive = isActive;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.VehicleDeviceMaps
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) return false;

        entity.IsDeleted = 1;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<VehicleDeviceMapDto>> BulkCreateAsync(List<VehicleDeviceMapDto> items)
    {
        var entities = items.Select(dto => new map_vehicle_device
        {
            AccountId = dto.AccountId,
            Fk_VehicleId = dto.Fk_VehicleId,
            fk_devicetypeid = dto.fk_devicetypeid,
            Fk_DeviceId = dto.Fk_DeviceId,
            fk_simid = dto.fk_simid,
            simnno = dto.simnno,
            Remarks = dto.Remarks,
            IsActive = dto.IsActive,
            IsDeleted = 0,
            InstallationDate = DateTime.UtcNow,
            createdBy = dto.createdBy,
            createdAt = DateTime.UtcNow
        }).ToList();

        _db.VehicleDeviceMaps.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(x => new VehicleDeviceMapDto
        {
            Id = x.Id,
            AccountId = x.AccountId,
            Fk_VehicleId = x.Fk_VehicleId,
            Fk_DeviceId = x.Fk_DeviceId
        }).ToList();
    }
}
