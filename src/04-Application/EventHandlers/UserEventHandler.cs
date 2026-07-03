using EfCore.Enterprise.Domain.Events.Identity;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Domain.Entities.Identity;
using EfCore.Enterprise.Infrastructure.Caching;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Application.EventHandlers;

public class UserEventHandler :
    INotificationHandler<UserCreatedEvent>,
    INotificationHandler<UserUpdatedEvent>,
    INotificationHandler<UserDeletedEvent>,
    INotificationHandler<UserLoginSuccessEvent>,
    INotificationHandler<UserLoginFailedEvent>,
    INotificationHandler<UserPasswordChangedEvent>,
    INotificationHandler<UserLockedEvent>,
    INotificationHandler<UserUnlockedEvent>,
    INotificationHandler<UserRolesAssignedEvent>,
    INotificationHandler<UserBatchEnabledEvent>
{
    private readonly ILogger<UserEventHandler> _logger;
    private readonly ICacheService _cache;
    private readonly ISuperRepository<LoginLog> _loginLogRepo;

    public UserEventHandler(
        ILogger<UserEventHandler> logger,
        ICacheService cache,
        ISuperRepository<LoginLog> loginLogRepo)
    {
        _logger = logger;
        _cache = cache;
        _loginLogRepo = loginLogRepo;
    }

    public async Task Handle(UserCreatedEvent e, CancellationToken ct)
    {
        _logger.LogInformation("领域事件: 用户创建 UserId={UserId}, Username={Username}", e.UserId, e.Username);

        await _cache.RemoveAsync($"cache:user:list", ct);
        await _cache.RemoveAsync("cache:user:stats", ct);
    }

    public async Task Handle(UserUpdatedEvent e, CancellationToken ct)
    {
        _logger.LogInformation("领域事件: 用户更新 UserId={UserId}, 变更字段={Fields}",
            e.UserId, string.Join(",", e.ChangedFields));

        await _cache.RemoveAsync($"cache:user:{e.UserId}", ct);
        await _cache.RemoveAsync($"cache:user:list", ct);
    }

    public async Task Handle(UserDeletedEvent e, CancellationToken ct)
    {
        _logger.LogWarning("领域事件: 用户删除 UserIds={Ids}", string.Join(",", e.UserIds));

        foreach (var userId in e.UserIds)
        {
            await _cache.RemoveAsync($"cache:user:{userId}", ct);
        }
        await _cache.RemoveAsync($"cache:user:list", ct);
        await _cache.RemoveAsync("cache:user:stats", ct);
    }

    public async Task Handle(UserLoginSuccessEvent e, CancellationToken ct)
    {
        _logger.LogInformation("领域事件: 登录成功 UserId={UserId}, IP={Ip}", e.UserId, e.IpAddress);

        await _cache.RemoveAsync($"cache:user:{e.UserId}", ct);
        await _cache.RemoveAsync("cache:user:stats", ct);
    }

    public Task Handle(UserLoginFailedEvent e, CancellationToken ct)
    {
        _logger.LogWarning("领域事件: 登录失败 UserId={UserId}, 原因={Reason}",
            e.UserId, e.FailReason);
        return Task.CompletedTask;
    }

    public async Task Handle(UserPasswordChangedEvent e, CancellationToken ct)
    {
        _logger.LogWarning("领域事件: 密码修改 UserId={UserId}, 管理员重置={IsReset}",
            e.UserId, e.IsResetByAdmin);

        await _cache.RemoveAsync($"cache:user:{e.UserId}", ct);
    }

    public Task Handle(UserLockedEvent e, CancellationToken ct)
    {
        _logger.LogWarning("领域事件: 账号锁定 UserId={UserId}, 解锁时间={LockoutEnd}",
            e.UserId, e.LockoutEnd);
        return Task.CompletedTask;
    }

    public async Task Handle(UserUnlockedEvent e, CancellationToken ct)
    {
        _logger.LogInformation("领域事件: 账号解锁 UserId={UserId}", e.UserId);

        await _cache.RemoveAsync($"cache:user:{e.UserId}", ct);
    }

    public async Task Handle(UserRolesAssignedEvent e, CancellationToken ct)
    {
        _logger.LogInformation("领域事件: 角色分配 UserId={UserId}, RoleIds={RoleIds}",
            e.UserId, string.Join(",", e.RoleIds));

        await _cache.RemoveAsync($"cache:user:{e.UserId}", ct);
        await _cache.RemoveAsync($"cache:user:permissions:{e.UserId}", ct);
    }

    public Task Handle(UserBatchEnabledEvent e, CancellationToken ct)
    {
        _logger.LogInformation("领域事件: 批量{Action} UserIds={Ids}",
            e.IsEnabled ? "启用" : "禁用", string.Join(",", e.UserIds));

        return Task.CompletedTask;
    }
}