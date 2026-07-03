using EfCore.Enterprise.Infrastructure.Services;
using EfCore.Enterprise.Shared.Models;
using System.Text.Json;

namespace EfCore.Enterprise.Presentation.Middleware;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        var idempotencyKey = context.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        if (!string.IsNullOrEmpty(idempotencyKey))
        {
            var isDuplicate = await idempotencyService.IsDuplicateAsync(idempotencyKey);

            if (isDuplicate)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                var result = ApiResult.Fail("请求已处理，请勿重复提交", 10009);
                await context.Response.WriteAsync(JsonSerializer.Serialize(result));
                return;
            }

            context.Response.OnCompleted(async () =>
            {
                if (context.Response.StatusCode < 400)
                {
                    await idempotencyService.MarkProcessedAsync(idempotencyKey);
                }
            });
        }

        await _next(context);
    }
}
