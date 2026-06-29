using Microsoft.Extensions.Caching.Distributed;
using ProductManagement.Application.Interfaces;
using System.Text.Json;

namespace ProductManagement.Infrastructure.Caching;

// Fallback when Redis is not configured
public class InMemoryCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public InMemoryCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var json = await _cache.GetStringAsync(key, ct);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(15)
        }, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await _cache.RemoveAsync(key, ct);

    public Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        // In-memory distributed cache doesn't support pattern deletion
        // In production, always use Redis
        return Task.CompletedTask;
    }
}
