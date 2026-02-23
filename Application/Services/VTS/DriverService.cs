using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


public class DriverService : IDriverService
{
    private readonly IdentityDbContext _db;

    public DriverService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateDriverDto dto)
    {
        // Account validation
        var accountExists = await _db.Accounts
            .AnyAsync(x => x.AccountId == dto.AccountId);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        // Mobile unique per account
        var mobileExists = await _db.Drivers
            .AnyAsync(x => x.Mobile == dto.Mobile &&
                           x.AccountId == dto.AccountId &&
                           !x.IsDeleted);

        if (mobileExists)
            throw new Exception("Driver mobile already exists");

        // License unique
        var licenseExists = await _db.Drivers
            .AnyAsync(x => x.LicenseNumber == dto.LicenseNumber &&
                           !x.IsDeleted);

        if (licenseExists)
            throw new Exception("License already exists");

        var entity = new mst_driver
        {
            AccountId = dto.AccountId,
            Name = dto.Name,
            Mobile = dto.Mobile,
            LicenseNumber = dto.LicenseNumber,
            LicenseExpiry = dto.LicenseExpiry,
            BloodGroup = dto.BloodGroup,
            EmergencyContact = dto.EmergencyContact,
            StatusKey = dto.StatusKey,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        _db.Drivers.Add(entity);
        await _db.SaveChangesAsync();

        return entity.DriverId;
    }

    public async Task<bool> UpdateAsync(int id, UpdateDriverDto dto)
    {
        var entity = await _db.Drivers
            .FirstOrDefaultAsync(x => x.DriverId == id && !x.IsDeleted);

        if (entity == null)
            return false;

        // Mobile validation
        var mobileExists = await _db.Drivers
            .AnyAsync(x => x.Mobile == dto.Mobile &&
                           x.AccountId == entity.AccountId &&
                           x.DriverId != id &&
                           !x.IsDeleted);

        if (mobileExists)
            throw new Exception("Driver mobile already exists");

        entity.Name = dto.Name;
        entity.Mobile = dto.Mobile;
        entity.LicenseNumber = dto.LicenseNumber;
        entity.LicenseExpiry = dto.LicenseExpiry;
        entity.BloodGroup = dto.BloodGroup;
        entity.EmergencyContact = dto.EmergencyContact;
        entity.StatusKey = dto.StatusKey;
        entity.IsActive = dto.IsActive;
        entity.UpdatedBy = dto.UpdatedBy;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int driverId, bool isActive)
    {
        var entity = await _db.Drivers
            .FirstOrDefaultAsync(x => x.DriverId == driverId && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int driverId)
    {
        var entity = await _db.Drivers
            .FirstOrDefaultAsync(x => x.DriverId == driverId && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DriverDto?> GetByIdAsync(int driverId)
    {
        return await _db.Drivers
            .Where(x => x.DriverId == driverId && !x.IsDeleted)
            .Select(x => MapToDto(x))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DriverDto>> GetAllAsync()
    {
        return await _db.Drivers
            .Where(x => !x.IsDeleted)
            .Select(x => MapToDto(x))
            .ToListAsync();
    }

    public async Task<PagedResultDto<DriverDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _db.Drivers
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(s) ||
                x.Mobile.ToLower().Contains(s) ||
                x.LicenseNumber.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.DriverId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync();

        return new PagedResultDto<DriverDto>
        {
            Items = items,
            TotalRecords = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DriverListUiResponseDto> GetDrivers(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        var query = _db.Drivers.Where(x => !x.IsDeleted);

        var total = await query.CountAsync();
        var active = await query.CountAsync(x => x.IsActive);

        var expiringSoon = await query.CountAsync(x =>
            x.LicenseExpiry != null &&
            x.LicenseExpiry <= DateTime.UtcNow.AddDays(30));

        var summary = new DriverSummaryDto
        {
            TotalDrivers = total,
            Active = active,
            Inactive = total - active,
            LicenseExpiringSoon = expiringSoon
        };

        var paged = await GetPagedAsync(page, pageSize, accountId, search);

        return new DriverListUiResponseDto
        {
            Summary = summary,
            Drivers = paged
        };
    }

    public async Task<List<DriverDto>> BulkCreateAsync(List<CreateDriverDto> drivers)
    {
        var entities = drivers.Select(dto => new mst_driver
        {
            AccountId = dto.AccountId,
            Name = dto.Name,
            Mobile = dto.Mobile,
            LicenseNumber = dto.LicenseNumber,
            LicenseExpiry = dto.LicenseExpiry,
            BloodGroup = dto.BloodGroup,
            EmergencyContact = dto.EmergencyContact,
            StatusKey = dto.StatusKey,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        }).ToList();

        _db.Drivers.AddRange(entities);
        await _db.SaveChangesAsync();

        return entities.Select(MapToDto).ToList();
    }

    private static DriverDto MapToDto(mst_driver x)
    {
        return new DriverDto
        {
            DriverId = x.DriverId,
            AccountId = x.AccountId,
            Name = x.Name,
            Mobile = x.Mobile,
            LicenseNumber = x.LicenseNumber,
            LicenseExpiry = x.LicenseExpiry,
            BloodGroup = x.BloodGroup,
            EmergencyContact = x.EmergencyContact,
            StatusKey = x.StatusKey,
            CreatedAt = x.CreatedAt,
            CreatedBy = x.CreatedBy,
            UpdatedAt = x.UpdatedAt,
            UpdatedBy = x.UpdatedBy,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted
        };
    }
}
