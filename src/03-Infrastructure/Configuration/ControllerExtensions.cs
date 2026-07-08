using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public static class ControllerExtensions
{
    public static IServiceCollection AddEfCoreControllers(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddHttpClient();
        services.AddHttpContextAccessor();

        return services;
    }
}