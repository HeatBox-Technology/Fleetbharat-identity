using System.Threading;
using System.Threading.Tasks;

public interface IBulkLookupResolver
{
    string LookupType { get; }

    Task<(bool Success, object? Value, string? Error)> ResolveAsync(
        string input,
        CancellationToken cancellationToken = default);
}

public interface IBulkLookupCache
{
    Task PreloadAsync(CancellationToken cancellationToken = default);
}
