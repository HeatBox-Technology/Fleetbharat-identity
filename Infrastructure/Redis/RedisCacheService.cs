using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace Infrastructure.Redis;

public class RedisCacheService : IRedisCacheService
{
    private readonly IDatabase _db;
    private readonly int _defaultTtlSeconds;

    public RedisCacheService(IConnectionMultiplexer mux, IConfiguration configuration)
    {
        _db = mux.GetDatabase();

        // optional default TTL
        _defaultTtlSeconds = int.TryParse(configuration["Redis:DefaultTtlSeconds"], out var ttl)
            ? ttl
            : 0;
    }

    public async Task<bool> SetAsync(string key, object? value, int? ttlSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key is required", nameof(key));

        var payload = Serialize(value);

        var ttl = ttlSeconds ?? _defaultTtlSeconds;
        TimeSpan? expiry = ttl > 0 ? TimeSpan.FromSeconds(ttl) : null;

        if (expiry.HasValue)
            return await _db.StringSetAsync(key, payload, expiry.Value);

        return await _db.StringSetAsync(key, payload);
    }


    public async Task<string?> GetAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required", nameof(key));

        var val = await _db.StringGetAsync(key);
        return val.HasValue ? val.ToString() : null;
    }

    public async Task<long> LPushAsync(string listKey, object? value)
    {
        if (string.IsNullOrWhiteSpace(listKey)) throw new ArgumentException("ListKey is required", nameof(listKey));

        var payload = Serialize(value);
        return await _db.ListLeftPushAsync(listKey, payload);
    }

    public async Task<string?> RPopAsync(string listKey)
    {
        if (string.IsNullOrWhiteSpace(listKey)) throw new ArgumentException("ListKey is required", nameof(listKey));

        var val = await _db.ListRightPopAsync(listKey);
        return val.HasValue ? val.ToString() : null;
    }

    public async Task<bool> HSetAsync(string hashKey, string field, object? value)
    {
        if (string.IsNullOrWhiteSpace(hashKey)) throw new ArgumentException("HashKey is required", nameof(hashKey));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Field is required", nameof(field));

        var payload = Serialize(value);
        await _db.HashSetAsync(hashKey, field, payload);
        return true;
    }

    public async Task<string?> HGetAsync(string hashKey, string field)
    {
        if (string.IsNullOrWhiteSpace(hashKey)) throw new ArgumentException("HashKey is required", nameof(hashKey));
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException("Field is required", nameof(field));

        var val = await _db.HashGetAsync(hashKey, field);
        return val.HasValue ? val.ToString() : null;
    }

    private static string Serialize(object? value)
    {
        if (value is null) return string.Empty;
        if (value is string s) return s;

        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }
}
