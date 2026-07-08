namespace EfCore.Enterprise.Infrastructure.Configuration;

public static class CorsExtensions
{
    public static IServiceCollection AddEfCoreCors(this IServiceCollection services, string policyName = "AllowAll")
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .SetIsOriginAllowed(_ => true);
            });
        });

        return services;
    }
}