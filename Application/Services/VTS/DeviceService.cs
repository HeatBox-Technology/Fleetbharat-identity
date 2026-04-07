using Application.DTOs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;

public class DeviceService : IDeviceService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeviceService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateDeviceDto dto)
    {
        var imei = dto.DeviceImeiOrSerial.Trim();

        var exists = await _db.Devices
            .AnyAsync(x => x.DeviceImeiOrSerial == imei && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device already exists");

        var entity = new mst_device
        {
            AccountId = dto.AccountId,
            ManufactureID = dto.ManufacturerId,
            DeviceTypeId = dto.DeviceTypeId,
            DeviceNo = dto.DeviceNo,
            DeviceImeiOrSerial = imei,
            DeviceStatus = "Active",
            createdBy = dto.CreatedBy,
            createdAt = DateTime.UtcNow,
            IsDeleted = false,
            IsActive = true
        };

        _db.Devices.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
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
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.DeviceImeiOrSerial != null && x.DeviceImeiOrSerial.ToLower().Contains(s)) ||
                (x.DeviceNo != null && x.DeviceNo.ToLower().Contains(s)));
        }

        var summaryData = await query
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(x =>
                    x.DeviceStatus != null &&
                    (x.DeviceStatus.ToLower() == "active" || x.DeviceStatus.ToLower() == "inservice"))
            })
            .FirstOrDefaultAsync();

        var totalDevices = summaryData?.Total ?? 0;
        var inService = summaryData?.Active ?? 0;

        var outOfService = totalDevices - inService;

        var summary = new DeviceCardSummaryDto
        {
            TotalDevices = totalDevices,
            InService = inService,
            OutOfService = outOfService
        };

        var items = await query
            .OrderByDescending(x => x.updatedAt ?? x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DeviceDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                ManufacturerId = x.ManufactureID,
                DeviceTypeId = x.DeviceTypeId,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial,
                DeviceStatus = x.DeviceStatus,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt,
                IsDeleted = x.IsDeleted,
                IsActive = x.IsActive
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
                ManufacturerId = x.ManufactureID,
                DeviceTypeId = x.DeviceTypeId,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial,
                DeviceStatus = x.DeviceStatus,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt,
                IsDeleted = x.IsDeleted,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateDeviceDto dto)
    {
        var entity = await _db.Devices
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.ManufactureID = dto.ManufacturerId;
        entity.DeviceTypeId = dto.DeviceTypeId;
        entity.DeviceNo = dto.DeviceNo;
        entity.DeviceImeiOrSerial = dto.DeviceImeiOrSerial.Trim();
        entity.DeviceStatus = dto.DeviceStatus;
        entity.IsActive = dto.IsActive;
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

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.updatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResultDto<DeviceDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Devices
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.DeviceImeiOrSerial != null && x.DeviceImeiOrSerial.ToLower().Contains(s)) ||
                (x.DeviceNo != null && x.DeviceNo.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync();

        var data = await query
            .OrderByDescending(x => x.updatedAt ?? x.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DeviceDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                ManufacturerId = x.ManufactureID,
                DeviceTypeId = x.DeviceTypeId,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial,
                DeviceStatus = x.DeviceStatus,
                CreatedBy = x.createdBy,
                CreatedAt = x.createdAt,
                UpdatedBy = x.updatedBy,
                UpdatedAt = x.updatedAt
            })
            .ToListAsync();

        return new PagedResultDto<DeviceDto>
        {
            Items = data,
            TotalRecords = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<List<DeviceDto>> BulkCreateAsync(List<CreateDeviceDto> devices)
    {
        var entities = devices.Select(dto => new mst_device
        {
            AccountId = dto.AccountId,
            ManufactureID = dto.ManufacturerId,
            DeviceTypeId = dto.DeviceTypeId,
            DeviceNo = dto.DeviceNo,
            DeviceImeiOrSerial = dto.DeviceImeiOrSerial.Trim(),
            DeviceStatus = "Active",
            createdBy = dto.CreatedBy,
            createdAt = DateTime.UtcNow,
            IsDeleted = false,
            IsActive = true
        }).ToList();

        _db.Devices.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(x => new DeviceDto
        {
            Id = x.Id,
            AccountId = x.AccountId,
            ManufacturerId = x.ManufactureID,
            DeviceTypeId = x.DeviceTypeId,
            DeviceNo = x.DeviceNo,
            DeviceImeiOrSerial = x.DeviceImeiOrSerial,
            DeviceStatus = x.DeviceStatus,
            CreatedBy = x.createdBy,
            CreatedAt = x.createdAt,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<byte[]> ExportdeviceCsvAsync(int? accountId, string? search)
    {
        var deviceQuery = _db.Devices
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser)
            .AsQueryable();

        if (accountId.HasValue)
        {
            deviceQuery = deviceQuery.Where(x => x.AccountId == accountId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            deviceQuery = deviceQuery.Where(x =>
                (!string.IsNullOrEmpty(x.DeviceNo) && x.DeviceNo.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.DeviceImeiOrSerial) && x.DeviceImeiOrSerial.ToLower().Contains(s)) ||
                (!string.IsNullOrEmpty(x.DeviceStatus) && x.DeviceStatus.ToLower().Contains(s))
            );
        }

        var query =
            from d in deviceQuery
            join a in _db.Accounts
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ApplyAccountHierarchyFilter(_currentUser)
            on d.AccountId equals a.AccountId
            select new
            {
                a.AccountName,
                d.DeviceNo,
                d.DeviceImeiOrSerial,
                d.DeviceStatus,
                LastUpdated = d.updatedAt ?? d.createdAt
            };

        var rows = await query
            .OrderByDescending(x => x.LastUpdated)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Account,Device Number,Device IMEI/Serial,Status,Last Updated");

        foreach (var d in rows)
        {
            var lastUpdated = d.LastUpdated != default(DateTime)
                ? d.LastUpdated.ToLocalTime().ToString("dd/MM/yyyy, hh:mm tt")
                : "";

            sb.AppendLine(
                $"\"{d.AccountName}\"," +
                $"\"{d.DeviceNo}\"," +
                $"\"{d.DeviceImeiOrSerial}\"," +
                $"\"{d.DeviceStatus}\"," +
                $"\"{lastUpdated}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
