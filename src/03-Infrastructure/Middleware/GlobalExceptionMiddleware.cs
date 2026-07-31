using System.Net;
using System.Text.Json;
using EfCore.Enterprise.Shared.Exceptions;
using EfCore.Enterprise.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            var requestInfo = GetRequestInfo(context);
            _logger.LogWarning(ex, "业务异常 | {RequestInfo} | Code: {Code} | Message: {Message}",
                requestInfo, ex.Code, ex.Message);

            await HandleBusinessExceptionAsync(context, ex);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "请求被取消 | {RequestInfo}", GetRequestInfo(context));
            await HandleExceptionAsync(context, 499, "请求已取消");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权访问 | {RequestInfo}", GetRequestInfo(context));
            await HandleExceptionAsync(context, 401, "未授权访问");
        }
        catch (Exception ex)
        {
            var requestInfo = GetRequestInfo(context);
            _logger.LogError(ex, "系统异常 | {RequestInfo} | ExceptionType: {ExceptionType} | Message: {Message}",
                requestInfo, ex.GetType().FullName, ex.Message);

            await HandleSystemExceptionAsync(context, ex);
        }
    }

    private static string GetRequestInfo(HttpContext context)
    {
        var request = context.Request;
        return $"{request.Method} {request.Path}{request.QueryString} | IP: {GetClientIpAddress(context)}";
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private async Task HandleBusinessExceptionAsync(HttpContext context, AppException ex)
    {
        var response = new
        {
            Success = false,
            Code = ex.Code,
            Message = ex.Message,
            Data = (object?)null,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            TraceId = context.TraceIdentifier
        };

        await WriteResponseAsync(context, response);
    }

    private async Task HandleSystemExceptionAsync(HttpContext context, Exception ex)
    {
        object response;

        
            response = new
            {
                Success = false,
                Code = 9999,
                Message = ex.Message,
                ExceptionType = ex.GetType().FullName,
                StackTrace = ex.StackTrace?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Take(10)
                    .ToArray(),
                InnerException = ex.InnerException?.Message,
                RequestDetails = new
                {
                    Method = context.Request.Method,
                    Path = $"{context.Request.Path}{context.Request.QueryString}",
                    QueryParams = context.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()),
                    ClientIp = GetClientIpAddress(context),
                    Headers = context.Request.Headers
                        .Where(h => h.Key.StartsWith("X-") || h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(h => h.Key, h => h.Value.ToString())
                },
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                TraceId = context.TraceIdentifier
            };
        
        

        await WriteResponseAsync(context, response);
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        int code,
        string message)
    {
        var response = new
        {
            Success = false,
            Code = code,
            Message = message,
            Data = (object?)null,
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            TraceId = context.TraceIdentifier
        };

        await WriteResponseAsync(context, response);
    }

    private static async Task WriteResponseAsync(HttpContext context, object response)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = (int)HttpStatusCode.OK;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);
    }
}