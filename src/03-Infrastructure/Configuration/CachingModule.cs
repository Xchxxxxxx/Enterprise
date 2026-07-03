using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Infrastructure.Configuration;

/// <summary>
/// 缓存模块，统一管理缓存相关服务（HotDataCache、BloomFilter等）的注�?
/// </summary>
/// <remarks>
/// 缓存服务本身已通过 [Injectable] 属性自动注册，此模块可用于额外的缓存策略配置�?
/// </remarks>
public class CachingModule : IModule
{
    /// <summary>
    /// 配置缓存相关服务
    /// </summary>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }
}
