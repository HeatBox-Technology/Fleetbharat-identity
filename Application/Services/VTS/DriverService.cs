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
    private readonly ICurrentUserService _currentUser;

    public DriverService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateDriverDto dto)
    {
        var accountExists = await _db.Accounts
            .AnyAsync(x => x.AccountId == dto.AccountId && !x.IsDeleted);

        if (!accountExists)
            throw new Exception("Invalid AccountId");

        // ✅ Access validation
        if (!_currentUser.IsSystem &&
            !_currentUser.AccessibleAccountIds.Contains(dto.AccountId))
            throw new Exception("Unauthorized account access");

        var mobile = dto.Mobile.Trim();
        var license = dto.LicenseNumber.Trim();

        // ✅ Mobile unique per account
        var mobileExists = await _db.Drivers
            .AnyAsync(x => x.Mobile == mobile &&
                           x.AccountId == dto.AccountId &&
                           !x.IsDeleted);

        if (mobileExists)
            throw new Exception("Driver mobile already exists");

        // ✅ License global unique
        var licenseExists = await _db.Drivers
            .AnyAsync(x => x.LicenseNumber == license && !x.IsDeleted);

        if (licenseExists)
            throw new Exception("License already exists");

        var entity = new mst_driver
        {
            AccountId = dto.AccountId,
            Name = dto.Name,
            Mobile = mobile,
            LicenseNumber = license,
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
            .Where(x => !x.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.DriverId == id);

        if (entity == null)
            return false;

        var mobile = dto.Mobile.Trim();
        var license = dto.LicenseNumber.Trim();

        // ✅ Mobile duplicate check
        var mobileExists = await _db.Drivers.AnyAsync(x =>
            x.Mobile == mobile &&
            x.AccountId == entity.AccountId &&
            x.DriverId != id &&
            !x.IsDeleted);

        if (mobileExists)
            throw new Exception("Driver mobile already exists");

        // ✅ License duplicate check
        var licenseExists = await _db.Drivers.AnyAsync(x =>
            x.LicenseNumber == license &&
            x.DriverId != id &&
            !x.IsDeleted);

        if (licenseExists)
            throw new Exception("License already exists");

        entity.Name = dto.Name;
        entity.Mobile = mobile;
        entity.LicenseNumber = license;
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
            //.ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x =>
    x.DriverId == driverId &&
    !x.IsDeleted &&
    (_currentUser.IsSystem ||
     _currentUser.AccessibleAccountIds.Contains(x.AccountId)));

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
            //.ApplyAccountHierarchyFilter(_currentUser)

            .FirstOrDefaultAsync(x =>
    x.DriverId == driverId &&
    !x.IsDeleted &&
    (_currentUser.IsSystem ||
     _currentUser.AccessibleAccountIds.Contains(x.AccountId)));

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedBy = _currentUser.AccountId;
        entity.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<DriverDto?> GetByIdAsync(int driverId)
    {
        return await _db.Drivers
     .Where(x =>
         x.DriverId == driverId &&
         !x.IsDeleted &&
         (_currentUser.IsSystem ||
          _currentUser.AccessibleAccountIds.Contains(x.AccountId)))
     .Select(x => MapToDto(x))
     .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<DriverDto>> GetAllAsync(
    int page = 1,
    int pageSize = 10,
    string? search = null)
    {
        var result = await GetPagedAsync(page, pageSize, null, search);
        return result.Items;
    }

    public async Task<PagedResultDto<DriverDto>> GetPagedAsync(
     int page,
     int pageSize,
     int? accountId = null,
     string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = BaseQuery();

        // ✅ Account filter
        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        // ✅ Search (NO ToLower)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();

            query = query.Where(x =>
                (x.Name != null && x.Name.Contains(s)) ||
                (x.Mobile != null && x.Mobile.Contains(s)) ||
                (x.LicenseNumber != null && x.LicenseNumber.Contains(s))
            );
        }

        var total = await query.CountAsync();

        var items = await query
        .OrderByDescending(x => x.DriverId)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new DriverDto
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
        })
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
        var query = BaseQuery();

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

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
        var entities = new List<mst_driver>();

        foreach (var dto in drivers)
        {
            if (!_currentUser.IsSystem &&
                !_currentUser.AccessibleAccountIds.Contains(dto.AccountId))
                continue; // skip unauthorized

            entities.Add(new mst_driver
            {
                AccountId = dto.AccountId,
                Name = dto.Name,
                Mobile = dto.Mobile?.Trim(),
                LicenseNumber = dto.LicenseNumber?.Trim(),
                LicenseExpiry = dto.LicenseExpiry,
                BloodGroup = dto.BloodGroup,
                EmergencyContact = dto.EmergencyContact,
                StatusKey = dto.StatusKey,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false
            });
        }

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
    private IQueryable<mst_driver> BaseQuery()
    {
        return _db.Drivers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser);
    }
    public async Task<byte[]> ExportDriversCsvAsync(int? accountId, string? search)
    {
        var query = _db.Drivers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser);

        // ✅ Account filter
        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        // ✅ Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();

            query = query.Where(x =>
                (x.Name != null && x.Name.Contains(s)) ||
                (x.Mobile != null && x.Mobile.Contains(s)) ||
                (x.LicenseNumber != null && x.LicenseNumber.Contains(s))
            );
        }

        var data = await query
            .OrderByDescending(x => x.DriverId)
            .Select(x => new
            {
                x.DriverId,
                x.Name,
                x.Mobile,
                x.LicenseNumber,
                x.LicenseExpiry,
                Status = x.IsActive ? "Active" : "Inactive"
            })
            .ToListAsync();

        // ✅ CSV Escape
        string Escape(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        var sb = new System.Text.StringBuilder();

        sb.AppendLine("DriverId,DriverName,Mobile,LicenseNumber,LicenseExpiry,Status");

        foreach (var d in data)
        {
            sb.AppendLine(
                $"{d.DriverId}," +
                $"{Escape(d.Name)}," +
                $"{Escape(d.Mobile)}," +
                $"{Escape(d.LicenseNumber)}," +
                $"{(d.LicenseExpiry.HasValue ? d.LicenseExpiry.Value.ToString("yyyy-MM-dd") : "")}," +
                $"{d.Status}"
            );
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }
}
