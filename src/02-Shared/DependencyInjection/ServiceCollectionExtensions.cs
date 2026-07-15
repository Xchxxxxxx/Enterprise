using System.Reflection;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;

namespace EfCore.Enterprise.Shared.DependencyInjection;

/// <summary>
/// 服务集合扩展方法，提供属性注入、约定注册、模块化配置等自动化DI注册能力
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly string[] SystemAssemblyPrefixes = new[]
    {
        "System.", "Microsoft.", "FluentValidation", "MediatR", "AutoMapper",
        "Serilog", "Hangfire", "Prometheus", "OpenTelemetry", "Swashbuckle",
        "Humanizer", "Newtonsoft", "Polly", "Castle.", "DynamicProxy",
        "xunit", "NUnit", "Moq", "Bogus", "BenchmarkDotNet", "coverlet",
        "MessagePack", "protobuf", "Grpc", "StackExchange", "Pipelines",
        "Azure.", "AWS.", "Google.", "Dapper", "EntityFramework", "MySql",
        "Npgsql", "Oracle", "SQLite", "MongoDB", "RabbitMQ", "Kafka",
        "Confluent", "AWSSDK", "AWSSDK.", "IdentityModel", "YamlDotNet",
        "CsvHelper", "Excel", "NPOI", "ClosedXML", "EPPlus", "iTextSharp",
        "PdfSharp", "Magick", "SkiaSharp", "SixLabors", "ZXing", "QRCoder",
        "BouncyCastle", "Portable.BouncyCastle", "HtmlAgilityPack", "AngleSharp",
        "Markdig", "NUglify", "WebMarkupMin", "Scrutor", "Scrutor.",
        "AspNetCore", "Swashbuckle", "NodaTime", "TimeZoneConverter",
        "RestSharp", "Flurl", "Refit", "Polly", "NetEscapades", "Scrutor"
    };

    private static Assembly[] GetProjectAssemblies(Assembly[]? specifiedAssemblies = null)
    {
        if (specifiedAssemblies != null && specifiedAssemblies.Length > 0)
            return specifiedAssemblies;

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Where(a => !SystemAssemblyPrefixes.Any(prefix =>
                a.GetName().Name?.StartsWith(prefix) == true))
            .ToArray();
    }
    /// <summary>
    /// 扫描程序集中所有标记了 <see cref="InjectableAttribute"/> 的类并自动注册到DI容器
    /// </summary>
    /// <remarks>
    /// 支持泛型类型定义。不传程序集时自动扫描所有已加载的非动态程序集。
    /// 注册时会根据 <see cref="InjectableAttribute.ExposeAs"/> 或命名约定（I{ClassName}）确定服务类型。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集，不传则扫描全部</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddInjectables(this IServiceCollection services, params Assembly[] assemblies)
    {
        assemblies = GetProjectAssemblies(assemblies);

        foreach (var assembly in assemblies)
        {
            RegisterInjectablesFromAssembly(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// 从指定程序集集合中扫描并注册可注入服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集集合</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddInjectablesFromAssemblies(this IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            RegisterInjectablesFromAssembly(services, assembly);
        }
        return services;
    }

    /// <summary>
    /// 扫描单个程序集中所有带 [Injectable] 特性的类并注册
    /// </summary>
    private static void RegisterInjectablesFromAssembly(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in types)
        {
            if (type.GetCustomAttribute<IgnoreInjectableAttribute>() != null) continue;

            var attribute = type.GetCustomAttribute<InjectableAttribute>();
            if (attribute == null) continue;

            if (type.IsGenericTypeDefinition)
            {
                var rawServiceType = attribute.ExposeAs ?? GetImplementedInterface(type) ?? type;
                var serviceType = rawServiceType.IsGenericType && !rawServiceType.IsGenericTypeDefinition
                    ? rawServiceType.GetGenericTypeDefinition()
                    : rawServiceType;
                services.Add(new ServiceDescriptor(serviceType, type, attribute.Lifetime));
            }
            else
            {
                var serviceType = attribute.ExposeAs ?? GetImplementedInterface(type) ?? type;
                services.Add(new ServiceDescriptor(serviceType, type, attribute.Lifetime));
            }
        }
    }

    /// <summary>
    /// 按命名约定自动注册服务：扫描所有 I{ClassName} 接口与其实现类 {ClassName}
    /// </summary>
    /// <remarks>
    /// 生命周期由类名后缀决定：
    /// Service/Repository/Provider → Scoped,
    /// Controller → Transient,
    /// Interceptor/Manager/Cache/Factory → Singleton
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddByConvention(this IServiceCollection services, params Assembly[] assemblies)
    {
        assemblies = GetProjectAssemblies(assemblies);

        foreach (var assembly in assemblies)
        {
            RegisterByConvention(services, assembly);
        }

        return services;
    }

    /// <summary>
    /// 按约定注册单个程序集的服务
    /// </summary>
    private static void RegisterByConvention(IServiceCollection services, Assembly assembly)
    {
        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .ToList();

        foreach (var type in types)
        {
            if (type.GetCustomAttribute<IgnoreInjectableAttribute>() != null) continue;
            if (type.GetCustomAttribute<InjectableAttribute>() != null) continue;

            var interfaces = type.GetInterfaces()
                .Where(i => i.Name == $"I{type.Name}")
                .ToList();

            if (interfaces.Any())
            {
                var serviceType = interfaces.First();
                var lifetime = GetLifetimeByConvention(type.Name);
                services.Add(new ServiceDescriptor(serviceType, type, lifetime));
            }
        }
    }

    /// <summary>
    /// 根据类名后缀推断服务的生命周期
    /// </summary>
    private static ServiceLifetime GetLifetimeByConvention(string typeName)
    {
        if (typeName.EndsWith("Service")) return ServiceLifetime.Scoped;
        if (typeName.EndsWith("Repository")) return ServiceLifetime.Scoped;
        if (typeName.EndsWith("Provider")) return ServiceLifetime.Scoped;
        if (typeName.EndsWith("Controller")) return ServiceLifetime.Transient;
        if (typeName.EndsWith("Interceptor")) return ServiceLifetime.Singleton;
        if (typeName.EndsWith("Manager")) return ServiceLifetime.Singleton;
        if (typeName.EndsWith("Cache")) return ServiceLifetime.Singleton;
        if (typeName.EndsWith("Factory")) return ServiceLifetime.Singleton;
        return ServiceLifetime.Scoped;
    }

    /// <summary>
    /// 自动发现并执行所有 <see cref="IModule"/> 实现的服务配置
    /// </summary>
    /// <remarks>
    /// 仅实例化具有无参构造函数的模块。有参构造函数的模块需手动调用 ConfigureServices。
    /// </remarks>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddModules(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
    {
        assemblies = GetProjectAssemblies(assemblies);

        foreach (var assembly in assemblies)
        {
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();

            foreach (var moduleType in moduleTypes)
            {
                if (moduleType.GetConstructor(Type.EmptyTypes) == null) continue;

                var module = (IModule)Activator.CreateInstance(moduleType)!;
                module.ConfigureServices(services, configuration);
            }
        }

        return services;
    }

    /// <summary>
    /// 自动扫描并注册 FluentValidation 验证器，零配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddFluentValidationAuto(this IServiceCollection services, params Assembly[] assemblies)
    {
        assemblies = GetProjectAssemblies(assemblies);

        services.AddValidatorsFromAssemblies(assemblies);
        return services;
    }

    /// <summary>
    /// 自动扫描并注册 MediatR 的 Handler/Behavior 等，零配置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合（链式调用）</returns>
    public static IServiceCollection AddMediatRAuto(this IServiceCollection services, params Assembly[] assemblies)
    {
        assemblies = GetProjectAssemblies(assemblies);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.Lifetime = ServiceLifetime.Scoped;
        });

        return services;
    }

    /// <summary>
    /// 获取类实现的第一个匹配命名约定 I{ClassName} 的接口
    /// </summary>
    private static Type? GetImplementedInterface(Type type)
    {
        var interfaces = type.GetInterfaces()
            .Where(i => i.Name == $"I{type.Name}")
            .ToList();

        return interfaces.FirstOrDefault();
    }
}