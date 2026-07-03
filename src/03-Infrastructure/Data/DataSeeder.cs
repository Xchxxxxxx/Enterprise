using EfCore.Enterprise.Domain.Entities.Permission;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data;

[Injectable(ServiceLifetime.Singleton)]
public class DataSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IServiceProvider serviceProvider, ILogger<DataSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("无法连接到MySQL数据库，跳过种子数据初始化");
                return;
            }

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            _logger.LogInformation("数据库重建完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "数据库初始化失败，跳过种子数据初始化");
            return;
        }

        var roleRepo = scope.ServiceProvider.GetRequiredService<ISuperRepository<SysRole>>();
        var permissionRepo = scope.ServiceProvider.GetRequiredService<ISuperRepository<SysPermission>>();
        var rolePermissionRepo = scope.ServiceProvider.GetRequiredService<ISuperRepository<SysRolePermission>>();
        var userRepo = scope.ServiceProvider.GetRequiredService<ISuperRepository<SysUser>>();
        var userRoleRepo = scope.ServiceProvider.GetRequiredService<ISuperRepository<SysUserRole>>();

        if (await roleRepo.Query().AnyAsync())
        {
            _logger.LogInformation("种子数据已存在，跳过初始化");
            return;
        }

        _logger.LogInformation("开始初始化种子数据...");

        var permissions = new List<SysPermission>
        {
            new("user:read", "用户查询", "查看用户列表和详情"),
            new("user:create", "用户创建", "创建新用户"),
            new("user:update", "用户更新", "修改用户信息"),
            new("user:delete", "用户删除", "删除用户"),
            new("user:reset-password", "重置密码", "重置用户密码"),
            new("user:assign-role", "分配角色", "分配用户角色"),
            new("user:export", "用户导出", "导出用户数据"),
            new("role:read", "角色查询", "查看角色列表"),
            new("role:create", "角色创建", "创建角色"),
            new("role:update", "角色更新", "修改角色"),
            new("role:delete", "角色删除", "删除角色"),
            new("log:read", "日志查询", "查看操作日志"),
            new("system:config", "系统配置", "系统配置管理"),
            new("system:monitor", "系统监控", "查看系统监控")
        };

        foreach (var p in permissions)
        {
            await permissionRepo.AddAsync(p);
        }
        await permissionRepo.SaveChangesAsync();

        var adminRole = new SysRole("admin", "超级管理员", "系统超级管理员，拥有所有权限", true);
        await roleRepo.AddAsync(adminRole);
        await roleRepo.SaveChangesAsync();

        var allPermissions = await permissionRepo.Query().ToListAsync();
        foreach (var p in allPermissions)
        {
            await rolePermissionRepo.AddAsync(new SysRolePermission(adminRole.Id, p.Id));
        }
        await rolePermissionRepo.SaveChangesAsync();

        var userRole = new SysRole("user", "普通用户", "普通用户");
        await roleRepo.AddAsync(userRole);
        await roleRepo.SaveChangesAsync();

        var basicPermissions = await permissionRepo.Query()
            .Where(p => p.Code == "user:read" || p.Code == "log:read")
            .ToListAsync();
        foreach (var p in basicPermissions)
        {
            await rolePermissionRepo.AddAsync(new SysRolePermission(userRole.Id, p.Id));
        }
        await rolePermissionRepo.SaveChangesAsync();

        var (adminHash, adminSalt) = PasswordHelper.HashPassword("Admin@123456");
        var admin = new SysUser("admin", adminHash, adminSalt, "系统管理员");
        await userRepo.AddAsync(admin);
        await userRepo.SaveChangesAsync();

        await userRoleRepo.AddAsync(new SysUserRole(admin.Id, adminRole.Id));
        await userRoleRepo.SaveChangesAsync();

        _logger.LogInformation("""
            种子数据初始化完成
            管理员账号: admin
            管理员密码: Admin@123456
            角色: 超级管理员(admin), 普通用户(user)
            权限: {Count}个权限项
            """, allPermissions.Count);
    }
}