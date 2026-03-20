using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IExternalSyncDashboardService
{
    Task<ExternalSyncDashboardDto> GetStatsAsync(CancellationToken ct = default);
    Task<List<ExternalSyncQueueItemDto>> GetFailedAsync(int take = 100, CancellationToken ct = default);
    Task<List<ExternalSyncDlqItemDto>> GetDlqAsync(int take = 100, CancellationToken ct = default);
    Task<bool> RetryFailedAsync(long queueId, CancellationToken ct = default);
    Task<bool> ReprocessDlqAsync(long dlqId, CancellationToken ct = default);
}
