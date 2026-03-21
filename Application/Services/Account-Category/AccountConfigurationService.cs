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
            // Store the selected UI languages as CSV in the database.
            AllowedLanguagesCsv = ToAllowedLanguagesCsv(req.AllowedLanguages),

            CreatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null,
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
        // Store the selected UI languages as CSV in the database.
        cfg.AllowedLanguagesCsv = ToAllowedLanguagesCsv(req.AllowedLanguages);

        cfg.IsActive = req.IsActive;
        cfg.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
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
        cfg.IsActive = false;
        cfg.DeletedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        cfg.DeletedAt = DateTime.UtcNow;
        cfg.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<AccountConfigurationResponseDto?> GetByIdAsync(int id)
    {
        var data = await (from cfg in _db.AccountConfigurations.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
                          join acc in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on cfg.AccountId equals acc.AccountId
                          where cfg.AccountConfigurationId == id && !cfg.IsDeleted
                          select new
                          {
                              cfg.AccountConfigurationId,
                              cfg.AccountId,
                              acc.AccountName,
                              cfg.MapProvider,
                              cfg.LicenseKey,
                              cfg.AddressKey,
                              cfg.TimeFormat,
                              cfg.DistanceUnit,
                              cfg.SpeedUnit,
                              cfg.FuelUnit,
                              cfg.TemperatureUnit,
                              cfg.AddressDisplay,
                              cfg.DateFormat,
                              cfg.DefaultLanguage,
                              cfg.AllowedLanguagesCsv,
                              cfg.CreatedOn,
                              cfg.UpdatedOn
                          }).FirstOrDefaultAsync();

        if (data == null)
            return null;

        return new AccountConfigurationResponseDto
        {
            AccountConfigurationId = data.AccountConfigurationId,
            AccountId = data.AccountId,
            AccountName = data.AccountName,
            MapProvider = data.MapProvider,
            licenseKey = data.LicenseKey,
            AddressKey = data.AddressKey,
            TimeFormat = data.TimeFormat,
            DistanceUnit = data.DistanceUnit,
            SpeedUnit = data.SpeedUnit,
            FuelUnit = data.FuelUnit,
            TemperatureUnit = data.TemperatureUnit,
            AddressDisplay = data.AddressDisplay,
            DateFormat = data.DateFormat,
            DefaultLanguage = data.DefaultLanguage,
            // Convert saved CSV back to a list for the UI.
            AllowedLanguages = ParseAllowedLanguagesCsv(data.AllowedLanguagesCsv),
            CreatedOn = data.CreatedOn,
            UpdatedOn = data.UpdatedOn
        };
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
                (x.acc.AccountName != null && x.acc.AccountName.ToLower().Contains(s)) ||
                (x.cfg.MapProvider != null && x.cfg.MapProvider.ToLower().Contains(s)) ||
                (x.cfg.DefaultLanguage != null && x.cfg.DefaultLanguage.ToLower().Contains(s)) ||
                (x.cfg.DateFormat != null && x.cfg.DateFormat.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.cfg.UpdatedOn ?? x.cfg.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.cfg.AccountConfigurationId,
                x.cfg.AccountId,
                x.acc.AccountName,
                x.cfg.MapProvider,
                x.cfg.LicenseKey,
                x.cfg.AddressKey,
                x.cfg.TimeFormat,
                x.cfg.DistanceUnit,
                x.cfg.SpeedUnit,
                x.cfg.FuelUnit,
                x.cfg.TemperatureUnit,
                x.cfg.AddressDisplay,
                x.cfg.DateFormat,
                x.cfg.DefaultLanguage,
                x.cfg.AllowedLanguagesCsv,
                x.cfg.CreatedOn,
                x.cfg.UpdatedOn
            })
            .ToListAsync();

        var responseItems = items.Select(x => new AccountConfigurationResponseDto
        {
            AccountConfigurationId = x.AccountConfigurationId,
            AccountId = x.AccountId,
            AccountName = x.AccountName,
            MapProvider = x.MapProvider,
            licenseKey = x.LicenseKey,
            AddressKey = x.AddressKey,
            TimeFormat = x.TimeFormat,
            DistanceUnit = x.DistanceUnit,
            SpeedUnit = x.SpeedUnit,
            FuelUnit = x.FuelUnit,
            TemperatureUnit = x.TemperatureUnit,
            AddressDisplay = x.AddressDisplay,
            DateFormat = x.DateFormat,
            DefaultLanguage = x.DefaultLanguage,
            // Convert saved CSV back to a list for the UI.
            AllowedLanguages = ParseAllowedLanguagesCsv(x.AllowedLanguagesCsv),
            CreatedOn = x.CreatedOn,
            UpdatedOn = x.UpdatedOn
        }).ToList();

        return new PagedResultDto<AccountConfigurationResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            Items = responseItems
        };
    }

    // Convert the incoming language list into the CSV format stored in the table.
    private static string? ToAllowedLanguagesCsv(List<string>? allowedLanguages)
    {
        if (allowedLanguages == null || allowedLanguages.Count == 0)
            return null;

        return string.Join(",",
            allowedLanguages
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()));
    }

    // Convert the stored CSV value back into a list for API responses.
    private static List<string> ParseAllowedLanguagesCsv(string? allowedLanguagesCsv)
    {
        if (string.IsNullOrWhiteSpace(allowedLanguagesCsv))
            return new List<string>();

        return allowedLanguagesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    }
}
