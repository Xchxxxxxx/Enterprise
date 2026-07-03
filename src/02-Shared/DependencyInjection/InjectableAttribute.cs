using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Shared.DependencyInjection;

/// <summary>
/// 标记一个类为可注入服务，框架将自动扫描并注册到DI容器中
/// </summary>
/// <remarks>
/// 使用方式：
/// <code>
/// [Injectable(ServiceLifetime.Scoped)]
/// public class UserService : IUserService { }
/// </code>
/// 也可以指定暴露的接口类型：
/// <code>
/// [Injectable(ServiceLifetime.Singleton, ExposeAs = typeof(IMyService))]
/// public class MyServiceImpl : IMyService, IOtherService { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class InjectableAttribute : Attribute
{
    /// <summary>
    /// 服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// 指定暴露给DI的接口类型，不指定则自动匹配 I{ClassName} 接口
    /// </summary>
    public Type? ExposeAs { get; set; }

    /// <summary>
    /// 初始化可注入服务标记
    /// </summary>
    /// <param name="lifetime">服务生命周期，默认Scoped</param>
    public InjectableAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        Lifetime = lifetime;
    }
}