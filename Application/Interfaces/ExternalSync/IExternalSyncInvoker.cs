using System.Threading;
using System.Threading.Tasks;

public interface IExternalSyncInvoker
{
    Task InvokeAsync(external_sync_config config, external_sync_queue queueItem, CancellationToken ct = default);
}
