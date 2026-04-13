using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class LookupResolverService : ILookupResolverService
{
    private readonly Dictionary<string, IBulkLookupResolver> _resolvers;

    public LookupResolverService(IEnumerable<IBulkLookupResolver> resolvers)
    {
        _resolvers = resolvers.ToDictionary(x => x.LookupType, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<(bool Success, object? Value, string? Error)> ResolveAsync(
        string lookupType,
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookupType))
            return (false, null, "Lookup type is required.");

        if (!_resolvers.TryGetValue(lookupType, out var resolver))
            return (false, null, $"Lookup resolver '{lookupType}' is not registered.");

        return await resolver.ResolveAsync(input, cancellationToken);
    }

    public async Task PreloadAsync(
        IEnumerable<string> lookupTypes,
        CancellationToken cancellationToken = default)
    {
        foreach (var lookupType in lookupTypes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (_resolvers.TryGetValue(lookupType, out var resolver) &&
                resolver is IBulkLookupCache cache)
            {
                await cache.PreloadAsync(cancellationToken);
            }
        }
    }
}
