using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IExternalBulkSyncService
{
    Task SyncAsync(string moduleKey, IReadOnlyCollection<object> batch, CancellationToken ct = default);
}
