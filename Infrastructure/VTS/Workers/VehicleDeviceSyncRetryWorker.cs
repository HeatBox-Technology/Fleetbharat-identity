using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class VehicleDeviceSyncRetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VehicleDeviceSyncRetryWorker> _logger;
    private readonly VehicleDeviceSyncRetryOptions _options;

    public VehicleDeviceSyncRetryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<VehicleDeviceSyncRetryWorker> logger,
        IOptions<VehicleDeviceSyncRetryOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value ?? new VehicleDeviceSyncRetryOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("VehicleDeviceSyncRetryWorker is disabled");
            return;
        }

        _logger.LogInformation("VehicleDeviceSyncRetryWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessPendingRetriesAsync(stoppingToken);
                var delaySeconds = processed > 0 ? GetActiveDelaySeconds() : GetIdleDelaySeconds();
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in VehicleDeviceSyncRetryWorker loop");
                await Task.Delay(TimeSpan.FromSeconds(GetActiveDelaySeconds()), stoppingToken);
            }
        }
    }

    private async Task<int> ProcessPendingRetriesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var externalApi = scope.ServiceProvider.GetRequiredService<IExternalMappingApiService>();

        var nowUtc = DateTime.UtcNow;
        var retryCutoff = nowUtc.AddMinutes(-GetRetryIntervalMinutes());

        var pendingLogs = await db.map_vehicle_device_sync_logs
            .Where(x => !x.IsSynced)
            .Where(x => x.RetryCount < GetMaxRetryCount())
            .Where(x => !x.LastTriedAt.HasValue || x.LastTriedAt <= retryCutoff)
            .OrderBy(x => x.LastTriedAt ?? x.CreatedAt)
            .Take(GetBatchSize())
            .ToListAsync(ct);

        foreach (var log in pendingLogs)
        {
            List<ExternalVehicleMappingRequest>? payload = null;

            try
            {
                payload = JsonSerializer.Deserialize<List<ExternalVehicleMappingRequest>>(log.PayloadJson);
                if (payload == null || payload.Count == 0)
                {
                    log.RetryCount += 1;
                    log.ErrorMessage = "Stored vehicle mapping payload is empty or invalid.";
                    log.LastTriedAt = DateTime.UtcNow;
                    continue;
                }

                var result = await externalApi.SendVehicleMappingAsync(payload);
                log.LastTriedAt = DateTime.UtcNow;

                if (result)
                {
                    log.IsSynced = true;
                    log.ErrorMessage = null;
                    _logger.LogInformation(
                        "Vehicle mapping sync retry succeeded for SyncLogId {SyncLogId}, MappingId {MappingId}",
                        log.Id,
                        log.MappingId);
                    continue;
                }

                log.RetryCount += 1;
                log.ErrorMessage = TrimError("External vehicle mapping API returned an unsuccessful status.");

                _logger.LogWarning(
                    "Vehicle mapping sync retry failed for SyncLogId {SyncLogId}, MappingId {MappingId}, Attempt {Attempt}/{MaxRetry}: {Error}",
                    log.Id,
                    log.MappingId,
                    log.RetryCount,
                    GetMaxRetryCount(),
                    log.ErrorMessage);
            }
            catch (JsonException ex)
            {
                log.RetryCount += 1;
                log.LastTriedAt = DateTime.UtcNow;
                log.ErrorMessage = TrimError($"Payload deserialization failed: {ex.Message}");

                _logger.LogError(
                    ex,
                    "Vehicle mapping retry payload could not be deserialized for SyncLogId {SyncLogId}",
                    log.Id);
            }
            catch (Exception ex)
            {
                log.RetryCount += 1;
                log.LastTriedAt = DateTime.UtcNow;
                log.ErrorMessage = TrimError(ex.Message);

                _logger.LogError(
                    ex,
                    "Vehicle mapping sync retry threw an exception for SyncLogId {SyncLogId}, MappingId {MappingId}",
                    log.Id,
                    log.MappingId);
            }
        }

        if (pendingLogs.Count > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return pendingLogs.Count;
    }

    private int GetBatchSize() => Math.Clamp(_options.BatchSize, 1, 100);
    private int GetMaxRetryCount() => Math.Clamp(_options.MaxRetryCount, 1, 20);
    private int GetRetryIntervalMinutes() => Math.Clamp(_options.RetryIntervalMinutes, 1, 1440);
    private int GetActiveDelaySeconds() => Math.Clamp(_options.ActiveDelaySeconds, 5, 300);
    private int GetIdleDelaySeconds() => Math.Clamp(_options.IdleDelaySeconds, 10, 600);

    private static string TrimError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "Unknown vehicle mapping sync error";
        }

        const int maxLength = 1800;
        return message.Length > maxLength ? message[..maxLength] : message;
    }
}
