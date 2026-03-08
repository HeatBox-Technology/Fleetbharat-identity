using System;
using System.Threading;
using System.Threading.Tasks;

public class ExternalDeadLetterService : IExternalDeadLetterService
{
    private readonly IExternalSyncRepository _repo;

    public ExternalDeadLetterService(IExternalSyncRepository repo)
    {
        _repo = repo;
    }

    public async Task MoveToDeadLetterAsync(external_sync_queue queueItem, string errorMessage, CancellationToken ct = default)
    {
        var dlq = new external_sync_dead_letter
        {
            ModuleName = queueItem.ModuleName,
            EntityId = queueItem.EntityId,
            PayloadJson = queueItem.PayloadJson,
            ErrorMessage = errorMessage,
            RetryCount = queueItem.RetryCount,
            CreatedAt = queueItem.CreatedAt,
            MovedToDLQAt = DateTime.UtcNow
        };

        await _repo.AddDeadLetterAsync(dlq, ct);
        await _repo.RemoveQueueAsync(queueItem, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
