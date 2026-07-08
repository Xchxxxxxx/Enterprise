using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddEfCoreSerilog(this WebApplicationBuilder builder)
    {
        var loggerConfig = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId();

        if (!builder.Configuration.GetSection("Serilog:WriteTo").Exists())
        {
            loggerConfig.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: SystemConsoleTheme.Colored,
                restrictedToMinimumLevel: builder.Environment.IsDevelopment()
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information);
        }

        Log.Logger = loggerConfig.CreateLogger();
        builder.Host.UseSerilog();

        return builder;
    }
}