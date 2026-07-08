using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EfCore.Enterprise.Infrastructure.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null, CancellationToken ct = default);
    Task WarmUpAsync<T>(string key, Func<Task<T>> factory, CancellationToken ct = default);
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await _cache.GetStringAsync(key, ct);
        return data == null ? default : JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
        {
            var randomOffset = TimeSpan.FromMilliseconds(new Random().Next(0, 5000));
            options.AbsoluteExpirationRelativeToNow = expiry.Value + randomOffset;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        }

        var data = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, data, options, ct);
        _logger.LogDebug("缓存写入: {Key}", key);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        return await _cache.GetStringAsync(key, ct) != null;
    }

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default)
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached != null)
        {
            _logger.LogDebug("缓存命中: {Key}", key);
            return cached;
        }

        var lockSem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await lockSem.WaitAsync(ct);
        try
        {
            cached = await GetAsync<T>(key, ct);
            if (cached != null) return cached;

            var value = await factory();
            if (value != null)
            {
                await SetAsync(key, value, expiry, ct);
            }
            else
            {
                await SetNullValueAsync(key, ct);
            }
            return value;
        }
        finally
        {
            lockSem.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public async Task WarmUpAsync<T>(
        string key,
        Func<Task<T>> factory,
        CancellationToken ct = default)
    {
        _logger.LogInformation("缓存预热: {Key}", key);
        await GetOrSetAsync(key, factory, TimeSpan.FromHours(24), ct);
    }

    private async Task SetNullValueAsync(string key, CancellationToken ct)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(key, "NULL_VALUE", options, ct);
    }
}