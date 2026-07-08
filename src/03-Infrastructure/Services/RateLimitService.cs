using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using EfCore.Enterprise.Shared.Exceptions;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IRateLimitService
{
    Task<bool> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken ct = default);
    Task<bool> IsBlacklistedAsync(string ip, CancellationToken ct = default);
    Task AddToBlacklistAsync(string ip, TimeSpan duration, CancellationToken ct = default);
}

public class RateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, RateLimitEntry> _counters = new();
    private readonly ConcurrentDictionary<string, DateTime> _blacklist = new();
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(ILogger<RateLimitService> logger)
    {
        _logger = logger;
    }

    public Task<bool> CheckAsync(
        string key,
        int maxRequests,
        TimeSpan window,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var entry = _counters.GetOrAdd(key, _ => new RateLimitEntry(now, 0));

        lock (entry)
        {
            if (now - entry.WindowStart > window)
            {
                entry.WindowStart = now;
                entry.Count = 0;
            }

            entry.Count++;

            if (entry.Count > maxRequests)
            {
                _logger.LogWarning("限流触发: {Key}, 请求�? {Count}", key, entry.Count);
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(true);
    }

    public Task<bool> IsBlacklistedAsync(string ip, CancellationToken ct = default)
    {
        if (_blacklist.TryGetValue(ip, out var expiry))
        {
            if (DateTime.UtcNow < expiry) return Task.FromResult(true);
            _blacklist.TryRemove(ip, out _);
        }
        return Task.FromResult(false);
    }

    public Task AddToBlacklistAsync(string ip, TimeSpan duration, CancellationToken ct = default)
    {
        _blacklist[ip] = DateTime.UtcNow.Add(duration);
        _logger.LogWarning("IP已加入黑名单: {Ip}, 时长: {Duration}", ip, duration);
        return Task.CompletedTask;
    }
}

public class RateLimitEntry
{
    public DateTime WindowStart { get; set; }
    public int Count { get; set; }

    public RateLimitEntry(DateTime windowStart, int count)
    {
        WindowStart = windowStart;
        Count = count;
    }
}