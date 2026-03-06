using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
                var processor = scope.ServiceProvider.GetRequiredService<IBulkProcessor>();
                await processor.ProcessAsync(workItem, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk worker failed for JobId={JobId}, Module={Module}", workItem.JobId, workItem.ModuleKey);
            }
        }
    }
}
