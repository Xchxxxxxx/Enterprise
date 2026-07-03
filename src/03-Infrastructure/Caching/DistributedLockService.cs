using EfCore.Enterprise.Shared.DependencyInjection;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Caching;

public interface IDistributedLockService
{
    Task<LockHandle?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct = default);
    Task ReleaseAsync(LockHandle handle);
}

[Injectable(ServiceLifetime.Scoped)]
public class DistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<DistributedLockService> _logger;

    public DistributedLockService(IServiceProvider serviceProvider, ILogger<DistributedLockService> logger)
    {
        _redis = serviceProvider.GetService<IConnectionMultiplexer>();
        _logger = logger;
    }

    public async Task<LockHandle?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct = default)
    {
        if (_redis == null) return new LockHandle(key, "local", Guid.NewGuid().ToString());

        var db = _redis.GetDatabase();
        var token = Guid.NewGuid().ToString();
        var acquired = await db.StringSetAsync(key, token, timeout, When.NotExists);

        if (acquired)
        {
            _logger.LogDebug("分布式锁获取成功: {Key}", key);
            return new LockHandle(key, token, token);
        }

        return null;
    }

    public async Task ReleaseAsync(LockHandle handle)
    {
        if (_redis == null || handle.Token == "local") return;

        var db = _redis.GetDatabase();
        var script = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        await db.ScriptEvaluateAsync(script, new RedisKey[] { handle.Key }, new RedisValue[] { handle.Token });
        _logger.LogDebug("分布式锁释放: {Key}", handle.Key);
    }
}

public class LockHandle
{
    public string Key { get; }
    public string Token { get; }
    public string Resource { get; }

    public LockHandle(string key, string token, string resource)
    {
        Key = key;
        Token = token;
        Resource = resource;
    }
}