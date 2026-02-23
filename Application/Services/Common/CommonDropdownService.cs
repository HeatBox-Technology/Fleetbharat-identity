using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

public class CommonDropdownService : ICommonDropdownService
{
    private readonly IdentityDbContext _db;
    private readonly IWebHostEnvironment _env;
    private List<DropdownDto>? _cache;

    public CommonDropdownService(IdentityDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<List<DropdownDto>> GetAccountsAsync(string? search, int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.Accounts.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.AccountName.ToLower().Contains(s) ||
                x.AccountCode.ToLower().Contains(s));
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

    public async Task<List<DropdownDto>> GetCategoriesAsync(string? search, int limit)
    {
        if (limit <= 0) limit = 20;

        var query = _db.Categories.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.LabelName.ToLower().Contains(s));
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
            .Where(x => x.AccountId == accountId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.RoleName.ToLower().Contains(s));
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
            .Where(x => x.AccountId == accountId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s) ||
                x.Email.ToLower().Contains(s));
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
    public async Task<List<DropdownDto>> GetCurrencyDropdownAsync()
    {
        return await _db.Currencies
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

    public async Task<List<DropdownDto>> GetDeviceTypes()
    {
        return await _db.DeviceTypes
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new DropdownDto
            {
                Id = x.Id,
                Value = x.Name
            })
            .ToListAsync();
    }
}


