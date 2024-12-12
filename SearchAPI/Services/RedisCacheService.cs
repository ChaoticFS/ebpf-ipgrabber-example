using StackExchange.Redis;
using System.Text.Json;

namespace SearchAPI.Services;
public class RedisCacheService : ICacheService
{
    private readonly IDatabase _cache;
    private readonly IConfiguration _configuration;

    public RedisCacheService(IConfiguration configuration)
    {
        _configuration = configuration;

        var options = ConfigurationOptions.Parse(_configuration["Redis:ConnectionString"]);
        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
        _cache = connectionMultiplexer.GetDatabase();
    }
    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _cache.StringGetAsync(key);
        return json.HasValue ? JsonSerializer.Deserialize<T>(json!) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var json = JsonSerializer.Serialize(value);
        await _cache.StringSetAsync(key, json, expiration);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.KeyDeleteAsync(key);
    }

    public async Task ClearAsync()
    {
        await _cache.ExecuteAsync("FLUSHDB");
    }
}
