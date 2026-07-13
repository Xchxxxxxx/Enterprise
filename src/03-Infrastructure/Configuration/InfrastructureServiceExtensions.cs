using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Infrastructure.Services;
using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Infrastructure.Caching;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using EfCore.Enterprise.Infrastructure.Data.Optimization;
using EfCore.Enterprise.Infrastructure.Data.ReadWriteSplitting;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string connectionString,
        bool enableRedis = false,
        string? redisConnection = null,
        bool enableHangfire = false,
        string? modelCachePath = null,
        string? complianceLogPath = null)
    {
        services.AddCoreServices(connectionString, enableRedis, redisConnection,
            enableHangfire, modelCachePath, complianceLogPath);

        services.AddDbContext<AppDbContext>(ConfigureDbContext(typeof(AppDbContext), connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices<TContext>(
        this IServiceCollection services,
        string connectionString,
        bool enableRedis = false,
        string? redisConnection = null,
        bool enableHangfire = false,
        string? modelCachePath = null,
        string? complianceLogPath = null)
        where TContext : AppDbContext
    {
        services.AddCoreServices(connectionString, enableRedis, redisConnection,
            enableHangfire, modelCachePath, complianceLogPath);

        services.AddDbContext<TContext>(ConfigureDbContext(typeof(TContext), connectionString));

        services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<TContext>());

        services.AddScoped<IUnitOfWork>(sp => new UnitOfWork(sp.GetRequiredService<TContext>()));
        services.AddScoped<IUnitOfWork<TContext>>(sp => new UnitOfWork<TContext>(sp.GetRequiredService<TContext>()));

        return services;
    }

    private static Action<IServiceProvider, DbContextOptionsBuilder> ConfigureDbContext(
        Type contextType, string connectionString)
    {
        return (sp, options) =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                sqlOptions.MigrationsAssembly(contextType.Assembly.FullName);
            });

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            options.AddInterceptors(
                sp.GetRequiredService<SqlLogInterceptor>(),
                sp.GetRequiredService<NPlusOneInterceptor>(),
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<TenantInterceptor>(),
                sp.GetRequiredService<ComplianceInterceptor>());
        };
    }

    private static IServiceCollection AddCoreServices(
        this IServiceCollection services,
        string connectionString,
        bool enableRedis,
        string? redisConnection,
        bool enableHangfire,
        string? modelCachePath,
        string? complianceLogPath)
    {
        services.AddInterceptors();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(ISuperRepository<>), typeof(SuperRepository<>));
        services.AddScoped(typeof(IRepository<>), typeof(SuperRepository<>));

        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddSingleton<IResilienceService, ResilienceService>();

        services.AddSingleton<QueryPrecompilationCache>();
        services.AddSingleton<IIdGeneratorService, SnowflakeIdGenerator>();
        services.AddSingleton<IRateLimitService, RateLimitService>();

        services.AddScoped<IIdempotencyService, IdempotencyService>();

        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IDistributedLockService, DistributedLockService>();

        services.AddScoped<IBackgroundJobService, HangfireJobService>();

        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.AddSingleton<IAlertService, AlertService>();
        services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();
        services.AddSingleton<IPluginManager, PluginManager>();

        services.AddSingleton<BloomFilter>(sp =>
            new BloomFilter(1000000, 0.01));
        services.AddSingleton<IHotDataCacheService, HotDataCacheService>();
        services.AddSingleton<IObjectPoolService, ObjectPoolService>();
        services.AddSingleton<IPerformanceMetrics, PerformanceMetrics>();
        services.AddSingleton<IDeadLetterQueueService, DeadLetterQueueService>();
        services.AddSingleton<IDomainEventBus, MediatorDomainEventBus>();
        services.AddHostedService<CacheWarmupService>();

        if (!string.IsNullOrEmpty(complianceLogPath))
        {
            services.AddSingleton<IComplianceAuditService>(sp =>
                new ComplianceAuditService(
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ComplianceAuditService>>(),
                    complianceLogPath));
        }

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<MemoryHealthCheck>("memory");

        if (enableRedis && !string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "EfCoreEnterprise:";
            });
            services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        if (enableHangfire)
        {
            services.AddHangfire(config =>
                config.UseMemoryStorage());
            services.AddHangfireServer();
        }

        if (!string.IsNullOrEmpty(modelCachePath))
        {
            services.AddSingleton(sp =>
                new ModelCacheManager(modelCachePath,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ModelCacheManager>>()));
        }

        // 自动注册 Infrastructure 程序集中标记了 [Injectable] 的服务（如 OpLogService）
        services.AddInjectables(typeof(InfrastructureServiceExtensions).Assembly);

        return services;
    }

    private static IServiceCollection AddInterceptors(this IServiceCollection services)
    {
        services.AddSingleton<AuditInterceptor>();
        services.AddSingleton<SoftDeleteInterceptor>();
        services.AddSingleton<SqlLogInterceptor>();
        services.AddScoped<NPlusOneInterceptor>();
        services.AddSingleton<FieldPermissionInterceptor>();
        services.AddSingleton<TenantInterceptor>();
        services.AddSingleton<ComplianceInterceptor>();
        services.AddSingleton<AuditTrailInterceptor>();
        services.AddSingleton<OptimisticLockInterceptor>();
        services.AddSingleton<DataPermissionInterceptor>();

        return services;
    }
}