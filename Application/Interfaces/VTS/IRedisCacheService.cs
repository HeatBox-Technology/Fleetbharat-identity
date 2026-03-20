using System.Threading.Tasks;


public interface IRedisCacheService
{
    Task<bool> SetAsync(string key, object? value, int? ttlSeconds = null);
    Task<string?> GetAsync(string key);

    Task<long> LPushAsync(string listKey, object? value);
    Task<string?> RPopAsync(string listKey);

    Task<bool> HSetAsync(string hashKey, string field, object? value);
    Task<string?> HGetAsync(string hashKey, string field);
}
