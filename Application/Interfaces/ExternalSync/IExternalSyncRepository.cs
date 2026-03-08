using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IExternalSyncRepository
{
    Task<external_sync_config?> GetActiveConfigAsync(string moduleName, CancellationToken ct = default);
    Task<Dictionary<string, external_sync_config>> GetActiveConfigsByModuleAsync(IEnumerable<string> moduleNames, CancellationToken ct = default);
    Task<List<ExternalSyncQueueBatchItem>> GetDuePendingQueueItemsAsync(int take, DateTime nowUtc, CancellationToken ct = default);
    Task<bool> TryMarkProcessingAsync(long queueId, DateTime processingAtUtc, CancellationToken ct = default);
    Task MarkSuccessAsync(long queueId, DateTime processedAtUtc, CancellationToken ct = default);
    Task MarkRetryAsync(long queueId, int retryCount, DateTime nextRetryTimeUtc, string errorMessage, CancellationToken ct = default);
    Task MarkFailedAsync(long queueId, string errorMessage, DateTime failedAtUtc, CancellationToken ct = default);
    Task<bool> MoveToDeadLetterByQueueIdAsync(long queueId, string errorMessage, CancellationToken ct = default);
    Task<int> CleanupSuccessfulQueueItemsAsync(DateTime olderThanUtc, int take, CancellationToken ct = default);
    Task<external_sync_queue?> GetQueueByIdAsync(long id, CancellationToken ct = default);
    Task<external_sync_dead_letter?> GetDlqByIdAsync(long id, CancellationToken ct = default);
    Task AddQueueAsync(external_sync_queue queueItem, CancellationToken ct = default);
    Task AddDeadLetterAsync(external_sync_dead_letter dlqItem, CancellationToken ct = default);
    Task RemoveQueueAsync(external_sync_queue queueItem, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<List<ExternalSyncModuleStatsDto>> GetModuleStatsAsync(CancellationToken ct = default);
    Task<List<external_sync_queue>> GetFailedQueueItemsAsync(int take, CancellationToken ct = default);
    Task<List<external_sync_dead_letter>> GetDlqItemsAsync(int take, CancellationToken ct = default);
}
