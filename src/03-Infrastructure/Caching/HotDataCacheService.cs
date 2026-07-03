using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Caching;

public interface IHotDataCacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiry = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
    T? GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiry = null);
}

[Injectable(ServiceLifetime.Singleton)]
public class HotDataCacheService : IHotDataCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<HotDataCacheService> _logger;
    private readonly Timer _cleanupTimer;

    public HotDataCacheService(ILogger<HotDataCacheService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.IsExpired)
            {
                _cache.TryRemove(key, out _);
                return default;
            }
            return (T)entry.Value;
        }
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var expireAt = expiry.HasValue ? DateTime.UtcNow.Add(expiry.Value) : (DateTime?)null;
        _cache[key] = new CacheEntry(value!, expireAt);
    }

    public void Remove(string key)
    {
        _cache.TryRemove(key, out _);
    }

    public void RemoveByPrefix(string prefix)
    {
        var keys = _cache.Keys.Where(k => k.StartsWith(prefix)).ToList();
        foreach (var key in keys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public T? GetOrSet<T>(string key, Func<T> factory, TimeSpan? expiry = null)
    {
        var value = Get<T>(key);
        if (value != null) return value;

        value = factory();
        Set(key, value, expiry);
        return value;
    }

    private void CleanupExpired(object? state)
    {
        var expiredKeys = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("清理过期热点缓存 {Count} 项", expiredKeys.Count);
        }
    }

    private class CacheEntry
    {
        public object Value { get; }
        public DateTime? ExpireAt { get; }

        public CacheEntry(object value, DateTime? expireAt)
        {
            Value = value;
            ExpireAt = expireAt;
        }

        public bool IsExpired => ExpireAt.HasValue && DateTime.UtcNow > ExpireAt.Value;
    }
}