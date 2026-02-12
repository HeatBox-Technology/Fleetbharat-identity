using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Controller;

[ApiController]
[Route("api/redis")]
public class RedisController : ControllerBase
{
    private readonly IRedisCacheService _redis;

    public RedisController(IRedisCacheService redis)
    {
        _redis = redis;
    }

    [HttpPost("set")]
    public async Task<IActionResult> Set([FromBody] RedisSetRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key))
            return BadRequest("Key is required");

        var ok = await _redis.SetAsync(dto.Key, dto.Value, dto.TtlSeconds);
        return Ok(new { ok });
    }

    [HttpGet("get")]
    public async Task<IActionResult> Get([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest("key is required");

        var val = await _redis.GetAsync(key);
        return Ok(new { key, value = val });
    }

    [HttpPost("lpush")]
    public async Task<IActionResult> LPush([FromBody] RedisListPushRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ListKey))
            return BadRequest("ListKey is required");

        var len = await _redis.LPushAsync(dto.ListKey, dto.Value);
        return Ok(new { ok = true, newLen = len });
    }

    [HttpPost("hset")]
    public async Task<IActionResult> HSet([FromBody] RedisHashSetRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.HashKey) || string.IsNullOrWhiteSpace(dto.Field))
            return BadRequest("HashKey and Field are required");

        var ok = await _redis.HSetAsync(dto.HashKey, dto.Field, dto.Value);
        return Ok(new { ok });
    }
}
