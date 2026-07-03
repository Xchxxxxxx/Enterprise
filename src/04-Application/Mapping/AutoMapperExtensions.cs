using System.Reflection;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Application.Mapping;

/// <summary>
/// AutoMapper扩展方法，提供自动扫描Profile注册功能
/// </summary>
public static class AutoMapperExtensions
{
    /// <summary>
    /// 自动扫描并注册所有程序集中的 AutoMapper Profile，零配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集，不传则扫描全部</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddAutoMapperAuto(this IServiceCollection services, params Assembly[] assemblies)
    {
        if (assemblies.Length == 0)
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .ToArray();
        }

        services.AddAutoMapper(assemblies);
        return services;
    }
}

/// <summary>
/// AutoMapper Profile基类，继承此类可免去覆写构造函数，直接实现 <see cref="Configure"/> 方法即可
/// </summary>
/// <example>
/// <code>
/// public class OrderProfile : BaseProfile
/// {
///     protected override void Configure()
///     {
///         CreateMap&lt;Order, OrderDto&gt;();
///         CreateMap&lt;CreateOrderDto, Order&gt;();
///     }
/// }
/// </code>
/// </example>
public abstract class BaseProfile : Profile
{
    /// <summary>
    /// 初始化Profile，自动调用 <see cref="Configure"/> 方法
    /// </summary>
    protected BaseProfile()
    {
        Configure();
    }

    /// <summary>
    /// 配置映射关系，子类在此方法中定义 CreateMap
    /// </summary>
    protected abstract void Configure();
}