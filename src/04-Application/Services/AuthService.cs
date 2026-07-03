using EfCore.Enterprise.Application.DTOs.Auth;
using EfCore.Enterprise.Application.DTOs.User;
using EfCore.Enterprise.Domain.Entities.Permission;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Domain.Services;
using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Application.Services;

[Injectable(ServiceLifetime.Scoped)]
public class AuthService
{
    private readonly ISuperRepository<SysUser> _userRepo;
    private readonly ISuperRepository<SysUserRole> _userRoleRepo;
    private readonly ISuperRepository<SysRole> _roleRepo;
    private readonly ISuperRepository<SysRolePermission> _rolePermissionRepo;
    private readonly ISuperRepository<SysPermission> _permissionRepo;
    private readonly IJwtService _jwtService;
    private readonly IUserDomainService _domainService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ISuperRepository<SysUser> userRepo,
        ISuperRepository<SysUserRole> userRoleRepo,
        ISuperRepository<SysRole> roleRepo,
        ISuperRepository<SysRolePermission> rolePermissionRepo,
        ISuperRepository<SysPermission> permissionRepo,
        IJwtService jwtService,
        IUserDomainService domainService,
        ILogger<AuthService> logger)
    {
        _userRepo = userRepo;
        _userRoleRepo = userRoleRepo;
        _roleRepo = roleRepo;
        _rolePermissionRepo = rolePermissionRepo;
        _permissionRepo = permissionRepo;
        _jwtService = jwtService;
        _domainService = domainService;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent)
    {
        var user = await _userRepo.Query()
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
        {
            await _domainService.RecordFailedLoginAsync(request.Username, ipAddress, userAgent, "用户不存在");
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        await _domainService.LoginAsync(user, request.Password, ipAddress, userAgent);

        var roleIds = await _userRoleRepo.Query()
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var roles = await _roleRepo.Query()
            .Where(r => roleIds.Contains(r.Id) && r.IsEnabled)
            .Select(r => r.Code)
            .ToListAsync();

        var permissionIds = await _rolePermissionRepo.Query()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var permissions = await _permissionRepo.Query()
            .Where(p => permissionIds.Contains(p.Id) && p.IsEnabled)
            .Select(p => p.Code)
            .Distinct()
            .ToListAsync();

        var (accessToken, refreshToken) = _jwtService.GenerateTokens(user.Id, user.Username, roles, permissions);

        _logger.LogInformation("用户 {Username} 登录成功", user.Username);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            User = new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                Email = user.Email,
                Phone = user.Phone,
                Avatar = user.Avatar,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var (accessToken, refreshToken) = _jwtService.RefreshToken(request.RefreshToken, request.AccessToken);

        var principal = _jwtService.ValidateToken(accessToken)
            ?? throw new UnauthorizedAccessException("Token无效");

        var userId = long.Parse(principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)!.Value;
        var roles = principal.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = principal.FindAll("permission").Select(c => c.Value).ToList();

        var user = await _userRepo.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            User = new UserInfo
            {
                Id = userId,
                Username = username,
                Nickname = user?.Nickname,
                Email = user?.Email,
                Phone = user?.Phone,
                Avatar = user?.Avatar,
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    public async Task LogoutAsync(long userId, string refreshToken)
    {
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);
        _logger.LogInformation("用户 {UserId} 已登出", userId);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var user = await _userRepo.Query().FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("用户不存在");

        if (!_domainService.VerifyPassword(request.OldPassword, user.PasswordHash, user.PasswordSalt))
            throw new UnauthorizedAccessException("旧密码不正确");

        var (hash, salt) = _domainService.HashPassword(request.NewPassword);
        user.ChangePassword(hash, salt);

        await _userRepo.UpdateAsync(user);
        await _userRepo.SaveChangesAsync();
        await _jwtService.RevokeAllUserTokensAsync(userId);
        _logger.LogInformation("用户 {UserId} 修改密码成功", userId);
    }
}