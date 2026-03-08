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

    public Task<List<external_sync_queue>> GetDuePendingQueueItemsAsync(int take, DateTime nowUtc, CancellationToken ct = default)
    {
        return _db.external_sync_queues
            .Where(x => x.Status == ExternalSyncStatus.Pending &&
                        (!x.NextRetryTime.HasValue || x.NextRetryTime <= nowUtc))
            .OrderBy(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
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
