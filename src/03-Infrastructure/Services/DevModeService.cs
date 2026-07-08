using System.Collections;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IDevModeService
{
    Task AutoMigrateAsync();
    Task AutoSeedAsync();
}

public interface IDataSeed
{
    Type EntityType { get; }
    IEnumerable<object> GetSeedData();
}

public interface IDataSeed<TEntity> : IDataSeed where TEntity : class
{
    IEnumerable<TEntity> GetSeeds();

    Type IDataSeed.EntityType => typeof(TEntity);
    IEnumerable<object> IDataSeed.GetSeedData() => GetSeeds().Cast<object>();
}

[Injectable(ServiceLifetime.Singleton)]
public class DevModeService : IDevModeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DevModeService> _logger;

    public DevModeService(IServiceProvider serviceProvider, ILogger<DevModeService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task AutoMigrateAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            _logger.LogInformation("开发模式：检查数据库迁移...");
            await dbContext.Database.MigrateAsync();
            _logger.LogInformation("开发模式：数据库迁移完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "开发模式：数据库迁移失败，尝试 EnsureCreated");
            await dbContext.Database.EnsureCreatedAsync();
        }
    }

    public async Task AutoSeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var seedTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface
                && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataSeed<>)))
            .ToList();

        foreach (var seedType in seedTypes)
        {
            try
            {
                var seedInstance = (IDataSeed)ActivatorUtilities.CreateInstance(scope.ServiceProvider, seedType);
                var seeds = seedInstance.GetSeedData().ToList();
                if (!seeds.Any()) continue;

                var entityType = seedInstance.EntityType;

                var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!.MakeGenericMethod(entityType);
                var dbSet = setMethod.Invoke(dbContext, null)!;
                var anyMethod = typeof(Queryable).GetMethods()
                    .First(m => m.Name == "Any" && m.GetParameters().Length == 1)
                    .MakeGenericMethod(entityType);
                var hasData = (bool)anyMethod.Invoke(null, new[] { dbSet })!;

                if (!hasData)
                {
                    dbContext.AddRange(seeds);
                    _logger.LogInformation("种子数据初始化完成 - {EntityType}", entityType.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "种子数据初始化失败 - {SeedType}", seedType.Name);
            }
        }

        await dbContext.SaveChangesAsync();
        _logger.LogInformation("种子数据全部初始化完成");
    }
}