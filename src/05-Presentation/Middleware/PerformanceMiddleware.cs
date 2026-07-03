using System.Diagnostics;
using EfCore.Enterprise.Infrastructure.Services;

namespace EfCore.Enterprise.Presentation.Middleware;

public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;

    public PerformanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPerformanceMonitor monitor)
    {
        var sw = Stopwatch.StartNew();

        await _next(context);

        sw.Stop();
        monitor.TrackRequest(context.Request.Path, sw.ElapsedMilliseconds, context.Response.StatusCode);
    }
}
