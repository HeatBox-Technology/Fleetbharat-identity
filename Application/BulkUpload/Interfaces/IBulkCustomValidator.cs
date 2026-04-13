using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IBulkCustomValidator
{
    string ModuleKey { get; }

    Task<List<string>> ValidateAsync(
        Dictionary<string, string> row,
        object dto,
        CancellationToken cancellationToken = default);
}
