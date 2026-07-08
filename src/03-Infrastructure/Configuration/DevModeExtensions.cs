using EfCore.Enterprise.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public class DevModeOptions
{
    public bool AutoMigrate { get; set; } = true;
    public bool AutoSeed { get; set; } = true;
}

public static class DevModeExtensions
{
    public static IApplicationBuilder UseDevMode(this IApplicationBuilder app, Action<DevModeOptions>? configure = null)
    {
        var options = new DevModeOptions();
        configure?.Invoke(options);

        using var scope = app.ApplicationServices.CreateScope();
        var devMode = scope.ServiceProvider.GetRequiredService<DevModeService>();

        if (options.AutoMigrate)
        {
            Task.Run(async () => await devMode.AutoMigrateAsync()).GetAwaiter().GetResult();
        }

        if (options.AutoSeed)
        {
            Task.Run(async () => await devMode.AutoSeedAsync()).GetAwaiter().GetResult();
        }

        return app;
    }
}