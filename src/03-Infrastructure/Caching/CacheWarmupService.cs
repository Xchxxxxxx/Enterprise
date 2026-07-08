using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Caching;

public class CacheWarmupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmupService> _logger;
    private readonly List<Func<IServiceProvider, Task>> _warmupTasks = new();

    public CacheWarmupService(IServiceProvider serviceProvider, ILogger<CacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void RegisterWarmup(Func<IServiceProvider, Task> warmupTask)
    {
        _warmupTasks.Add(warmupTask);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始缓存预热, 共 {Count} 项任务", _warmupTasks.Count);

        var tasks = _warmupTasks.Select(async (task, index) =>
        {
            try
            {
                await task(_serviceProvider);
                _logger.LogInformation("缓存预热任务 {Index} 完成", index + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "缓存预热任务 {Index} 失败", index + 1);
            }
        });

        await Task.WhenAll(tasks);
        _logger.LogInformation("缓存预热全部完成");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}