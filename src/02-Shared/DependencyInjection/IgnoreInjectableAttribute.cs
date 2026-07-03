namespace EfCore.Enterprise.Shared.DependencyInjection;

/// <summary>
/// 标记此特性可禁止自动DI注册（[Injectable]和约定注册均会跳过）
/// </summary>
/// <remarks>
/// 用于需要手动注册的服务（如构造函数包含非DI参数 string/int/Dictionary 等配置值）
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class IgnoreInjectableAttribute : Attribute
{
}