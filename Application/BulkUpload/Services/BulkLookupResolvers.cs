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

    private DateTime _lastLoadedAt = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task PreloadAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded && DateTime.UtcNow - _lastLoadedAt < CacheDuration)
            return;

        _lookup = await LoadAsync(cancellationToken);
        _isLoaded = true;
        _lastLoadedAt = DateTime.UtcNow;
    }
    public async Task<(bool Success, object? Value, string? Error)> ResolveAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        await PreloadAsync(cancellationToken);

        var possibleKeys = GetLookupKeys(input).ToList();
        Console.WriteLine($"[BulkLookup][{LookupType}] ResolveAsync input: '{input ?? string.Empty}'");
        Console.WriteLine($"[BulkLookup][{LookupType}] ResolveAsync normalized keys: {string.Join(", ", possibleKeys.Select(x => $"'{x}'"))}");

        List<int>? ids = null;
        var matchedKeys = new List<string>();

        foreach (var key in possibleKeys)
        {
            if (_lookup.TryGetValue(key, out ids) && ids?.Count > 0)
            {
                matchedKeys.Add($"{key} => [{string.Join(", ", ids)}]");
                break;
            }
        }

        Console.WriteLine($"[BulkLookup][{LookupType}] ResolveAsync matching keys: {(matchedKeys.Count == 0 ? "none" : string.Join("; ", matchedKeys))}");

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

    protected static string NormalizeLookupValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalizedChars = value
            .Trim()
            .Select(c =>
            {
                if (char.IsControl(c))
                    return ' ';

                if (c == '\u00A0')
                    return ' ';

                if (c == '\t' || c == '\n' || c == '\r' || c == '-' || c == '_')
                    return ' ';

                return c;
            })
            .ToArray();

        return string.Join(
            " ",
            new string(normalizedChars)
                 .ToLowerInvariant()
                 .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        );
    }

    protected static IEnumerable<string> GetLookupKeys(string? value)
    {
        var normalized = Normalize(value);
        var compact = NormalizeCompact(value);
        var withoutQuotes = NormalizeWithoutQuotes(value);
        var normalizedLookup = NormalizeLookupValue(value);
        var compactNormalizedLookup = NormalizeCompact(normalizedLookup);
        var withoutQuotesNormalizedLookup = NormalizeWithoutQuotes(normalizedLookup);

        return new[]
        {
            normalized,
            compact,
            withoutQuotes,
            normalizedLookup,
            compactNormalizedLookup,
            withoutQuotesNormalizedLookup
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.OrdinalIgnoreCase);
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
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .Select(x => new
            {
                x.AccountId,
                x.AccountName,
                x.AccountCode
            })
            .ToListAsync(cancellationToken);

        Console.WriteLine($"[BulkLookup][account] LoadAsync total accounts loaded: {accounts.Count}");
        var lookup = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var account in accounts)
        {
            var accountId = account.AccountId;
            var accountName = account.AccountName ?? string.Empty;
            var accountCode = account.AccountCode ?? string.Empty;
            var combined = string.IsNullOrWhiteSpace(accountCode)
                ? accountName
                : $"{accountName} ({accountCode})";

            var keys = new[]
            {
                accountName,
                accountCode,
                combined
            }
            .SelectMany(GetLookupKeys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

            Console.WriteLine(
                $"[BulkLookup][account] LoadAsync account loaded: Id={accountId}, Name='{accountName}', Code='{accountCode}', Keys={string.Join(", ", keys.Select(x => $"'{x}'"))}");

            foreach (var key in keys)
            {
                if (!lookup.TryGetValue(key, out var ids))
                {
                    ids = new List<int>();
                    lookup[key] = ids;
                }

                if (!ids.Contains(accountId))
                    ids.Add(accountId);
            }
        }

        return lookup;
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

