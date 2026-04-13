using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class VehicleBulkUniqueRule : IBulkUniqueRule
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public VehicleBulkUniqueRule(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public string ModuleKey => "vehicles";

    public async Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim().ToLower();
        var query = _db.Vehicles
            .AsNoTracking()
            //.ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        string accountLabel = "selected account";
        if (TryGetInt(scopeValues, "AccountId", out var accountId))
        {
            query = query.Where(x => x.AccountId == accountId);
            accountLabel = await GetAccountLabelAsync(accountId, cancellationToken);
        }

        bool exists = propertyName switch
        {
            "VehicleNumber" => await query.AnyAsync(x => x.VehicleNumber.ToLower() == normalized, cancellationToken),
            "VinOrChassisNumber" => await query.AnyAsync(x => x.VinOrChassisNumber.ToLower() == normalized, cancellationToken),
            _ => false
        };

        return exists
            ? (true, BuildFriendlyDuplicateMessage(propertyName, value, accountLabel))
            : (false, null);
    }

    private async Task<string> GetAccountLabelAsync(int accountId, CancellationToken cancellationToken)
    {
        var accountName = await _db.Accounts
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .Select(x => x.AccountName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(accountName)
            ? $"AccountId {accountId}"
            : accountName;
    }

    private static string BuildFriendlyDuplicateMessage(string propertyName, string value, string accountLabel)
    {
        return propertyName switch
        {
            "VehicleNumber" => $"Vehicle number '{value}' already exists for account '{accountLabel}'.",
            "VinOrChassisNumber" => $"VIN/Chassis number '{value}' already exists for account '{accountLabel}'.",
            _ => $"{propertyName} '{value}' already exists for account '{accountLabel}'."
        };
    }

    private static bool TryGetInt(Dictionary<string, object> values, string key, out int result)
    {
        if (values.TryGetValue(key, out var raw) && raw != null && int.TryParse(raw.ToString(), out result))
            return true;

        result = 0;
        return false;
    }
}

public class DeviceBulkUniqueRule : IBulkUniqueRule
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeviceBulkUniqueRule(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public string ModuleKey => "devices";

    public async Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim().ToLower();
        var query = _db.Devices
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (TryGetInt(scopeValues, "AccountId", out var accountId))
            query = query.Where(x => x.AccountId == accountId);

        bool exists = propertyName switch
        {
            "DeviceImeiOrSerial" => await query.AnyAsync(x => x.DeviceImeiOrSerial.ToLower() == normalized, cancellationToken),
            "DeviceNo" => await query.AnyAsync(x => x.DeviceNo.ToLower() == normalized, cancellationToken),
            _ => false
        };

        return exists
            ? (true, $"{propertyName} '{value}' already exists.")
            : (false, null);
    }

    private static bool TryGetInt(Dictionary<string, object> values, string key, out int result)
    {
        if (values.TryGetValue(key, out var raw) && raw != null && int.TryParse(raw.ToString(), out result))
            return true;

        result = 0;
        return false;
    }
}

public class DriverBulkUniqueRule : IBulkUniqueRule
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DriverBulkUniqueRule(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public string ModuleKey => "drivers";

    public async Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim().ToLower();
        var query = _db.Drivers
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        bool exists = propertyName switch
        {
            "Mobile" when TryGetInt(scopeValues, "AccountId", out var accountId)
                => await query.AnyAsync(x => x.AccountId == accountId && x.Mobile.ToLower() == normalized, cancellationToken),
            "LicenseNumber"
                => await query.AnyAsync(x => x.LicenseNumber.ToLower() == normalized, cancellationToken),
            _ => false
        };

        return exists
            ? (true, $"{propertyName} '{value}' already exists.")
            : (false, null);
    }

    private static bool TryGetInt(Dictionary<string, object> values, string key, out int result)
    {
        if (values.TryGetValue(key, out var raw) && raw != null && int.TryParse(raw.ToString(), out result))
            return true;

        result = 0;
        return false;
    }
}

public class GeofenceBulkUniqueRule : IBulkUniqueRule
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GeofenceBulkUniqueRule(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public string ModuleKey => "geofence";

    public async Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default)
    {
        var normalized = value.Trim().ToLower();
        var query = _db.GeofenceZones
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);

        if (TryGetInt(scopeValues, "AccountId", out var accountId))
            query = query.Where(x => x.AccountId == accountId);

        bool exists = propertyName switch
        {
            "UniqueCode" => await query.AnyAsync(x => x.UniqueCode.ToLower() == normalized, cancellationToken),
            "DisplayName" => await query.AnyAsync(x => x.DisplayName.ToLower() == normalized, cancellationToken),
            _ => false
        };

        return exists
            ? (true, $"{propertyName} '{value}' already exists.")
            : (false, null);
    }

    private static bool TryGetInt(Dictionary<string, object> values, string key, out int result)
    {
        if (values.TryGetValue(key, out var raw) && raw != null && int.TryParse(raw.ToString(), out result))
            return true;

        result = 0;
        return false;
    }
}
