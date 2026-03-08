using System;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncQueueService : IExternalSyncQueueService
{
    private readonly IExternalSyncRepository _repo;

    public ExternalSyncQueueService(IExternalSyncRepository repo)
    {
        _repo = repo;
    }

    public async Task EnqueueAsync(ExternalSyncQueueCreateRequest request, CancellationToken ct = default)
    {
        var config = await _repo.GetActiveConfigAsync(request.ModuleName, ct);
        if (config == null)
            return;

        var queueItem = new external_sync_queue
        {
            ModuleName = request.ModuleName,
            EntityId = request.EntityId,
            PayloadJson = request.PayloadJson,
            Status = ExternalSyncStatus.Pending,
            RetryCount = 0,
            NextRetryTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddQueueAsync(queueItem, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
