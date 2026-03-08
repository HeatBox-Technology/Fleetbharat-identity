using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExternalSyncWorker> _logger;

    public ExternalSyncWorker(IServiceScopeFactory scopeFactory, ILogger<ExternalSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExternalSyncWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IExternalSyncRepository>();
                var invoker = scope.ServiceProvider.GetRequiredService<IExternalSyncInvoker>();
                var retryPolicy = scope.ServiceProvider.GetRequiredService<IExternalSyncRetryPolicy>();
                var dlqService = scope.ServiceProvider.GetRequiredService<IExternalDeadLetterService>();

                var now = DateTime.UtcNow;
                var dueItems = await repo.GetDuePendingQueueItemsAsync(100, now, stoppingToken);

                foreach (var item in dueItems)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    var config = await repo.GetActiveConfigAsync(item.ModuleName, stoppingToken);
                    if (config == null)
                    {
                        item.Status = ExternalSyncStatus.Failed;
                        item.ErrorMessage = $"No active external_sync_config for module {item.ModuleName}";
                        item.LastAttemptAt = DateTime.UtcNow;
                        await repo.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    try
                    {
                        item.Status = ExternalSyncStatus.Processing;
                        item.LastAttemptAt = DateTime.UtcNow;
                        await repo.SaveChangesAsync(stoppingToken);

                        await invoker.InvokeAsync(config, item, stoppingToken);

                        item.Status = ExternalSyncStatus.Success;
                        item.ErrorMessage = null;
                        item.NextRetryTime = null;
                        await repo.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        item.RetryCount += 1;
                        item.ErrorMessage = ex.Message;
                        item.LastAttemptAt = DateTime.UtcNow;

                        if (!config.RetryEnabled || item.RetryCount >= config.MaxRetryCount)
                        {
                            await dlqService.MoveToDeadLetterAsync(item, ex.Message, stoppingToken);
                            continue;
                        }

                        item.Status = ExternalSyncStatus.Pending;
                        item.NextRetryTime = retryPolicy.CalculateNextRetryUtc(
                            item.RetryCount,
                            DateTime.UtcNow,
                            config.RetryIntervalMinutes);

                        await repo.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ExternalSyncWorker loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
