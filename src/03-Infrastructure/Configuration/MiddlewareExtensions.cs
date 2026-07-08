using EfCore.Enterprise.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace EfCore.Enterprise.Infrastructure.Configuration;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseEfCoreMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<PerformanceMiddleware>();
        app.UseMiddleware<RequestLogMiddleware>();
        app.UseMiddleware<RateLimitMiddleware>();
        app.UseMiddleware<IdempotencyMiddleware>();
        app.UseMiddleware<OpLogMiddleware>();

        return app;
    }

    public static IApplicationBuilder UseEfCoreGrayRelease(this IApplicationBuilder app)
    {
        app.UseMiddleware<GrayReleaseMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseEfCoreValidation(this IApplicationBuilder app)
    {
        app.UseMiddleware<ValidationFilterMiddleware>();
        return app;
    }
}