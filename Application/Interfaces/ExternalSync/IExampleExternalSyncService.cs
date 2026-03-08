using System.Threading;
using System.Threading.Tasks;

public interface IExampleExternalSyncService
{
    Task SyncAsync(string payloadJson, CancellationToken ct = default);
}
