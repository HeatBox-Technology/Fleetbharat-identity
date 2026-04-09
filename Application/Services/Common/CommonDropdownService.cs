using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class CommonDropdownService : ICommonDropdownService
{
    private readonly IdentityDbContext _db;
    private readonly IHierarchyService _hierarchyService;
    private readonly ICurrentUserService _currentUser;

    public CommonDropdownService(
        IdentityDbContext db,
        IHierarchyService hierarchyService,
        ICurrentUserService currentUser)
    {
        _db = db;
        _hierarchyService = hierarchyService;
        _currentUser = currentUser;
    }

    public async Task<List<DropdownDto>> GetAccountsAsync(string? search, int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.Accounts.AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.AccountName != null && x.AccountName.ToLower().Contains(s)) ||
                (x.AccountCode != null && x.AccountCode.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(x => x.AccountName)
            .Take(limit)
            .Select(x => new DropdownDto
            {
                Id = x.AccountId,
                Value = $"{x.AccountName} ({x.AccountCode})"
            })
            .ToListAsync();
    }

    public async Task<List<AccountDropdownDto>> GetAccountDropdownAsync(int? accountId, int? categoryId)
    {
        var query = _db.Accounts
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        return await query
            .OrderBy(x => x.AccountName)
            .Select(x => new AccountDropdownDto
            {
                AccountId = x.AccountId,
                AccountCode = x.AccountCode,
                AccountName = x.AccountName
            })
            .ToListAsync();
    }

    public async Task<List<DropdownDto>> GetCategoriesAsync(string? search, int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.Categories.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.LabelName != null && x.LabelName.ToLower().Contains(s));
        }

        return await query
            .OrderBy(x => x.LabelName)
            .Take(limit)
            .Select(x => new DropdownDto
            {
                Id = x.CategoryId,
                Value = x.LabelName
            })
            .ToListAsync();
    }

    public async Task<List<DropdownDto>> GetRolesAsync(int accountId, string? search, int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.Roles.AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.RoleName != null && x.RoleName.ToLower().Contains(s));
        }

        return await query
            .OrderBy(x => x.RoleName)
            .Take(limit)
            .Select(x => new DropdownDto
            {
                Id = x.RoleId,
                Value = x.RoleName
            })
            .ToListAsync();
    }

    public async Task<List<DropdownGuidDto>> GetUsersAsync(int accountId, string? search, int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.Users.AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.FirstName != null && x.FirstName.ToLower().Contains(s)) ||
                (x.LastName != null && x.LastName.ToLower().Contains(s)) ||
                (x.Email != null && x.Email.ToLower().Contains(s)));
        }

        return await query
            .OrderBy(x => x.FirstName)
            .Take(limit)
            .Select(x => new DropdownGuidDto
            {
                Id = x.UserId,
                Value = $"{x.FirstName} {x.LastName} ({x.Email})"
            })
            .ToListAsync();
    }
    public async Task<List<DriverDropdownDto>> GetDriversDropdownAsync(
        int? driverId,
        int? accountId,
        string? name,
        string? mobile)
    {
        var query = _db.Drivers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser);

        if (driverId.HasValue)
            query = query.Where(x => x.DriverId == driverId.Value);

        if (accountId.HasValue)
            query = query.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(name))
        {
            var normalizedName = name.Trim().ToLower();
            query = query.Where(x =>
                x.Name != null &&
                x.Name.ToLower().Contains(normalizedName));
        }

        if (!string.IsNullOrWhiteSpace(mobile))
        {
            var normalizedMobile = mobile.Trim().ToLower();
            query = query.Where(x =>
                x.Mobile != null &&
                x.Mobile.ToLower().Contains(normalizedMobile));
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new DriverDropdownDto
            {
                DriverId = x.DriverId,
                AccountId = x.AccountId,
                Name = x.Name,
                Mobile = x.Mobile
            })
            .ToListAsync();
    }
    public async Task<List<VehicleTypeDropdownDto>> GetVehicleTypesAsync(int? id)
    {
        var query = _db.VehicleTypes
            .AsNoTracking()
            .Where(x => x.Status != null && new[] { "active", "true", "1", "enabled" }.Contains(x.Status.Trim().ToLower()));

        if (id.HasValue)
            query = query.Where(x => x.Id == id.Value);

        return await query
            .OrderBy(x => x.VehicleTypeName)
            .Select(x => new VehicleTypeDropdownDto
            {
                Id = x.Id,
                VehicleTypeName = x.VehicleTypeName,
                Category = x.Category
            })
            .ToListAsync();
    }
    public async Task<List<DropdownDto>> GetCurrencyDropdownAsync()
    {
        return await _db.Currencies
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new DropdownDto
            {
                Id = x.CurrencyId,
                Value = x.Code + " - " + x.Name + " (" + x.Symbol + ")"
            })
            .ToListAsync();
    }
    public async Task<List<DropdownDto>> GetFormModuleDropdownAsync()
    {
        return await _db.FormModules
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ModuleName)
            .Select(x => new DropdownDto
            {
                Id = x.FormModuleId,
                Value = x.ModuleName
            })
            .ToListAsync();
    }
    public async Task<List<DropdownDto>> GetVehicles(int accountId)
    {
        return await _db.Vehicles
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId &&
                        x.Status == "Active" &&
                        !x.IsDeleted)
            .OrderBy(x => x.VehicleNumber)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.VehicleNumber
            })
            .ToListAsync();
    }

    public async Task<List<DropdownDto>> GetDevices(int accountId)
    {
        return await _db.Devices
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId &&
                        x.IsActive &&
                        !x.IsDeleted)
            .OrderBy(x => x.DeviceNo)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.DeviceNo
            })
            .ToListAsync();
    }

    public async Task<List<DropdownDto>> GetSims(int accountId)
    {
        return await _db.Sims
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId &&
                        x.IsActive &&
                        !x.IsDeleted)
            .OrderBy(x => x.Iccid)
            .Select(x => new DropdownDto
            {
                Id = x.SimId,
                Value = x.Iccid
            })
            .ToListAsync();
    }

    public async Task<List<DropdownDto>> GetDeviceType()
    {
        return await _db.DeviceTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.IsEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.Name
            })
            .ToListAsync();
    }
    public async Task<List<DropdownDto>> GetTripTypes()
    {
        return await _db.TripTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.IsEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.Name
            })
            .ToListAsync();
    }
    public async Task<List<DropdownDto>> GetGeofences(
     int accountId,
     string? search,
     int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.GeofenceZones.AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId &&
                        x.Status == "ENABLED" &&
                        !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.DisplayName != null && x.DisplayName.ToLower().Contains(s));
        }

        return await query
            .OrderBy(x => x.DisplayName)
            .Take(limit)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.DisplayName
            })
            .ToListAsync();
    }
    public async Task<List<DropdownDto>> GetManufacture()
    {
        return await _db.OemManufacturers
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsEnabled)
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.Name
            })
            .ToListAsync();
    }

    public Task<FormFilterConfigResponseDto?> GetFilterConfigByFormNameAsync(string formName) =>
        _hierarchyService.GetFilterConfigByFormNameAsync(formName);
}


