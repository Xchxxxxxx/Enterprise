using EfCore.Enterprise.Domain.Entities.Identity;
using EfCore.Enterprise.Domain.Entities.Permission;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Domain.Services;

[Injectable(ServiceLifetime.Scoped)]
public class UserDomainService : IUserDomainService
{
    private const int MinPasswordLength = 8;
    private const int MaxLoginFailedAttempts = 5;
    private const int LockoutDurationMinutes = 15;

    private readonly ISuperRepository<SysUser> _userRepo;
    private readonly ISuperRepository<SysUserRole> _userRoleRepo;
    private readonly ISuperRepository<LoginLog> _loginLogRepo;

    public UserDomainService(
        ISuperRepository<SysUser> userRepo,
        ISuperRepository<SysUserRole> userRoleRepo,
        ISuperRepository<LoginLog> loginLogRepo)
    {
        _userRepo = userRepo;
        _userRoleRepo = userRoleRepo;
        _loginLogRepo = loginLogRepo;
    }

    public bool IsPasswordStrongEnough(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
            return false;

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    public bool IsLockoutPeriodExpired(DateTimeOffset? lockoutEnd)
    {
        return !lockoutEnd.HasValue || lockoutEnd.Value <= DateTimeOffset.UtcNow;
    }

    public bool ShouldLockAccount(int currentFailedCount)
    {
        return currentFailedCount >= MaxLoginFailedAttempts;
    }

    public int GetLockoutMinutes(int failedCount)
    {
        if (failedCount >= 10) return 60;
        if (failedCount >= 7) return 30;
        return LockoutDurationMinutes;
    }

    public (string hash, string salt) HashPassword(string password)
    {
        return PasswordHelper.HashPassword(password);
    }

    public bool VerifyPassword(string password, string hash, string salt)
    {
        return PasswordHelper.VerifyPassword(password, hash, salt);
    }

    public async Task LoginAsync(SysUser user, string password, string? ip, string? userAgent)
    {
        if (!user.IsEnabled)
        {
            await RecordLoginLog(user.Id, user.Username, ip, userAgent, false, "账号已禁用");
            throw new UnauthorizedAccessException("账号已被禁用");
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            var remaining = (user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes;
            await RecordLoginLog(user.Id, user.Username, ip, userAgent, false, $"账号已锁定({remaining:F0}分钟后解锁)");
            throw new UnauthorizedAccessException($"账号已锁定，请{remaining:F0}分钟后重试");
        }

        if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            user.RecordLoginFailed(ip, "密码错误", MaxLoginFailedAttempts, LockoutDurationMinutes);
            await _userRepo.UpdateAsync(user);
            await _userRepo.SaveChangesAsync();
            await RecordLoginLog(user.Id, user.Username, ip, userAgent, false, "密码错误");
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        user.RecordLoginSuccess(ip, userAgent);
        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();
        await RecordLoginLog(user.Id, user.Username, ip, userAgent, true, null);
    }

    public async Task RecordFailedLoginAsync(string username, string? ip, string? userAgent, string reason)
    {
        await RecordLoginLog(null, username, ip, userAgent, false, reason);
    }

    public async Task<SysUser> CreateUserAsync(SysUser user, List<long> roleIds)
    {
        var exists = await _userRepo.Query().AnyAsync(u => u.Username == user.Username);
        if (exists)
            throw new InvalidOperationException("用户名已存在");

        user.RecordCreated(0, roleIds);

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        if (roleIds.Count > 0)
        {
            foreach (var roleId in roleIds)
            {
                await _userRoleRepo.AddAsync(new SysUserRole(user.Id, roleId));
            }
            await _userRoleRepo.SaveChangesAsync();
        }

        return user;
    }

    public async Task UpdateUserRolesAsync(SysUser user, List<long> roleIds)
    {
        user.RecordUpdated(0, new List<string>());

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();

        var existingRoles = await _userRoleRepo.Query()
            .Where(ur => ur.UserId == user.Id)
            .ToListAsync();
        foreach (var er in existingRoles)
        {
            await _userRoleRepo.DeleteAsync(er);
        }
        await _userRoleRepo.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            await _userRoleRepo.AddAsync(new SysUserRole(user.Id, roleId));
        }
        await _userRoleRepo.SaveChangesAsync();
    }

    public async Task AssignRolesAsync(long userId, List<long> roleIds)
    {
        var existingRoles = await _userRoleRepo.Query()
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
        foreach (var er in existingRoles)
        {
            await _userRoleRepo.DeleteAsync(er);
        }
        await _userRoleRepo.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            await _userRoleRepo.AddAsync(new SysUserRole(userId, roleId));
        }
        await _userRoleRepo.SaveChangesAsync();
    }

    private async Task RecordLoginLog(long? userId, string? username, string? ip, string? userAgent, bool isSuccess, string? failReason)
    {
        var log = new LoginLog(userId, username, ip, userAgent, isSuccess, failReason);
        await _loginLogRepo.AddAsync(log);
        await _loginLogRepo.SaveChangesAsync();
    }
}