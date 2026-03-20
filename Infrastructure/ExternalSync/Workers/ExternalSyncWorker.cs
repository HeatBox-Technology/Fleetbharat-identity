using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ExternalSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExternalSyncWorker> _logger;
    private readonly IExternalSyncConcurrencyLimiter _concurrencyLimiter;
    private readonly ExternalSyncWorkerOptions _options;
    private int _loopCount;

    public ExternalSyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ExternalSyncWorker> logger,
        IExternalSyncConcurrencyLimiter concurrencyLimiter,
        IOptions<ExternalSyncWorkerOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _concurrencyLimiter = concurrencyLimiter;
        _options = options.Value ?? new ExternalSyncWorkerOptions();
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

                var now = DateTime.UtcNow;
                var dueItems = await repo.GetDuePendingQueueItemsAsync(GetBatchSize(), now, stoppingToken);

                if (dueItems.Count == 0)
                {
                    await RunCleanupAsync(repo, now, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(GetIdleDelaySeconds()), stoppingToken);
                    continue;
                }

                var claimedItems = new List<ExternalSyncQueueBatchItem>(dueItems.Count);
                foreach (var item in dueItems)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (await repo.TryMarkProcessingAsync(item.Id, now, stoppingToken))
                    {
                        claimedItems.Add(item);
                    }
                }

                if (claimedItems.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(GetActiveDelaySeconds()), stoppingToken);
                    continue;
                }

                var configsByModule = await repo.GetActiveConfigsByModuleAsync(
                    claimedItems.Select(x => x.ModuleName),
                    stoppingToken);

                var tasks = claimedItems
                    .Select(item => ProcessSingleItemAsync(item, configsByModule, stoppingToken))
                    .ToArray();

                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ExternalSyncWorker loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(GetActiveDelaySeconds()), stoppingToken);
        }
    }

    private async Task ProcessSingleItemAsync(
        ExternalSyncQueueBatchItem item,
        IReadOnlyDictionary<string, external_sync_config> configsByModule,
        CancellationToken ct)
    {
        using var lease = await _concurrencyLimiter.WaitAsync(ct);

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IExternalSyncRepository>();
        var invoker = scope.ServiceProvider.GetRequiredService<IExternalSyncInvoker>();
        var retryPolicy = scope.ServiceProvider.GetRequiredService<IExternalSyncRetryPolicy>();
        var dlqService = scope.ServiceProvider.GetRequiredService<IExternalDeadLetterService>();

        if (!configsByModule.TryGetValue(item.ModuleName, out var config))
        {
            await repo.MarkFailedAsync(
                item.Id,
                $"No active external_sync_config for module {item.ModuleName}",
                DateTime.UtcNow,
                ct);
            return;
        }

        try
        {
            await invoker.InvokeAsync(config, item.EntityId, item.PayloadJson, ct);
            await repo.MarkSuccessAsync(item.Id, DateTime.UtcNow, ct);
        }
        catch (Exception ex)
        {
            var retryCount = item.RetryCount + 1;
            var errorMessage = TrimError(ex.Message);

            if (!config.RetryEnabled || retryCount >= config.MaxRetryCount)
            {
                await dlqService.MoveToDeadLetterByQueueIdAsync(item.Id, errorMessage, ct);
                return;
            }

            var nextRetryTime = retryPolicy.CalculateNextRetryUtc(
                retryCount,
                DateTime.UtcNow,
                config.RetryIntervalMinutes);

            await repo.MarkRetryAsync(item.Id, retryCount, nextRetryTime, errorMessage, ct);
        }
    }

    private async Task RunCleanupAsync(IExternalSyncRepository repo, DateTime nowUtc, CancellationToken ct)
    {
        _loopCount++;
        if (_loopCount % 10 != 0)
        {
            return;
        }

        var deletedRows = await repo.CleanupSuccessfulQueueItemsAsync(
            nowUtc.AddDays(-GetRetentionDays()),
            GetCleanupBatchSize(),
            ct);

        if (deletedRows > 0)
        {
            _logger.LogInformation("ExternalSync cleanup removed {DeletedRows} old success rows", deletedRows);
        }
    }

    private int GetBatchSize() => Math.Clamp(_options.BatchSize, 1, 100);
    private int GetActiveDelaySeconds() => Math.Clamp(_options.ActiveDelaySeconds, 5, 120);
    private int GetIdleDelaySeconds() => Math.Clamp(_options.IdleDelaySeconds, 10, 300);
    private int GetRetentionDays() => Math.Clamp(_options.SuccessRetentionDays, 1, 90);
    private int GetCleanupBatchSize() => Math.Clamp(_options.CleanupBatchSize, 50, 1000);

    private static string TrimError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unknown sync error";
        }

        const int maxLength = 1800;
        return message.Length > maxLength ? message[..maxLength] : message;
    }
}
