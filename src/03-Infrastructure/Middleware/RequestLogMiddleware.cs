using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Middleware;

public class RequestLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLogMiddleware> _logger;

    public RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;

        _logger.LogInformation(
            "请求开始: {RequestId} {Method} {Path}",
            requestId,
            context.Request.Method,
            context.Request.Path);

        await _next(context);

        sw.Stop();

        _logger.LogInformation(
            "请求结束: {RequestId} {StatusCode} {ElapsedMs}ms",
            requestId,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds);
    }
}