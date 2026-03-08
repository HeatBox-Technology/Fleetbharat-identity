using System.Threading;
using System.Threading.Tasks;

public interface IExternalDeadLetterService
{
    Task MoveToDeadLetterAsync(external_sync_queue queueItem, string errorMessage, CancellationToken ct = default);
    Task<bool> MoveToDeadLetterByQueueIdAsync(long queueId, string errorMessage, CancellationToken ct = default);
}
