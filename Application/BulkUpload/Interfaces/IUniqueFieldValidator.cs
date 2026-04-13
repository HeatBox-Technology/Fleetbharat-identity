using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IUniqueFieldValidator
{
    Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string moduleKey,
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default);
}

public interface IBulkUniqueRule
{
    string ModuleKey { get; }

    Task<(bool IsDuplicate, string? Error)> ValidateAsync(
        string propertyName,
        string value,
        Dictionary<string, object> scopeValues,
        CancellationToken cancellationToken = default);
}
