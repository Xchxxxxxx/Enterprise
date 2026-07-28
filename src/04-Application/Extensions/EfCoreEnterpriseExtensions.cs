using EfCore.Enterprise.Application.Mapping;
using EfCore.Enterprise.Infrastructure.Configuration;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;

namespace EfCore.Enterprise.Application.Extensions;

public static class EfCoreEnterpriseExtensions
{
    public static IServiceCollection AddEfCoreEnterprise(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("未找到连接字符串 \"DefaultConnection\"");

        var redisConfig = ReadRedisConfiguration(configuration);

        services.AddEfCoreControllers();
        services.AddEfCoreSwagger(configuration);
        services.AddEfCoreJwt(configuration);
        services.AddEfCoreOpenTelemetry(configuration);
        services.AddEfCoreCors();
        services.AddHealthChecks();

        services.AddInfrastructureServices(
            connectionString: connectionString,
            enableRedis: redisConfig.EnableRedis,
            redisConnection: redisConfig.RedisConnection,
            enableHangfire: configuration.GetValue<bool>("EfCoreEnterprise:EnableHangfire"),
            modelCachePath: configuration.GetValue<string>("EfCoreEnterprise:ModelCachePath"),
            complianceLogPath: configuration.GetValue<string>("EfCoreEnterprise:ComplianceLogPath"));

        services.AddApplicationServices();

        return services;
    }

    public static IServiceCollection AddEfCoreEnterprise<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : AppDbContext
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("未找到连接字符串 \"DefaultConnection\"");

        var redisConfig = ReadRedisConfiguration(configuration);

        services.AddEfCoreControllers();
        services.AddEfCoreSwagger(configuration);
        services.AddEfCoreJwt(configuration);
        services.AddEfCoreOpenTelemetry(configuration);
        services.AddEfCoreCors();
        services.AddHealthChecks();

        services.AddInfrastructureServices<TContext>(
            connectionString: connectionString,
            enableRedis: redisConfig.EnableRedis,
            redisConnection: redisConfig.RedisConnection,
            enableHangfire: configuration.GetValue<bool>("EfCoreEnterprise:EnableHangfire"),
            modelCachePath: configuration.GetValue<string>("EfCoreEnterprise:ModelCachePath"),
            complianceLogPath: configuration.GetValue<string>("EfCoreEnterprise:ComplianceLogPath"));

        services.AddApplicationServices();

        return services;
    }

    private static (bool EnableRedis, string? RedisConnection) ReadRedisConfiguration(IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        
        var explicitEnable = configuration.GetValue<bool?>("EfCoreEnterprise:EnableRedis");
        bool enableRedis;
        
        if (explicitEnable.HasValue)
        {
            enableRedis = explicitEnable.Value && !string.IsNullOrEmpty(redisConnection);
        }
        else
        {
            enableRedis = !string.IsNullOrEmpty(redisConnection);
        }

        return (enableRedis, redisConnection);
    }

    public static IServiceCollection AddEfCoreAutoInject(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var frameworkAssembly = typeof(EfCore.Enterprise.Infrastructure.Services.CurrentUserService).Assembly;
        var allAssemblies = new[] { frameworkAssembly }.Concat(assemblies).ToArray();

        services.AddInjectables(allAssemblies);
        services.AddFluentValidationAuto(allAssemblies);
        services.AddMediatRAuto(allAssemblies);
        services.AddAutoMapperAuto(allAssemblies);

        return services;
    }

    public static IApplicationBuilder UseEfCorePipeline(this IApplicationBuilder app, bool isDevelopment)
    {
        if (isDevelopment)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseCors("AllowAll");
        app.UseSerilogRequestLogging();
        app.UseEfCoreMiddleware();

        if (!isDevelopment)
        {
            app.UseHttpsRedirection();
        }
       
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseOpenTelemetryPrometheusScrapingEndpoint();

        return app;
    }
}