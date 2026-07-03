using EfCore.Enterprise.Domain.Events.Identity;

namespace EfCore.Enterprise.Domain.Entities.Permission;

public class SysUser : BaseFullEntity
{
    private SysUser() { }

    public SysUser(string username, string passwordHash, string passwordSalt, string? nickname = null)
    {
        Username = username;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        Nickname = nickname;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public string? Nickname { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Avatar { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public bool EmailConfirmed { get; private set; }
    public bool PhoneConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public DateTimeOffset? LastLoginTime { get; private set; }
    public string? LastLoginIp { get; private set; }
    public int LoginFailedCount { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public string SecurityStamp { get; private set; } = Guid.NewGuid().ToString("N");

    public void UpdateProfile(string? nickname, string? email, string? phone, string? avatar)
    {
        Nickname = nickname;
        Email = email;
        Phone = phone;
        Avatar = avatar;
    }

    public void ChangePassword(string newHash, string newSalt)
    {
        PasswordHash = newHash;
        PasswordSalt = newSalt;
        SecurityStamp = Guid.NewGuid().ToString("N");
        LoginFailedCount = 0;
        LockoutEnd = null;
    }

    public void RecordLoginSuccess(string? ip, string? userAgent)
    {
        LoginFailedCount = 0;
        LockoutEnd = null;
        LastLoginTime = DateTimeOffset.UtcNow;
        LastLoginIp = ip;

        AddDomainEvent(new UserLoginSuccessEvent
        {
            UserId = Id,
            Username = Username,
            IpAddress = ip,
            UserAgent = userAgent,
            LoginTime = DateTimeOffset.UtcNow
        });
    }

    public void RecordLoginFailed(string? ip, string? failReason, int maxAttempts, int lockoutMinutes)
    {
        LoginFailedCount++;

        if (LoginFailedCount >= maxAttempts)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(lockoutMinutes);
            AddDomainEvent(new UserLockedEvent
            {
                UserId = Id,
                Username = Username,
                LockoutEnd = LockoutEnd.Value,
                Reason = $"登录失败{LoginFailedCount}次"
            });
        }

        AddDomainEvent(new UserLoginFailedEvent
        {
            UserId = Id,
            Username = Username,
            IpAddress = ip,
            FailReason = failReason ?? "密码错误"
        });
    }

    public void Unlock()
    {
        LoginFailedCount = 0;
        LockoutEnd = null;
        AddDomainEvent(new UserUnlockedEvent
        {
            UserId = Id,
            Username = Username,
            UnlockedBy = 0
        });
    }

    public void Enable()
    {
        IsEnabled = true;
        AddDomainEvent(new UserBatchEnabledEvent
        {
            UserIds = new List<long> { Id },
            IsEnabled = true,
            OperatedBy = 0
        });
    }

    public void Disable()
    {
        IsEnabled = false;
        LockoutEnd = null;
        LoginFailedCount = 0;
        AddDomainEvent(new UserBatchEnabledEvent
        {
            UserIds = new List<long> { Id },
            IsEnabled = false,
            OperatedBy = 0
        });
    }

    public void RecordCreated(long createdBy, List<long> roleIds)
    {
        AddDomainEvent(new UserCreatedEvent
        {
            UserId = Id,
            Username = Username,
            Nickname = Nickname,
            Email = Email,
            Phone = Phone,
            RoleIds = roleIds,
            CreatedBy = createdBy
        });
    }

    public void RecordUpdated(long updatedBy, List<string> changedFields)
    {
        AddDomainEvent(new UserUpdatedEvent
        {
            UserId = Id,
            Username = Username,
            UpdatedBy = updatedBy,
            ChangedFields = changedFields
        });
    }

    public void RecordDeleted(long deletedBy)
    {
        AddDomainEvent(new UserDeletedEvent
        {
            UserIds = new List<long> { Id },
            DeletedBy = deletedBy
        });
    }
}