using EfCore.Enterprise.Shared.DependencyInjection;
using FluentValidation;
using FluentValidation.Results;
using EfCore.Enterprise.Shared.Models;
using EfCore.Enterprise.Shared.Enums;

namespace EfCore.Enterprise.Application.Validation;

/// <summary>
/// 统一验证服务接口，提供同步和异步验证能力
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// 异步验证对象
    /// </summary>
    /// <typeparam name="T">验证对象类型</typeparam>
    /// <param name="instance">要验证的对象实例</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>验证结果</returns>
    Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken ct = default);

    /// <summary>
    /// 同步验证对象
    /// </summary>
    ValidationResult Validate<T>(T instance);
}

/// <summary>
/// 统一验证服务，自动从DI容器解析对应的 FluentValidation 验证器
/// </summary>
/// <remarks>
/// 使用 [Injectable] 属性标注，自动注册为 Scoped 生命周期。
/// 如果未找到对应的验证器，返回空的验证结果（视为通过）。
/// </remarks>
[Injectable(ServiceLifetime.Scoped)]
public class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 初始化验证服务
    /// </summary>
    public ValidationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 异步验证：从DI容器查找 IValidator&lt;T&gt; 并执行异步验证
    /// </summary>
    public async Task<ValidationResult> ValidateAsync<T>(T instance, CancellationToken ct = default)
    {
        var validator = (IValidator<T>?)_serviceProvider.GetService(typeof(IValidator<T>));
        if (validator == null) return new ValidationResult();

        return await validator.ValidateAsync(instance, ct);
    }

    /// <summary>
    /// 同步验证：从DI容器查找 IValidator&lt;T&gt; 并执行同步验证
    /// </summary>
    public ValidationResult Validate<T>(T instance)
    {
        var validator = (IValidator<T>?)_serviceProvider.GetService(typeof(IValidator<T>));
        if (validator == null) return new ValidationResult();

        return validator.Validate(instance);
    }
}

/// <summary>
/// 验证结果扩展方法，将 FluentValidation 结果转换为统一的 ApiResult
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// 将验证结果转为 ApiResult，验证失败时收集所有错误信息
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="result">验证结果</param>
    /// <param name="data">验证通过时返回的数据</param>
    /// <returns>ApiResult</returns>
    public static ApiResult<T> ToApiResult<T>(this ValidationResult result, T? data = default)
    {
        if (result.IsValid) return ApiResult<T>.Success(data!);

        var errors = result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToList();
        return ApiResult<T>.Fail(ErrorCode.ValidationError, string.Join("; ", errors));
    }
}