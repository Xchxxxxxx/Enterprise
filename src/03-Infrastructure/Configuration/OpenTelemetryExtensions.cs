using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddEfCoreOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "EfCore.Enterprise";
        var serviceVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName: serviceName, serviceVersion: serviceVersion);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                      .AddHttpClientInstrumentation()
                      .AddRuntimeInstrumentation()
                      .AddProcessInstrumentation()
                      .AddMeter("EfCore.Enterprise")
                      .AddPrometheusExporter();
            })
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();

                var otelEndpoint = configuration["OpenTelemetry:Endpoint"];
                if (!string.IsNullOrEmpty(otelEndpoint))
                {
                    tracing.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(otelEndpoint);
                    });
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
            });

        return services;
    }
}