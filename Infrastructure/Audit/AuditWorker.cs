using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class AuditWorker : BackgroundService
{
    private readonly AuditQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditWorker> _logger;
    private readonly AuditLoggingOptions _options;

    public AuditWorker(
        AuditQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<AuditLoggingOptions> options,
        ILogger<AuditWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        var batch = new List<AuditLog>(Math.Max(1, _options.BatchSize));
        var flushInterval = TimeSpan.FromMilliseconds(Math.Max(100, _options.FlushIntervalMs));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var hasData = await _queue.Reader.WaitToReadAsync(stoppingToken);
                if (!hasData)
                    continue;

                batch.Clear();
                var deadline = DateTime.UtcNow.Add(flushInterval);

                while (batch.Count < _options.BatchSize && DateTime.UtcNow <= deadline)
                {
                    while (batch.Count < _options.BatchSize && _queue.Reader.TryRead(out var item))
                    {
                        batch.Add(item);
                    }

                    if (batch.Count >= _options.BatchSize)
                        break;

                    if (DateTime.UtcNow > deadline)
                        break;

                    await Task.Delay(25, stoppingToken);
                }

                if (batch.Count > 0)
                {
                    await SaveWithRetryAsync(batch, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit worker loop failed.");
                await Task.Delay(200, stoppingToken);
            }
        }
    }

    private async Task SaveWithRetryAsync(IReadOnlyCollection<AuditLog> batch, CancellationToken cancellationToken)
    {
        var maxRetry = Math.Max(1, _options.MaxRetryCount);

        for (var attempt = 1; attempt <= maxRetry; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IAuditLogStore>();
                await store.SaveBatchAsync(batch, cancellationToken);
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxRetry)
            {
                var delayMs = 100 * attempt;
                _logger.LogWarning(ex, "Transient failure while saving audit batch. Retrying attempt {Attempt}/{MaxRetry}.", attempt, maxRetry);
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception ex) =>
        ex is TimeoutException || ex is DbUpdateException;
}
