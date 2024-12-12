using System.Collections.Concurrent;

namespace SearchAPI.Services;
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (object Value, DateTime Expiry)> _cache;

    public InMemoryCacheService()
    {
        _cache = new ConcurrentDictionary<string, (object Value, DateTime Expiry)>();
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > DateTime.UtcNow)
            {
                return Task.FromResult((T?)entry.Value);
            }

            _cache.TryRemove(key, out _);
        }

        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var expiry = DateTime.UtcNow.Add(expiration);
        _cache[key] = (value!, expiry);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }
}
