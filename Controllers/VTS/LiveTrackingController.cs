using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using Newtonsoft.Json;
using Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Controller;

[ApiController]
[Route("api/live-tracking")]
[AllowAnonymous]
public class LiveTrackingController : ControllerBase
{
    private readonly IConnectionMultiplexer _mux;
    private readonly ILogger<LiveTrackingController> _logger;

    public LiveTrackingController(IConnectionMultiplexer mux, ILogger<LiveTrackingController> logger)
    {
        _mux = mux;
        _logger = logger;
    }

    /// <summary>
    /// Get live tracking data for a vehicle/device from Redis using a key.
    /// </summary>
    /// <param name="key">Redis key (e.g. dashboard::AP11AA1111)</param>
    /// <returns>Device live tracking data</returns>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string key)
    {
        var db = _mux.GetDatabase();
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { ok = false, message = "Missing Redis key." });

        var redisValue = await db.StringGetAsync(key);
        if (redisValue.IsNullOrEmpty)
            return NotFound(new { ok = false, message = "No live tracking data found for key." });

        // Log the raw Redis value
        _logger.LogInformation("Raw Redis value for key {Key}: {RawValue}", key, redisValue.ToString());

        try
        {
            var dto = JsonConvert.DeserializeObject<DeviceLiveTrackingDto>(redisValue.ToString());
            _logger.LogInformation("Live tracking response for key {Key}: {@Dto}", key, dto);
            if (dto == null || dto.Latitude == 0 || dto.Longitude == 0)
            {
                return BadRequest(new { ok = false, message = "Invalid or missing coordinates in live tracking data.", data = dto });
            }
            return Ok(new { ok = true, data = dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing live tracking data for key {Key}", key);
            return BadRequest(new { ok = false, message = "Invalid data format.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get live tracking data for multiple vehicles from Redis.
    /// </summary>
    /// <param name="vehicleNos">Comma-separated vehicle numbers</param>
    /// <param name="orgId">Organization id</param>
    /// <returns>List of DeviceLiveTrackingDto</returns>
    [HttpGet("batch")]
    public async Task<IActionResult> GetBatch([FromQuery] string? vehicleNos, [FromQuery] int? orgId)
    {
        if (string.IsNullOrWhiteSpace(vehicleNos) && !orgId.HasValue)
            return BadRequest(new { ok = false, message = "Pass either vehicleNos or orgId parameter." });

        var db = _mux.GetDatabase();
        RedisKey[] keys;

        if (!string.IsNullOrWhiteSpace(vehicleNos))
        {
            var vehicles = vehicleNos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            keys = vehicles.Select(v => (RedisKey)$"dashboard::{v}").ToArray();
        }
        else
        {
            keys = await GetDashboardKeysAsync();
        }

        if (keys.Length == 0)
            return Ok(new { ok = true, data = new List<DeviceLiveTrackingDto>() });

        var redisValues = await db.StringGetAsync(keys);
        var result = new List<DeviceLiveTrackingDto>();

        for (int i = 0; i < redisValues.Length; i++)
        {
            var val = redisValues[i];
            if (val.IsNullOrEmpty)
                continue;

            try
            {
                var dto = JsonConvert.DeserializeObject<DeviceLiveTrackingDto>(val.ToString());
                if (dto == null)
                    continue;

                if (orgId.HasValue && dto.OrgId != orgId.Value)
                    continue;

                result.Add(dto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid redis payload for key {Key}", keys[i]);
            }
        }

        return Ok(new { ok = true, data = result });
    }

    private Task<RedisKey[]> GetDashboardKeysAsync()
    {
        var endpoints = _mux.GetEndPoints();
        var keys = new List<RedisKey>();

        foreach (var endpoint in endpoints)
        {
            var server = _mux.GetServer(endpoint);
            if (!server.IsConnected)
                continue;

            var endpointKeys = server.Keys(pattern: "dashboard::*").ToArray();
            if (endpointKeys.Length > 0)
                keys.AddRange(endpointKeys);
        }

        return Task.FromResult(keys.Distinct().ToArray());
    }
}
