using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Infrastructure.Caching;
using EfCore.Enterprise.Infrastructure.Services;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using EfCore.Enterprise.Infrastructure.Data.Optimization;
using EfCore.Enterprise.Infrastructure.Data.ReadWriteSplitting;
using EfCore.Enterprise.Infrastructure.Configuration;
using EfCore.Enterprise.Shared.DependencyInjection;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public class CoreModule : IModule
{
    private readonly string _connectionString;
    private readonly bool _enableRedis;
    private readonly string? _redisConnection;
    private readonly bool _enableHangfire;
    private readonly string? _modelCachePath;
    private readonly string? _complianceLogPath;

    public CoreModule(
        string connectionString,
        bool enableRedis = false,
        string? redisConnection = null,
        bool enableHangfire = false,
        string? modelCachePath = null,
        string? complianceLogPath = null)
    {
        _connectionString = connectionString;
        _enableRedis = enableRedis;
        _redisConnection = redisConnection;
        _enableHangfire = enableHangfire;
        _modelCachePath = modelCachePath;
        _complianceLogPath = complianceLogPath;
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        ConfigureDbContext(services);
        services.AddScoped(typeof(IRepository<>), typeof(SuperRepository<>));
        services.AddHostedService<CacheWarmupService>();
        ConfigureSpecialServices(services);
        AddHealthChecks(services);
        ConfigureCache(services);
        ConfigureHangfire(services);
    }

    private void ConfigureDbContext(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString), mysqlOptions =>
            {
                mysqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                mysqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            });

            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            options.AddInterceptors(
                sp.GetRequiredService<SqlLogInterceptor>(),
                sp.GetRequiredService<NPlusOneInterceptor>(),
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>(),
                sp.GetRequiredService<TenantInterceptor>(),
                sp.GetRequiredService<ComplianceInterceptor>());
        });
    }

    private void ConfigureSpecialServices(IServiceCollection services)
    {
        services.AddSingleton<BloomFilter>(sp =>
            new BloomFilter(1000000, 0.01));

        if (!string.IsNullOrEmpty(_complianceLogPath))
        {
            services.AddSingleton<IComplianceAuditService>(sp =>
                new ComplianceAuditService(
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ComplianceAuditService>>(),
                    _complianceLogPath));
        }

        if (!string.IsNullOrEmpty(_modelCachePath))
        {
            services.AddSingleton(sp =>
                new ModelCacheManager(_modelCachePath,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ModelCacheManager>>()));
        }
    }

    private void AddHealthChecks(IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database")
            .AddCheck<MemoryHealthCheck>("memory");
    }

    private void ConfigureCache(IServiceCollection services)
    {
        if (_enableRedis && !string.IsNullOrEmpty(_redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = _redisConnection;
                options.InstanceName = "EfCoreEnterprise:";
            });
            services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
    }

    private void ConfigureHangfire(IServiceCollection services)
    {
        if (_enableHangfire)
        {
            services.AddHangfire(config =>
                config.UseMemoryStorage());
            services.AddHangfireServer();
        }
    }
}