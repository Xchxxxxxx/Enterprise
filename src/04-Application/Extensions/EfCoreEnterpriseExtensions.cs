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

        services.AddEfCoreControllers();
        services.AddEfCoreSwagger(configuration);
        services.AddEfCoreJwt(configuration);
        services.AddEfCoreOpenTelemetry(configuration);
        services.AddEfCoreCors();
        services.AddHealthChecks();

        services.AddInfrastructureServices(connectionString);
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

        services.AddEfCoreControllers();
        services.AddEfCoreSwagger(configuration);
        services.AddEfCoreJwt(configuration);
        services.AddEfCoreOpenTelemetry(configuration);
        services.AddEfCoreCors();
        services.AddHealthChecks();

        services.AddInfrastructureServices<TContext>(connectionString);
        services.AddApplicationServices();

        return services;
    }

    public static IServiceCollection AddEfCoreAutoInject(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddInjectables(assemblies);
        services.AddFluentValidationAuto(assemblies);
        services.AddMediatRAuto(assemblies);
        services.AddAutoMapperAuto(assemblies);

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