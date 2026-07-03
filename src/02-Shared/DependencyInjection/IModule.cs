using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Shared.DependencyInjection;

/// <summary>
/// 模块化服务注册接口，实现此接口可将服务配置集中管理
/// </summary>
/// <remarks>
/// 框架通过 <see cref="ServiceCollectionExtensions.AddModules"/> 自动发现并调用所有模块的配置方法。
/// 无参数构造函数的模块会被自动实例化，有参数构造函数的模块需手动注册。
/// </remarks>
public interface IModule
{
    /// <summary>
    /// 配置模块内的服务注册
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">应用配置</param>
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}