using EfCore.Enterprise.Shared.DependencyInjection;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IIdempotencyService
{
    Task<bool> IsDuplicateAsync(string key, CancellationToken ct = default);
    Task MarkProcessedAsync(string key, TimeSpan? ttl = null, CancellationToken ct = default);
    Task<string> GenerateIdempotencyKeyAsync(string data, CancellationToken ct = default);
}

[Injectable(ServiceLifetime.Scoped)]
public class IdempotencyService : IIdempotencyService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(IDistributedCache cache, ILogger<IdempotencyService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsDuplicateAsync(string key, CancellationToken ct = default)
    {
        var existing = await _cache.GetStringAsync(key, ct);
        return existing != null;
    }

    public async Task MarkProcessedAsync(string key, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromHours(24)
        };
        await _cache.SetStringAsync(key, "PROCESSED", options, ct);
        _logger.LogDebug("幂等标记已设�? {Key}", key);
    }

    public Task<string> GenerateIdempotencyKeyAsync(string data, CancellationToken ct = default)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Task.FromResult($"IDEMP-{Convert.ToHexString(hash)}");
    }
}
