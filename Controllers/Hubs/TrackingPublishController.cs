using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("api/tracking")]

public class TrackingPublishController : ControllerBase
{
    private readonly IConnectionMultiplexer _mux;
    private readonly IConfiguration _config;

    public TrackingPublishController(IConnectionMultiplexer mux, IConfiguration config)
    {
        _mux = mux;
        _config = config;
    }

    /// <summary>
    /// Publish a GPS update to Redis Pub/Sub (Channel: gps:{deviceId})
    /// </summary>
    /// <remarks>
    /// Use this endpoint when GPS data first arrives in this API (from device gateway, TCP server, webhook, etc.).
    ///
    /// Flow:
    /// API receives GPS → Publish to Redis channel "gps:{deviceId}" → .NET Redis subscriber broadcasts via SignalR → Next.js UI updates live map.
    ///
    /// Example channel:
    /// gps:123
    ///
    /// Example payload:
    /// {
    ///   "deviceId": "123",
    ///   "lat": 28.6139,
    ///   "lng": 77.2090,
    ///   "speed": 42,
    ///   "ts": "2026-02-09T21:35:00+05:30"
    /// }
    /// </remarks>
    [HttpPost("publish")]


    public async Task<IActionResult> Publish([FromBody] GpsDto dto)
    {
        var prefix = _config["Redis:ChannelPrefix"] ?? "gps";
        var channel = $"{prefix}:{dto.DeviceId}";
        var payload = JsonSerializer.Serialize(dto);

        var sub = _mux.GetSubscriber();
        await sub.PublishAsync(RedisChannel.Literal(channel), payload);

        return Ok(new { ok = true, channel });
    }
}

public class GpsDto
{
    public string DeviceId { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double? Speed { get; set; }
    public DateTimeOffset? Ts { get; set; }
}
