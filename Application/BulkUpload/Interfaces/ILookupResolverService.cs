using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface ILookupResolverService
{
    Task<(bool Success, object? Value, string? Error)> ResolveAsync(
        string lookupType,
        string input,
        CancellationToken cancellationToken = default);

    Task PreloadAsync(
        IEnumerable<string> lookupTypes,
        CancellationToken cancellationToken = default);
}
