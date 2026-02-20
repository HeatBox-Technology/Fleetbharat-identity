using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

public class DeviceService : IDeviceService
{
    private readonly IdentityDbContext _db;

    public DeviceService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(DeviceDto dto)
    {
        var imei = dto.DeviceImeiOrSerial.Trim();

        var exists = await _db.Devices
            .AnyAsync(x => x.DeviceImeiOrSerial == imei && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device already exists");

        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var entity = new mst_device
            {
                AccountId = dto.AccountId,
                ManufactureID = dto.ManufactureID,
                DeviceTypeId = dto.DeviceTypeId,
                DeviceNo = dto.DeviceNo,
                DeviceImeiOrSerial = imei,
                DeviceStatus = dto.DeviceStatus ?? "Active",
                createdBy = dto.CreatedBy,
                createdAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Devices.Add(entity);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            return entity.Id;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<DeviceListUiResponseDto> GetDevices(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.Devices
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.DeviceImeiOrSerial.ToLower().Contains(s) ||
                x.DeviceNo.ToLower().Contains(s));
        }

        // Summary
        var totalDevices = await query.CountAsync();

        var inService = await query.CountAsync(x =>
            x.DeviceStatus.ToLower() == "active" ||
            x.DeviceStatus.ToLower() == "inservice");

        var outOfService = totalDevices - inService;

        var summary = new DeviceCardSummaryDto
        {
            TotalDevices = totalDevices,
            InService = inService,
            OutOfService = outOfService
        };

        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DeviceDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                ManufactureID = x.ManufactureID,
                DeviceTypeId = x.DeviceTypeId,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial,
                DeviceStatus = x.DeviceStatus,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt,
                IsDeleted = x.IsDeleted
            })
            .ToListAsync();

        return new DeviceListUiResponseDto
        {
            Summary = summary,
            Devices = new PagedResultDto<DeviceDto>
            {
                Items = items,
                TotalRecords = totalDevices,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    public async Task<DeviceDto?> GetByIdAsync(int id)
    {
        return await _db.Devices
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new DeviceDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                ManufactureID = x.ManufactureID,
                DeviceTypeId = x.DeviceTypeId,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial,
                DeviceStatus = x.DeviceStatus,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt,
                IsDeleted = x.IsDeleted
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, DeviceDto dto)
    {
        var entity = await _db.Devices
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.AccountId = dto.AccountId;
        entity.ManufactureID = dto.ManufactureID;
        entity.DeviceTypeId = dto.DeviceTypeId;
        entity.DeviceNo = dto.DeviceNo;
        entity.DeviceImeiOrSerial = dto.DeviceImeiOrSerial.Trim();
        entity.DeviceStatus = dto.DeviceStatus;
        entity.updatedBy = dto.UpdatedBy;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, string status)
    {
        var entity = await _db.Devices
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.DeviceStatus = status;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.Devices
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        // Soft Delete
        entity.IsDeleted = true;
        entity.updatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<DeviceDto>> BulkCreateAsync(List<DeviceDto> devices)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var entities = devices.Select(dto => new mst_device
            {
                AccountId = dto.AccountId,
                ManufactureID = dto.ManufactureID,
                DeviceTypeId = dto.DeviceTypeId,
                DeviceNo = dto.DeviceNo,
                DeviceImeiOrSerial = dto.DeviceImeiOrSerial.Trim(),
                DeviceStatus = dto.DeviceStatus ?? "Active",
                createdBy = dto.CreatedBy,
                createdAt = DateTime.UtcNow,
                IsDeleted = false
            }).ToList();

            _db.Devices.AddRange(entities);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            return entities.Select(x => new DeviceDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                ManufactureID = x.ManufactureID,
                DeviceTypeId = x.DeviceTypeId,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial,
                DeviceStatus = x.DeviceStatus,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt
            }).ToList();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
