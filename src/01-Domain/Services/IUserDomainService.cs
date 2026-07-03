using EfCore.Enterprise.Domain.Entities.Identity;
using EfCore.Enterprise.Domain.Entities.Permission;

namespace EfCore.Enterprise.Domain.Services;

public interface IUserDomainService
{
    // 密码规则
    bool IsPasswordStrongEnough(string password);
    bool IsLockoutPeriodExpired(DateTimeOffset? lockoutEnd);
    bool ShouldLockAccount(int currentFailedCount);
    int GetLockoutMinutes(int failedCount);
    (string hash, string salt) HashPassword(string password);
    bool VerifyPassword(string password, string hash, string salt);

    // 跨聚合业务逻辑
    Task LoginAsync(SysUser user, string password, string? ip, string? userAgent);
    Task RecordFailedLoginAsync(string username, string? ip, string? userAgent, string reason);
    Task<SysUser> CreateUserAsync(SysUser user, List<long> roleIds);
    Task UpdateUserRolesAsync(SysUser user, List<long> roleIds);
    Task AssignRolesAsync(long userId, List<long> roleIds);
}