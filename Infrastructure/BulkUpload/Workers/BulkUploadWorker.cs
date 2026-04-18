using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

public class BulkUploadWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBulkUploadQueue _queue;
    private readonly ILogger<BulkUploadWorker> _logger;

    public BulkUploadWorker(
        IServiceScopeFactory scopeFactory,
        IBulkUploadQueue queue,
        ILogger<BulkUploadWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.DequeueAllAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();

            try
            {
                var backgroundCurrentUser = scope.ServiceProvider.GetRequiredService<BackgroundCurrentUserContext>();
                backgroundCurrentUser.UserId = workItem.UserId;
                backgroundCurrentUser.AccountId = workItem.AccountId;
                backgroundCurrentUser.RoleId = workItem.RoleId;
                backgroundCurrentUser.Role = workItem.Role;
                backgroundCurrentUser.HierarchyPath = workItem.HierarchyPath;
                backgroundCurrentUser.IsSystemRole = workItem.IsSystemRole;
                backgroundCurrentUser.IsAuthenticated = workItem.IsAuthenticated;
                backgroundCurrentUser.AccessibleAccountIds = workItem.AccessibleAccountIds;

                var processor = scope.ServiceProvider.GetRequiredService<IBulkProcessor>();
                await processor.ProcessAsync(workItem, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk worker failed for JobId={JobId}, Module={Module}", workItem.JobId, workItem.ModuleKey);

                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    var job = await db.bulk_jobs.FirstOrDefaultAsync(x => x.Id == workItem.JobId, stoppingToken);
                    if (job != null)
                    {
                        job.ProcessedRows = workItem.Rows?.Count ?? 0;
                        job.FailedRows = workItem.Rows?.Count ?? 0;
                        job.SuccessRows = 0;
                        job.Status = "COMPLETED_WITH_ERRORS";
                        job.CompletedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception statusEx)
                {
                    _logger.LogError(statusEx, "Failed to mark bulk job {JobId} as failed after worker exception", workItem.JobId);
                }
            }
        }
    }
}
