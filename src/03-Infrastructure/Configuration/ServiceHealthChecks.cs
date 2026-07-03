using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using EfCore.Enterprise.Infrastructure.Data;
using StackExchange.Redis;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(AppDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(ct);
            if (canConnect)
            {
                return HealthCheckResult.Healthy("数据库连接正常");
            }
            return HealthCheckResult.Unhealthy("数据库连接失败");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库健康检查失败");
            return HealthCheckResult.Unhealthy("数据库健康检查异常", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisHealthCheck> _logger;

    public RedisHealthCheck(IConnectionMultiplexer? redis, ILogger<RedisHealthCheck> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        if (_redis == null)
        {
            return HealthCheckResult.Degraded("Redis未配置");
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis连接正常");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis健康检查失败");
            return HealthCheckResult.Unhealthy("Redis连接异常", ex);
        }
    }
}

public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        var memory = GC.GetTotalMemory(false);
        var memoryMB = memory / (1024 * 1024);

        if (memoryMB > 2048)
        {
            return Task.FromResult(HealthCheckResult.Degraded($"内存使用过高: {memoryMB}MB"));
        }

        return Task.FromResult(HealthCheckResult.Healthy($"内存正常: {memoryMB}MB"));
    }
}