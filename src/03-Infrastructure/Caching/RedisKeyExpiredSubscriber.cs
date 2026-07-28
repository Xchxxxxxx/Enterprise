using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EfCore.Enterprise.Infrastructure.Caching;

public class RedisKeyExpiredSubscriber : BackgroundService
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisKeyExpiredSubscriber> _logger;

    public RedisKeyExpiredSubscriber(
        IConnectionMultiplexer multiplexer,
        ILogger<RedisKeyExpiredSubscriber> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis Key过期事件订阅器已启动");

        var subscriber = _multiplexer.GetSubscriber();
        await subscriber.SubscribeAsync(RedisChannel.Literal("__keyevent@0__:expired"), 
            (channel, message) => OnKeyExpired(message));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private void OnKeyExpired(RedisValue message)
    {
        var key = message.ToString();
        
        try
        {
            _logger.LogInformation("检测到Key过期: {Key}", key);

            if (key.StartsWith("EfCoreEnterprise:"))
            {
                HandleCacheKeyExpired(key);
            }
            else if (key.StartsWith("DistributedLock:"))
            {
                HandleDistributedLockKeyExpired(key);
            }
            else if (key.StartsWith("RateLimit:"))
            {
                HandleRateLimitKeyExpired(key);
            }
            else
            {
                HandleCustomKeyExpired(key);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理过期Key时发生错误: {Key}", key);
        }
    }

    private void HandleCacheKeyExpired(string key)
    {
        var cacheKey = key.Replace("EfCoreEnterprise:", "");
        _logger.LogInformation("缓存Key已过期，可能需要刷新数据: {CacheKey}", cacheKey);
        
        // 可以在这里触发缓存预热或数据刷新逻辑
        // 例如：发布领域事件、调用服务等
    }

    private void HandleDistributedLockKeyExpired(string key)
    {
        var lockKey = key.Replace("DistributedLock:", "");
        _logger.LogWarning("分布式锁已过期，可能存在死锁风险: {LockKey}", lockKey);
        
        // 可以在这里触发锁释放后的清理逻辑
    }

    private void HandleRateLimitKeyExpired(string key)
    {
        var rateLimitKey = key.Replace("RateLimit:", "");
        _logger.LogDebug("限流计数器已重置: {RateLimitKey}", rateLimitKey);
    }

    private void HandleCustomKeyExpired(string key)
    {
        _logger.LogDebug("自定义Key已过期: {Key}", key);
        
        // 处理其他类型的过期事件
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止Redis Key过期事件订阅器...");
        await base.StopAsync(cancellationToken);
    }
}