
using EfCore.Enterprise.Domain.Attributes;
using EfCore.Enterprise.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;
using System.Reflection;

namespace EfCore.Enterprise.Infrastructure.Middleware;

public class OpLogMiddleware
{
    private readonly RequestDelegate _next;

    public OpLogMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, OpLogService opLogService)
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<OpLogAttribute>();

        if (attribute == null)
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            sw.Stop();
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            var detail = attribute.DetailTemplate;
            if (detail != null && endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>() is { } descriptor)
            {
                foreach (var param in descriptor.MethodInfo.GetParameters())
                {
                    detail = detail.Replace($"{{{param.Name}}}", context.Request.RouteValues[param.Name]?.ToString() ?? "");
                }
            }

            var isSuccess = context.Response.StatusCode >= 200 && context.Response.StatusCode < 300;
            var userId = context.Items["CurrentUserId"] as long?;
            var userName = context.Items["CurrentUserName"] as string;

            await opLogService.LogAsync(
                attribute.Module,
                attribute.Action,
                detail,
                userName,
                userId,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers["User-Agent"].ToString(),
                sw.ElapsedMilliseconds,
                isSuccess);
        }
        catch
        {
            sw.Stop();
            var userId = context.Items["CurrentUserId"] as long?;
            var userName = context.Items["CurrentUserName"] as string;

            await opLogService.LogAsync(
                attribute.Module,
                attribute.Action,
                attribute.DetailTemplate,
                userName,
                userId,
                context.Connection.RemoteIpAddress?.ToString(),
                context.Request.Headers["User-Agent"].ToString(),
                sw.ElapsedMilliseconds,
                false);

            throw;
        }
    }
}