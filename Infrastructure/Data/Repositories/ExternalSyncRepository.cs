using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncRepository : IExternalSyncRepository
{
    private readonly IdentityDbContext _db;

    public ExternalSyncRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public Task<external_sync_config?> GetActiveConfigAsync(string moduleName, CancellationToken ct = default)
    {
        return _db.external_sync_configs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ModuleName == moduleName && x.IsActive, ct);
    }

    public async Task<Dictionary<string, external_sync_config>> GetActiveConfigsByModuleAsync(IEnumerable<string> moduleNames, CancellationToken ct = default)
    {
        var modules = moduleNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (modules.Count == 0)
        {
            return new Dictionary<string, external_sync_config>(StringComparer.OrdinalIgnoreCase);
        }

        var configs = await _db.external_sync_configs
            .AsNoTracking()
            .Where(x => x.IsActive && modules.Contains(x.ModuleName))
            .ToListAsync(ct);

        return configs.ToDictionary(x => x.ModuleName, x => x, StringComparer.OrdinalIgnoreCase);
    }

    public Task<List<ExternalSyncQueueBatchItem>> GetDuePendingQueueItemsAsync(int take, DateTime nowUtc, CancellationToken ct = default)
    {
        return _db.external_sync_queues
            .AsNoTracking()
            .Where(x => x.Status == ExternalSyncStatus.Pending &&
                        (!x.NextRetryTime.HasValue || x.NextRetryTime <= nowUtc))
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .Select(x => new ExternalSyncQueueBatchItem
            {
                Id = x.Id,
                ModuleName = x.ModuleName,
                EntityId = x.EntityId,
                PayloadJson = x.PayloadJson,
                RetryCount = x.RetryCount,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<bool> TryMarkProcessingAsync(long queueId, DateTime processingAtUtc, CancellationToken ct = default)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE external_sync_queue
               SET status = {ExternalSyncStatus.Processing},
                   last_attempt_at = {processingAtUtc},
                   error_message = NULL
               WHERE id = {queueId}
                 AND status = {ExternalSyncStatus.Pending};", ct);

        return rows == 1;
    }

    public async Task MarkSuccessAsync(long queueId, DateTime processedAtUtc, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE external_sync_queue
               SET status = {ExternalSyncStatus.Success},
                   error_message = NULL,
                   next_retry_time = NULL,
                   last_attempt_at = {processedAtUtc}
               WHERE id = {queueId};", ct);
    }

    public async Task MarkRetryAsync(long queueId, int retryCount, DateTime nextRetryTimeUtc, string errorMessage, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE external_sync_queue
               SET status = {ExternalSyncStatus.Pending},
                   retry_count = {retryCount},
                   next_retry_time = {nextRetryTimeUtc},
                   error_message = {errorMessage},
                   last_attempt_at = {DateTime.UtcNow}
               WHERE id = {queueId};", ct);
    }

    public async Task MarkFailedAsync(long queueId, string errorMessage, DateTime failedAtUtc, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE external_sync_queue
               SET status = {ExternalSyncStatus.Failed},
                   error_message = {errorMessage},
                   last_attempt_at = {failedAtUtc}
               WHERE id = {queueId};", ct);
    }

    public async Task<bool> MoveToDeadLetterByQueueIdAsync(long queueId, string errorMessage, CancellationToken ct = default)
    {
        var queueItem = await _db.external_sync_queues
            .FirstOrDefaultAsync(x => x.Id == queueId, ct);

        if (queueItem == null)
        {
            return false;
        }

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

        await _db.external_sync_dead_letters.AddAsync(dlq, ct);
        _db.external_sync_queues.Remove(queueItem);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> CleanupSuccessfulQueueItemsAsync(DateTime olderThanUtc, int take, CancellationToken ct = default)
    {
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $@"DELETE FROM external_sync_queue
               WHERE id IN (
                   SELECT id
                   FROM external_sync_queue
                   WHERE status = {ExternalSyncStatus.Success}
                     AND created_at < {olderThanUtc}
                   ORDER BY created_at
                   LIMIT {take}
               );", ct);
    }

    public Task<external_sync_queue?> GetQueueByIdAsync(long id, CancellationToken ct = default)
    {
        return _db.external_sync_queues.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<external_sync_dead_letter?> GetDlqByIdAsync(long id, CancellationToken ct = default)
    {
        return _db.external_sync_dead_letters.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task AddQueueAsync(external_sync_queue queueItem, CancellationToken ct = default)
    {
        return _db.external_sync_queues.AddAsync(queueItem, ct).AsTask();
    }

    public Task AddDeadLetterAsync(external_sync_dead_letter dlqItem, CancellationToken ct = default)
    {
        return _db.external_sync_dead_letters.AddAsync(dlqItem, ct).AsTask();
    }

    public Task RemoveQueueAsync(external_sync_queue queueItem, CancellationToken ct = default)
    {
        _db.external_sync_queues.Remove(queueItem);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return _db.SaveChangesAsync(ct);
    }

    public async Task<List<ExternalSyncModuleStatsDto>> GetModuleStatsAsync(CancellationToken ct = default)
    {
        var queueStats = await _db.external_sync_queues
            .AsNoTracking()
            .GroupBy(x => x.ModuleName)
            .Select(g => new
            {
                Module = g.Key,
                Pending = g.Count(x => x.Status == ExternalSyncStatus.Pending),
                Processing = g.Count(x => x.Status == ExternalSyncStatus.Processing),
                Success = g.Count(x => x.Status == ExternalSyncStatus.Success),
                Failed = g.Count(x => x.Status == ExternalSyncStatus.Failed)
            })
            .ToListAsync(ct);

        var dlqStats = await _db.external_sync_dead_letters
            .AsNoTracking()
            .GroupBy(x => x.ModuleName)
            .Select(g => new { Module = g.Key, DLQ = g.Count() })
            .ToListAsync(ct);

        var modules = queueStats.Select(x => x.Module)
            .Union(dlqStats.Select(x => x.Module))
            .Distinct()
            .ToList();

        var result = new List<ExternalSyncModuleStatsDto>(modules.Count);
        foreach (var module in modules)
        {
            var q = queueStats.FirstOrDefault(x => x.Module == module);
            var d = dlqStats.FirstOrDefault(x => x.Module == module);

            result.Add(new ExternalSyncModuleStatsDto
            {
                Module = module,
                Pending = q?.Pending ?? 0,
                Processing = q?.Processing ?? 0,
                Success = q?.Success ?? 0,
                Failed = q?.Failed ?? 0,
                DLQ = d?.DLQ ?? 0
            });
        }

        return result.OrderBy(x => x.Module).ToList();
    }

    public Task<List<external_sync_queue>> GetFailedQueueItemsAsync(int take, CancellationToken ct = default)
    {
        return _db.external_sync_queues
            .AsNoTracking()
            .Where(x => x.Status == ExternalSyncStatus.Failed)
            .OrderByDescending(x => x.LastAttemptAt ?? x.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<List<external_sync_dead_letter>> GetDlqItemsAsync(int take, CancellationToken ct = default)
    {
        return _db.external_sync_dead_letters
            .AsNoTracking()
            .OrderByDescending(x => x.MovedToDLQAt)
            .Take(take)
            .ToListAsync(ct);
    }
}
