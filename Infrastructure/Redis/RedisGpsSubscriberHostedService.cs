using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace Infrastructure.Redis;

public class RedisGpsSubscriberHostedService : BackgroundService
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IHubContext<TrackingHub> _hub;
    private readonly IConfiguration _config;
    private readonly ILogger<RedisGpsSubscriberHostedService> _logger;

    public RedisGpsSubscriberHostedService(
        IConnectionMultiplexer mux,
        IHubContext<TrackingHub> hub,
        IConfiguration config,
        ILogger<RedisGpsSubscriberHostedService> logger)
    {
        _mux = mux;
        _hub = hub;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var prefix = _config["Redis:ChannelPrefix"] ?? "gps";
        var patternChannel = RedisChannel.Pattern($"{prefix}:*");

        // Exponential backoff
        var delay = TimeSpan.FromSeconds(2);
        var maxDelay = TimeSpan.FromSeconds(30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_mux.IsConnected)
                {
                    _logger.LogWarning("Redis not connected. Will retry subscribe in {Delay}s.", delay.TotalSeconds);
                    await Task.Delay(delay, stoppingToken);
                    delay = NextDelay(delay, maxDelay);
                    continue;
                }

                var subscriber = _mux.GetSubscriber();

                _logger.LogInformation("Subscribing to Redis pattern: {Pattern}", $"{prefix}:*");

                await subscriber.SubscribeAsync(patternChannel, (channel, message) =>
                {
                    // Fire-and-forget safely (do not throw on redis thread)
                    _ = HandleMessageAsync(channel, message, stoppingToken);
                });

                _logger.LogInformation("Redis subscription active.");

                // reset backoff on success
                delay = TimeSpan.FromSeconds(2);

                // Keep alive until cancellation; if redis drops, ConnectionFailed handler logs it
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // graceful stop
            }
            catch (RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis connection error while subscribing. Retrying in {Delay}s.", delay.TotalSeconds);
            }
            catch (RedisTimeoutException ex)
            {
                _logger.LogWarning(ex, "Redis timeout while subscribing. Retrying in {Delay}s.", delay.TotalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Redis subscriber. Retrying in {Delay}s.", delay.TotalSeconds);
            }

            await Task.Delay(delay, stoppingToken);
            delay = NextDelay(delay, maxDelay);
        }
    }

    private async Task HandleMessageAsync(RedisChannel channel, RedisValue message, CancellationToken ct)
    {
        try
        {
            var channelStr = channel.ToString();
            var parts = channelStr.Split(':', 2);
            if (parts.Length != 2) return;

            var deviceId = parts[1];
            if (string.IsNullOrWhiteSpace(deviceId)) return;

            await _hub.Clients.Group($"device:{deviceId}")
                .SendAsync("gps_update", message.ToString(), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // ignore on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling redis message for channel {Channel}", channel.ToString());
        }
    }

    private static TimeSpan NextDelay(TimeSpan current, TimeSpan max)
    {
        var nextSeconds = Math.Min(max.TotalSeconds, current.TotalSeconds * 2);
        return TimeSpan.FromSeconds(nextSeconds);
    }
}
