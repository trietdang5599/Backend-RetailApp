using Microsoft.Extensions.Caching.Memory;
using ProductManagement.Application.Interfaces;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ProductManagement.Infrastructure.Caching;

// Fallback when Redis is not configured — supports pattern invalidation
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, bool> _keys = new();

    public InMemoryCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue(key, out string? json);
        var result = json is null ? default : JsonSerializer.Deserialize<T>(json);
        return Task.FromResult(result);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        _cache.Set(key, json, expiry ?? TimeSpan.FromMinutes(15));
        _keys[key] = true;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        var prefix = pattern.TrimEnd('*');
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix)).ToList())
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }
}
