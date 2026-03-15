using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using Newtonsoft.Json;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
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
    private const int RedisBatchSize = 500;
    private readonly IConnectionMultiplexer _mux;
    private readonly IdentityDbContext _db;
    private readonly ILogger<LiveTrackingController> _logger;

    public LiveTrackingController(
        IConnectionMultiplexer mux,
        IdentityDbContext db,
        ILogger<LiveTrackingController> logger)
    {
        _mux = mux;
        _db = db;
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
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { ok = false, message = "Missing Redis key." });

        RedisValue redisValue;
        try
        {
            var db = _mux.GetDatabase();
            redisValue = await db.StringGetAsync(key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection error while fetching live tracking key {Key}", key);
            return StatusCode(503, new { ok = false, message = "Redis is unavailable.", data = (object?)null });
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogError(ex, "Redis timeout while fetching live tracking key {Key}", key);
            return StatusCode(503, new { ok = false, message = "Redis request timed out.", data = (object?)null });
        }

        if (redisValue.IsNullOrEmpty)
            return NotFound(new { ok = false, message = "No live tracking data found for key." });

        // Log the raw Redis value
        _logger.LogInformation("Raw Redis value for key {Key}: {RawValue}", key, redisValue.ToString());

        try
        {
            var dto = JsonConvert.DeserializeObject<DeviceLiveTrackingDto>(redisValue.ToString());
            PopulateVehicleIdFromKey(dto, key);
            PopulateVehicleNoFromKey(dto, key);
            await EnrichFromDatabaseAsync(dto);
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

        RedisKey[] keys;

        if (!string.IsNullOrWhiteSpace(vehicleNos))
        {
            var vehicles = vehicleNos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            keys = vehicles.Select(v => (RedisKey)$"dashboard::{v}").ToArray();
            _logger.LogInformation(
                "LiveTracking batch requested by vehicleNos. Requested vehicles: {Vehicles}. Redis keys: {Keys}",
                vehicles,
                keys.Select(x => x.ToString()).ToArray());
        }
        else
        {
            keys = await GetDashboardKeysByOrgIdAsync(orgId!.Value);
            _logger.LogInformation(
                "LiveTracking batch requested by orgId {OrgId}. Redis key count: {KeyCount}",
                orgId.Value,
                keys.Length);
        }

        if (keys.Length == 0)
        {
            _logger.LogWarning(
                "LiveTracking batch found no Redis keys for vehicleNos {VehicleNos} and orgId {OrgId}",
                vehicleNos,
                orgId);
            return Ok(new { ok = true, data = new List<DeviceLiveTrackingDto>() });
        }

        RedisValue[] redisValues;
        try
        {
            redisValues = await FetchRedisValuesAsync(keys);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection error while fetching batch live tracking for orgId {OrgId} and vehicleNos {VehicleNos}", orgId, vehicleNos);
            return StatusCode(503, new { ok = false, message = "Redis is unavailable.", data = new List<DeviceLiveTrackingDto>() });
        }
        catch (RedisTimeoutException ex)
        {
            _logger.LogError(ex, "Redis timeout while fetching batch live tracking for orgId {OrgId} and vehicleNos {VehicleNos}", orgId, vehicleNos);
            return StatusCode(503, new { ok = false, message = "Redis request timed out.", data = new List<DeviceLiveTrackingDto>() });
        }

        var dtos = new List<DeviceLiveTrackingDto>();
        var dtosNeedingEnrichment = new List<DeviceLiveTrackingDto>();
        var foundRedisKeys = new List<string>();
        var missingRedisKeys = new List<string>();

        for (int i = 0; i < redisValues.Length; i++)
        {
            var val = redisValues[i];
            if (val.IsNullOrEmpty)
            {
                missingRedisKeys.Add(keys[i].ToString());
                continue;
            }

            try
            {
                var dto = JsonConvert.DeserializeObject<DeviceLiveTrackingDto>(val.ToString());
                if (dto == null)
                    continue;
                PopulateVehicleIdFromKey(dto, keys[i]);
                PopulateVehicleNoFromKey(dto, keys[i]);

                if (orgId.HasValue && dto.OrgId != 0 && dto.OrgId != orgId.Value)
                    continue;

                if (NeedsDatabaseEnrichment(dto, orgId))
                {
                    dtosNeedingEnrichment.Add(dto);
                }
                else
                {
                    dto.DriverMapped = false;
                    dto.DataSource = "redis";
                }

                dtos.Add(dto);
                foundRedisKeys.Add(keys[i].ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid redis payload for key {Key}", keys[i]);
            }
        }

        _logger.LogInformation(
            "LiveTracking batch Redis fetch summary. Requested: {RequestedCount}, Found: {FoundCount}, Missing: {MissingCount}, NeedsDbEnrichment: {NeedsDbEnrichment}",
            keys.Length,
            foundRedisKeys.Count,
            missingRedisKeys.Count,
            dtosNeedingEnrichment.Count);

        if (dtosNeedingEnrichment.Count > 0)
        {
            await EnrichFromDatabaseAsync(dtosNeedingEnrichment);
        }

        _logger.LogInformation(
            "LiveTracking batch DB enrichment summary. TotalDtos: {TotalDtos}, OrgMatches: {OrgMatches}, Vehicles: {Vehicles}",
            dtos.Count,
            orgId.HasValue ? dtos.Count(x => x.OrgId == orgId.Value) : dtos.Count,
            dtos.Select(x => new
            {
                x.VehicleNo,
                x.DeviceNo,
                x.Imei,
                x.OrgId,
                x.DataSource
            }).ToArray());

        return Ok(new
        {
            ok = true,
            data = orgId.HasValue
                ? dtos.Where(x => x.OrgId == orgId.Value).ToList()
                : dtos
        });
    }

    private async Task<RedisKey[]> GetDashboardKeysByOrgIdAsync(int orgId)
    {
        var vehicleIds = await _db.VehicleDeviceMaps
            .AsNoTracking()
            .Where(x => x.AccountId == orgId && x.IsActive && !x.IsDeleted)
            .Select(x => x.Fk_VehicleId)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation(
            "LiveTracking DB vehicle lookup for orgId {OrgId}. VehicleIds: {VehicleIds}",
            orgId,
            vehicleIds);

        if (vehicleIds.Count == 0)
            return Array.Empty<RedisKey>();

        return vehicleIds
            .Select(x => (RedisKey)$"dashboard::{x}")
            .ToArray();
    }

    private async Task<RedisValue[]> FetchRedisValuesAsync(RedisKey[] keys)
    {
        var db = _mux.GetDatabase();
        var values = new RedisValue[keys.Length];

        for (int i = 0; i < keys.Length; i += RedisBatchSize)
        {
            var chunk = keys.Skip(i).Take(RedisBatchSize).ToArray();
            var chunkValues = await db.StringGetAsync(chunk);

            for (int j = 0; j < chunkValues.Length; j++)
            {
                values[i + j] = chunkValues[j];
            }
        }

        return values;
    }

    private static void PopulateVehicleIdFromKey(DeviceLiveTrackingDto? dto, RedisKey key)
    {
        if (dto == null || dto.VehicleId.HasValue)
            return;

        PopulateVehicleIdFromKey(dto, key.ToString());
    }

    private static void PopulateVehicleIdFromKey(DeviceLiveTrackingDto? dto, string? key)
    {
        if (dto == null || dto.VehicleId.HasValue || string.IsNullOrWhiteSpace(key))
            return;

        const string prefix = "dashboard::";
        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var suffix = key[prefix.Length..].Trim();
        if (int.TryParse(suffix, out var vehicleId))
            dto.VehicleId = vehicleId;
    }

    private static void PopulateVehicleNoFromKey(DeviceLiveTrackingDto? dto, RedisKey key)
    {
        if (dto == null || !string.IsNullOrWhiteSpace(dto.VehicleNo))
            return;

        PopulateVehicleNoFromKey(dto, key.ToString());
    }

    private static void PopulateVehicleNoFromKey(DeviceLiveTrackingDto? dto, string? key)
    {
        if (dto == null || !string.IsNullOrWhiteSpace(dto.VehicleNo) || string.IsNullOrWhiteSpace(key))
            return;

        const string prefix = "dashboard::";
        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        dto.VehicleNo = key[prefix.Length..].Trim();
    }

    private async Task EnrichFromDatabaseAsync(DeviceLiveTrackingDto? dto)
    {
        if (dto == null)
            return;

        var match = await FindVehicleDeviceMatchAsync(
            dto.VehicleNo,
            dto.DeviceNo,
            dto.Imei);

        ApplyDatabaseMatch(dto, match);
    }

    private async Task EnrichFromDatabaseAsync(List<DeviceLiveTrackingDto> dtos)
    {
        if (dtos.Count == 0)
            return;

        var vehicleNos = dtos
            .Select(x => x.VehicleNo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vehicleIds = dtos
            .Where(x => x.VehicleId.HasValue)
            .Select(x => x.VehicleId!.Value)
            .Distinct()
            .ToList();

        var deviceNos = dtos
            .Select(x => x.DeviceNo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var imeis = dtos
            .Select(x => x.Imei)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (vehicleIds.Count == 0 && vehicleNos.Count == 0 && deviceNos.Count == 0 && imeis.Count == 0)
            return;

        var matches = await _db.VehicleDeviceMaps
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Device)
            .Where(x => x.IsActive && !x.IsDeleted)
            .Where(x =>
                vehicleIds.Contains(x.Fk_VehicleId) ||
                vehicleNos.Contains(x.Vehicle.VehicleNumber) ||
                deviceNos.Contains(x.Device.DeviceNo) ||
                imeis.Contains(x.Device.DeviceImeiOrSerial))
            .Select(x => new VehicleDeviceMatch(
                x.AccountId,
                x.Fk_VehicleId,
                x.Vehicle.VehicleNumber,
                x.Fk_DeviceId,
                x.Device.DeviceNo,
                x.Device.DeviceImeiOrSerial))
            .ToListAsync();

        foreach (var dto in dtos)
        {
            var match = matches.FirstOrDefault(x =>
                (dto.VehicleId.HasValue && x.VehicleId == dto.VehicleId.Value) ||
                IsSame(x.Imei, dto.Imei) ||
                IsSame(x.DeviceNo, dto.DeviceNo) ||
                IsSame(x.VehicleNo, dto.VehicleNo));

            ApplyDatabaseMatch(dto, match);
        }
    }

    private async Task<VehicleDeviceMatch?> FindVehicleDeviceMatchAsync(string? vehicleNo, string? deviceNo, string? imei)
    {
        if (string.IsNullOrWhiteSpace(vehicleNo) &&
            string.IsNullOrWhiteSpace(deviceNo) &&
            string.IsNullOrWhiteSpace(imei))
        {
            return null;
        }

        return await _db.VehicleDeviceMaps
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Device)
            .Where(x => x.IsActive && !x.IsDeleted)
            .Where(x =>
                (!string.IsNullOrWhiteSpace(vehicleNo) && x.Vehicle.VehicleNumber == vehicleNo) ||
                (!string.IsNullOrWhiteSpace(deviceNo) && x.Device.DeviceNo == deviceNo) ||
                (!string.IsNullOrWhiteSpace(imei) && x.Device.DeviceImeiOrSerial == imei))
            .Select(x => new VehicleDeviceMatch(
                x.AccountId,
                x.Fk_VehicleId,
                x.Vehicle.VehicleNumber,
                x.Fk_DeviceId,
                x.Device.DeviceNo,
                x.Device.DeviceImeiOrSerial))
            .FirstOrDefaultAsync();
    }

    private static void ApplyDatabaseMatch(DeviceLiveTrackingDto dto, VehicleDeviceMatch? match)
    {
        if (match == null)
        {
            dto.DriverMapped = false;
            dto.DataSource = "redis";
            return;
        }

        dto.OrgId = match.AccountId;
        dto.VehicleId = match.VehicleId;
        dto.DeviceId = match.DeviceId;
        dto.VehicleNo = match.VehicleNo;
        dto.DeviceNo = match.DeviceNo;
        dto.Imei = match.Imei;
        dto.DriverMapped = false;
        dto.DataSource = "redis+postgres";
    }

    private static bool IsSame(string? left, string? right)
    {
        return !string.IsNullOrWhiteSpace(left) &&
               !string.IsNullOrWhiteSpace(right) &&
               string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool NeedsDatabaseEnrichment(DeviceLiveTrackingDto dto, int? requestedOrgId)
    {
        if (dto.VehicleId.HasValue &&
            !string.IsNullOrWhiteSpace(dto.VehicleNo) &&
            (!string.IsNullOrWhiteSpace(dto.DeviceNo) || !string.IsNullOrWhiteSpace(dto.Imei)))
        {
            if (!requestedOrgId.HasValue || dto.OrgId == requestedOrgId.Value)
                return false;
        }

        if (dto.VehicleId.HasValue &&
            requestedOrgId.HasValue &&
            dto.OrgId == requestedOrgId.Value)
        {
            return false;
        }

        return true;
    }

    private sealed record VehicleDeviceMatch(
        int AccountId,
        int VehicleId,
        string VehicleNo,
        int DeviceId,
        string DeviceNo,
        string Imei);
}
