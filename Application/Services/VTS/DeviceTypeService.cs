using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Domain.Entities;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

public class DeviceTypeService : IDeviceTypeService
{
    private readonly IdentityDbContext _db;

    public DeviceTypeService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateDeviceTypeDto dto)
    {
        var code = dto.Code.Trim().ToUpper();

        var exists = await _db.DeviceTypes
            .AnyAsync(x => x.Code == code && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device type already exists.");

        var entity = new mst_device_type
        {
            Code = code,
            Name = dto.Name.Trim(),
            oemmanufacturerid = dto.oemmanufacturerId,
            Description = dto.Description,
            IsEnabled = true,
            IsActive = true,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        _db.DeviceTypes.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<DeviceTypeListUiResponseDto> GetDeviceTypes(
        int page,
        int pageSize,
        string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.DeviceTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(x =>
                x.Code.ToLower().Contains(s) ||
                x.Name.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var enabled = await query.CountAsync(x => x.IsEnabled);
        var disabled = total - enabled;

        var summary = new DeviceTypeSummaryDto
        {
            TotalDeviceTypes = total,
            Enabled = enabled,
            Disabled = disabled
        };

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DeviceTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                UpdatedBy = x.UpdatedBy,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new DeviceTypeListUiResponseDto
        {
            Summary = summary,
            DeviceTypes = new PagedResultDto<DeviceTypeDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    public async Task<PagedResultDto<DeviceTypeDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null)
    {
        var result = await GetDeviceTypes(page, pageSize, search);
        return result.DeviceTypes;
    }

    public async Task<List<DeviceTypeDto>> GetAllAsync()
    {
        return await _db.DeviceTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.IsEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new DeviceTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name
            })
            .ToListAsync();
    }

    public async Task<DeviceTypeDto?> GetByIdAsync(int id)
    {
        return await _db.DeviceTypes
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new DeviceTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                IsActive = x.IsActive,
                CreatedBy = x.CreatedBy,
                CreatedAt = x.CreatedAt,
                UpdatedBy = x.UpdatedBy,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateDeviceTypeDto dto)
    {
        var entity = await _db.DeviceTypes
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        var code = dto.Code.Trim().ToUpper();

        var exists = await _db.DeviceTypes
            .AnyAsync(x => x.Code == code && x.Id != id && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device type already exists.");

        entity.Code = code;
        entity.Name = dto.Name.Trim();
        entity.oemmanufacturerid = dto.oemmanufacturerId;
        entity.Description = dto.Description;
        entity.IsEnabled = dto.IsEnabled;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isEnabled)
    {
        var entity = await _db.DeviceTypes
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.IsEnabled = isEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.DeviceTypes
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<DeviceTypeDto>> BulkCreateAsync(List<CreateDeviceTypeDto> items)
    {
        var entities = items.Select(dto => new mst_device_type
        {
            Code = dto.Code.Trim().ToUpper(),
            Name = dto.Name.Trim(),
            oemmanufacturerid = dto.oemmanufacturerId,
            Description = dto.Description,
            IsEnabled = true,
            IsActive = true,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _db.DeviceTypes.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(x => new DeviceTypeDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name
        }).ToList();
    }
}