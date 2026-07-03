using EfCore.Enterprise.Infrastructure.Services;
using EfCore.Enterprise.Shared.Models;
using System.Text.Json;

namespace EfCore.Enterprise.Presentation.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitService rateLimitService)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var isBlacklisted = await rateLimitService.IsBlacklistedAsync(clientIp);
        if (isBlacklisted)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            var result = ApiResult.Fail("请求过于频繁，IP已被限制", 10010);
            await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            return;
        }

        var key = $"{clientIp}:{context.Request.Path}";
        var allowed = await rateLimitService.CheckAsync(key, 100, TimeSpan.FromMinutes(1));

        if (!allowed)
        {
            await rateLimitService.AddToBlacklistAsync(clientIp, TimeSpan.FromMinutes(5));
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            var result = ApiResult.Fail("请求频率超限，请稍后重试", 10010);
            await context.Response.WriteAsync(JsonSerializer.Serialize(result));
            return;
        }

        await _next(context);
    }
}
