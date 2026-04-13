using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public abstract class BulkLookupResolverBase : IBulkLookupResolver, IBulkLookupCache
{
    private bool _isLoaded;
    private Dictionary<string, List<int>> _lookup = new(StringComparer.OrdinalIgnoreCase);

    public abstract string LookupType { get; }

    public async Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded)
            return;

        _lookup = await LoadAsync(cancellationToken);
        _isLoaded = true;
    }

    public async Task<(bool Success, object? Value, string? Error)> ResolveAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        await PreloadAsync(cancellationToken);

        var normalized = Normalize(input);
        var compact = NormalizeCompact(input);

        List<int>? ids = null;
        if (!string.IsNullOrWhiteSpace(normalized))
            _lookup.TryGetValue(normalized, out ids);

        if ((ids == null || ids.Count == 0) && !string.IsNullOrWhiteSpace(compact))
            _lookup.TryGetValue(compact, out ids);

        if ((ids == null || ids.Count == 0) && string.Equals(normalized, compact, StringComparison.OrdinalIgnoreCase) == false)
        {
            var normalizedNoQuotes = NormalizeWithoutQuotes(input);
            if (!string.IsNullOrWhiteSpace(normalizedNoQuotes))
                _lookup.TryGetValue(normalizedNoQuotes, out ids);
        }

        if (ids == null || ids.Count == 0)
            return (false, null, $"'{input}' was not found for lookup '{LookupType}'.");

        if (ids.Count > 1)
            return (false, null, $"'{input}' matched multiple records for lookup '{LookupType}'. Please use a unique value.");

        return (true, ids[0], null);
    }

    protected abstract Task<Dictionary<string, List<int>>> LoadAsync(CancellationToken cancellationToken);

    protected static Dictionary<string, List<int>> BuildLookupMap<T>(
        IEnumerable<T> items,
        Func<T, int> idSelector,
        Func<T, IEnumerable<string?>> aliasesSelector)
    {
        var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            foreach (var alias in aliasesSelector(item))
            {
                var id = idSelector(item);
                AddAlias(map, Normalize(alias), id);
                AddAlias(map, NormalizeCompact(alias), id);
                AddAlias(map, NormalizeWithoutQuotes(alias), id);
            }
        }

        return map;
    }

    protected static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Trim().ToLowerInvariant();
    }

    protected static string NormalizeCompact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Where(c => !char.IsWhiteSpace(c) && c != '\'' && c != '"' && c != '-' && c != '_');

        return new string(chars.ToArray());
    }

    protected static string NormalizeWithoutQuotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return value
            .Trim()
            .Trim('\'', '"')
            .ToLowerInvariant();
    }

    private static void AddAlias(Dictionary<string, List<int>> map, string key, int id)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (!map.TryGetValue(key, out var ids))
        {
            ids = new List<int>();
            map[key] = ids;
        }

        if (!ids.Contains(id))
            ids.Add(id);
    }
}

public class AccountBulkLookupResolver : BulkLookupResolverBase
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AccountBulkLookupResolver(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public override string LookupType => "account";

    protected override async Task<Dictionary<string, List<int>>> LoadAsync(CancellationToken cancellationToken)
    {
        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.AccountId, x.AccountName, x.AccountCode })
            .ToListAsync(cancellationToken);

        return BuildLookupMap(
            accounts,
            x => x.AccountId,
            x => new[] { x.AccountName, x.AccountCode, $"{x.AccountName} ({x.AccountCode})" });
    }
}

public class VehicleTypeBulkLookupResolver : BulkLookupResolverBase
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public VehicleTypeBulkLookupResolver(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public override string LookupType => "vehicleType";

    protected override async Task<Dictionary<string, List<int>>> LoadAsync(CancellationToken cancellationToken)
    {
        var items = await _db.VehicleTypes
            .AsNoTracking()
            .Where(x => x.Status != null && x.Status.ToLower() == "active")
            .Select(x => new { x.Id, x.VehicleTypeName })
            .ToListAsync(cancellationToken);

        return BuildLookupMap(items, x => x.Id, x => new[] { x.VehicleTypeName });
    }
}

public class ManufacturerBulkLookupResolver : BulkLookupResolverBase
{
    private readonly IdentityDbContext _db;

    public ManufacturerBulkLookupResolver(IdentityDbContext db)
    {
        _db = db;
    }

    public override string LookupType => "manufacturer";

    protected override async Task<Dictionary<string, List<int>>> LoadAsync(CancellationToken cancellationToken)
    {
        var items = await _db.OemManufacturers
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsEnabled)
            .Select(x => new { x.Id, x.Name, x.Code })
            .ToListAsync(cancellationToken);

        return BuildLookupMap(items, x => x.Id, x => new[] { x.Name, x.Code, $"{x.Name} ({x.Code})" });
    }
}

public class DeviceTypeBulkLookupResolver : BulkLookupResolverBase
{
    private readonly IdentityDbContext _db;

    public DeviceTypeBulkLookupResolver(IdentityDbContext db)
    {
        _db = db;
    }

    public override string LookupType => "deviceType";

    protected override async Task<Dictionary<string, List<int>>> LoadAsync(CancellationToken cancellationToken)
    {
        var items = await _db.DeviceTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive && x.IsEnabled)
            .Select(x => new { x.Id, x.Name, x.Code })
            .ToListAsync(cancellationToken);

        return BuildLookupMap(items, x => x.Id, x => new[] { x.Name, x.Code });
    }
}

public class GeofenceBulkLookupResolver : BulkLookupResolverBase
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GeofenceBulkLookupResolver(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public override string LookupType => "geofence";

    protected override async Task<Dictionary<string, List<int>>> LoadAsync(CancellationToken cancellationToken)
    {
        var items = await _db.GeofenceZones
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .Select(x => new { x.Id, x.DisplayName, x.UniqueCode })
            .ToListAsync(cancellationToken);

        return BuildLookupMap(items, x => x.Id, x => new[] { x.DisplayName, x.UniqueCode });
    }
}
