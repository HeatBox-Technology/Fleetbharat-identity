using System.Threading;
using System.Threading.Tasks;

public interface IExternalSyncQueueService
{
    Task EnqueueAsync(ExternalSyncQueueCreateRequest request, CancellationToken ct = default);
}
