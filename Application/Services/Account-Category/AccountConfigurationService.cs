using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class AccountConfigurationService : IAccountConfigurationService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AccountConfigurationService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateAccountConfigurationRequest req)
    {
        // ✅ check account exists
        var acc = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == req.AccountId);
        if (acc == null) throw new KeyNotFoundException("Account not found");

        // ✅ one config per account
        var exists = await _db.AccountConfigurations
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == req.AccountId);
        if (exists) throw new InvalidOperationException("Configuration already exists for this account");

        var cfg = new mst_account_configuration
        {
            AccountId = req.AccountId,

            MapProvider = req.MapProvider,
            LicenseKey = req.LicenseKey,
            AddressKey = req.AddressKey,

            DateFormat = req.DateFormat,
            TimeFormat = req.TimeFormat,
            DistanceUnit = req.DistanceUnit,
            SpeedUnit = req.SpeedUnit,
            FuelUnit = req.FuelUnit,
            TemperatureUnit = req.TemperatureUnit,
            AddressDisplay = req.AddressDisplay,

            DefaultLanguage = req.DefaultLanguage,
            AllowedLanguagesCsv = req.AllowedLanguages == null
                ? null
                : string.Join(",", req.AllowedLanguages),

            CreatedOn = DateTime.UtcNow,
            IsActive = true
        };

        _db.AccountConfigurations.Add(cfg);
        await _db.SaveChangesAsync();
        return cfg.AccountConfigurationId;
    }

    public async Task<bool> UpdateAsync(int id, UpdateAccountConfigurationRequest req)
    {
        var cfg = await _db.AccountConfigurations
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountConfigurationId == id && !x.IsDeleted);
        if (cfg == null) return false;

        cfg.MapProvider = req.MapProvider;
        cfg.LicenseKey = req.LicenseKey;
        cfg.AddressKey = req.AddressKey;

        cfg.DateFormat = req.DateFormat;
        cfg.TimeFormat = req.TimeFormat;
        cfg.DistanceUnit = req.DistanceUnit;
        cfg.SpeedUnit = req.SpeedUnit;
        cfg.FuelUnit = req.FuelUnit;
        cfg.TemperatureUnit = req.TemperatureUnit;
        cfg.AddressDisplay = req.AddressDisplay;

        cfg.DefaultLanguage = req.DefaultLanguage;
        cfg.AllowedLanguagesCsv = req.AllowedLanguages == null
            ? null
            : string.Join(",", req.AllowedLanguages);

        cfg.IsActive = req.IsActive;
        cfg.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var cfg = await _db.AccountConfigurations
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountConfigurationId == id);
        if (cfg == null) return false;

        cfg.IsDeleted = true;
        cfg.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AccountConfigurationResponseDto?> GetByIdAsync(int id)
    {
        var data = await (from cfg in _db.AccountConfigurations.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
                          join acc in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on cfg.AccountId equals acc.AccountId
                          where cfg.AccountConfigurationId == id && !cfg.IsDeleted
                          select new AccountConfigurationResponseDto
                          {
                              AccountConfigurationId = cfg.AccountConfigurationId,
                              AccountId = cfg.AccountId,
                              AccountName = acc.AccountName,

                              MapProvider = cfg.MapProvider,
                              licenseKey = cfg.LicenseKey,
                              AddressKey = cfg.AddressKey,
                              TimeFormat = cfg.TimeFormat,
                              DistanceUnit = cfg.DistanceUnit,
                              SpeedUnit = cfg.SpeedUnit,
                              FuelUnit = cfg.FuelUnit,
                              TemperatureUnit = cfg.TemperatureUnit,
                              AddressDisplay = cfg.AddressDisplay,
                              DateFormat = cfg.DateFormat,
                              DefaultLanguage = cfg.DefaultLanguage,

                              CreatedOn = cfg.CreatedOn,
                              UpdatedOn = cfg.UpdatedOn
                          }).FirstOrDefaultAsync();

        return data;
    }

    public async Task<PagedResultDto<AccountConfigurationResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        int? accountId)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = from cfg in _db.AccountConfigurations.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
                    join acc in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on cfg.AccountId equals acc.AccountId
                    where !cfg.IsDeleted
                    select new { cfg, acc };

        if (accountId.HasValue)
            query = query.Where(x => x.cfg.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.acc.AccountName.ToLower().Contains(s) ||
                x.cfg.MapProvider.ToLower().Contains(s) ||
                x.cfg.DefaultLanguage.ToLower().Contains(s) ||
                x.cfg.DateFormat.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.cfg.UpdatedOn ?? x.cfg.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccountConfigurationResponseDto
            {
                AccountConfigurationId = x.cfg.AccountConfigurationId,
                AccountId = x.cfg.AccountId,
                AccountName = x.acc.AccountName,

                MapProvider = x.cfg.MapProvider,
                licenseKey = x.cfg.LicenseKey,
                AddressKey = x.cfg.AddressKey,
                TimeFormat = x.cfg.TimeFormat,
                DistanceUnit = x.cfg.DistanceUnit,
                SpeedUnit = x.cfg.SpeedUnit,
                FuelUnit = x.cfg.FuelUnit,
                TemperatureUnit = x.cfg.TemperatureUnit,
                AddressDisplay = x.cfg.AddressDisplay,
                DateFormat = x.cfg.DateFormat,
                DefaultLanguage = x.cfg.DefaultLanguage,

                CreatedOn = x.cfg.CreatedOn,
                UpdatedOn = x.cfg.UpdatedOn
            })
            .ToListAsync();

        return new PagedResultDto<AccountConfigurationResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            Items = items
        };
    }
}
