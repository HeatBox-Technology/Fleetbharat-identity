using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncDashboardService : IExternalSyncDashboardService
{
    private readonly IExternalSyncRepository _repo;

    public ExternalSyncDashboardService(IExternalSyncRepository repo)
    {
        _repo = repo;
    }

    public async Task<ExternalSyncDashboardDto> GetStatsAsync(CancellationToken ct = default)
    {
        var items = await _repo.GetModuleStatsAsync(ct);
        return new ExternalSyncDashboardDto { Items = items };
    }

    public async Task<List<ExternalSyncQueueItemDto>> GetFailedAsync(int take = 100, CancellationToken ct = default)
    {
        var rows = await _repo.GetFailedQueueItemsAsync(take, ct);
        return rows.Select(x => new ExternalSyncQueueItemDto
        {
            Id = x.Id,
            ModuleName = x.ModuleName,
            EntityId = x.EntityId,
            Status = x.Status,
            RetryCount = x.RetryCount,
            CreatedAt = x.CreatedAt,
            LastAttemptAt = x.LastAttemptAt,
            ErrorMessage = x.ErrorMessage
        }).ToList();
    }

    public async Task<List<ExternalSyncDlqItemDto>> GetDlqAsync(int take = 100, CancellationToken ct = default)
    {
        var rows = await _repo.GetDlqItemsAsync(take, ct);
        return rows.Select(x => new ExternalSyncDlqItemDto
        {
            Id = x.Id,
            ModuleName = x.ModuleName,
            EntityId = x.EntityId,
            RetryCount = x.RetryCount,
            ErrorMessage = x.ErrorMessage,
            CreatedAt = x.CreatedAt,
            MovedToDLQAt = x.MovedToDLQAt
        }).ToList();
    }

    public async Task<bool> RetryFailedAsync(long queueId, CancellationToken ct = default)
    {
        var row = await _repo.GetQueueByIdAsync(queueId, ct);
        if (row == null || row.Status != ExternalSyncStatus.Failed)
            return false;

        row.Status = ExternalSyncStatus.Pending;
        row.ErrorMessage = null;
        row.NextRetryTime = System.DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReprocessDlqAsync(long dlqId, CancellationToken ct = default)
    {
        var dlq = await _repo.GetDlqByIdAsync(dlqId, ct);
        if (dlq == null)
            return false;

        await _repo.AddQueueAsync(new external_sync_queue
        {
            ModuleName = dlq.ModuleName,
            EntityId = dlq.EntityId,
            PayloadJson = dlq.PayloadJson,
            Status = ExternalSyncStatus.Pending,
            RetryCount = 0,
            NextRetryTime = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return true;
    }
}
