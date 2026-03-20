using System.Threading;
using System.Threading.Tasks;

public interface IExternalSyncInvoker
{
    Task InvokeAsync(
        external_sync_config config,
        string entityId,
        string? payloadJson,
        CancellationToken ct = default);
}
