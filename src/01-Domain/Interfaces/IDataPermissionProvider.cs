namespace EfCore.Enterprise.Domain.Interfaces;

/// <summary>
/// 数据权限提供者接口，用于获取当前用户的身份与权限信息
/// </summary>
/// <remarks>
/// 实现此接口以接入自定义的权限系统（如JWT、IdentityServer等）。
/// 框架的 <c>DataPermissionInterceptor</c> 会调用此接口实现行级数据隔离。
/// </remarks>
public interface IDataPermissionProvider
{
    /// <summary>
    /// 获取当前登录用户ID（未登录返回null）
    /// </summary>
    long? GetCurrentUserId();

    /// <summary>
    /// 获取当前用户拥有的角色ID列表，用于数据权限过滤
    /// </summary>
    IEnumerable<long> GetCurrentRoleIds();

    /// <summary>
    /// 获取当前用户拥有的权限标识列表（如 "order:read", "user:delete"）
    /// </summary>
    IEnumerable<string> GetCurrentPermissions();
}