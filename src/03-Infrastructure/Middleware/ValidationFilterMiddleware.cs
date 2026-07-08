using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EfCore.Enterprise.Infrastructure.Middleware;

public class ValidationFilterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationFilterMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilterMiddleware(RequestDelegate next, ILogger<ValidationFilterMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase))
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (!string.IsNullOrEmpty(body))
            {
                var endpoint = context.GetEndpoint();
                var validatorType = endpoint?.Metadata.GetMetadata<ValidatorMetadata>();

                if (validatorType != null)
                {
                    var validator = _serviceProvider.GetService(validatorType.ValidatorType) as IValidator;
                    if (validator != null)
                    {
                        try
                        {
                            var dto = JsonSerializer.Deserialize(body, validatorType.DtoType,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            if (dto != null)
                            {
                                var validationContext = new ValidationContext<object>(dto);
                                var result = await validator.ValidateAsync(validationContext);

                                if (!result.IsValid)
                                {
                                    var errors = result.Errors.Select(e => new
                                    {
                                        Field = e.PropertyName,
                                        Message = e.ErrorMessage
                                    });

                                    _logger.LogWarning("校验失败: {Path}, 错误: {Errors}",
                                        context.Request.Path, JsonSerializer.Serialize(errors));

                                    context.Response.StatusCode = 400;
                                    context.Response.ContentType = "application/json";
                                    var response = JsonSerializer.Serialize(new
                                    {
                                        Code = 400,
                                        Message = "参数校验失败",
                                        Errors = errors
                                    });
                                    await context.Response.WriteAsync(response);
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "校验中间件异常");
                        }
                    }
                }
            }
        }

        await _next(context);
    }
}

public class ValidatorMetadata
{
    public Type ValidatorType { get; }
    public Type DtoType { get; }

    public ValidatorMetadata(Type validatorType, Type dtoType)
    {
        ValidatorType = validatorType;
        DtoType = dtoType;
    }
}