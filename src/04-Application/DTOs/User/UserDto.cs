using System.ComponentModel.DataAnnotations;

namespace EfCore.Enterprise.Application.DTOs.User;

public class UserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsEnabled { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool PhoneConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LastLoginTime { get; set; }
    public string? LastLoginIp { get; set; }
    public int LoginFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<long> RoleIds { get; set; } = new();
}

public class CreateUserRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    [MinLength(3, ErrorMessage = "用户名至少3位")]
    [MaxLength(50, ErrorMessage = "用户名最多50位")]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_]*$", ErrorMessage = "用户名必须以字母开头，只允许字母、数字和下划线")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    [MinLength(8, ErrorMessage = "密码至少8位")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "密码必须包含大小写字母、数字和特殊字符")]
    public string Password { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Nickname { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "手机号格式不正确")]
    public string? Phone { get; set; }

    public string? Avatar { get; set; }

    public List<long> RoleIds { get; set; } = new();
}

public class UpdateUserRequest
{
    [Required(ErrorMessage = "用户ID不能为空")]
    public long Id { get; set; }

    [MaxLength(50)]
    public string? Nickname { get; set; }

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "手机号格式不正确")]
    public string? Phone { get; set; }

    public string? Avatar { get; set; }

    public bool? IsEnabled { get; set; }
    public List<long>? RoleIds { get; set; }
}

public class UserQueryRequest
{
    public string? Username { get; set; }
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? IsEnabled { get; set; }
    public DateTimeOffset? CreatedTimeStart { get; set; }
    public DateTimeOffset? CreatedTimeEnd { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortField { get; set; }
    public bool SortDesc { get; set; }
}

public class AssignRolesRequest
{
    [Required(ErrorMessage = "用户ID不能为空")]
    public long UserId { get; set; }

    [Required(ErrorMessage = "角色ID不能为空")]
    public List<long> RoleIds { get; set; } = new();
}

public class BatchDeleteRequest
{
    [Required(ErrorMessage = "用户ID列表不能为空")]
    [MinLength(1, ErrorMessage = "至少选择一个用户")]
    public List<long> Ids { get; set; } = new();
}

public class BatchEnableRequest
{
    [Required]
    public List<long> Ids { get; set; } = new();
    public bool IsEnabled { get; set; }
}

public class LoginLogDto
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public string? Location { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailReason { get; set; }
    public DateTimeOffset LoginTime { get; set; }
}

public class LoginLogQueryRequest
{
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public bool? IsSuccess { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class UserStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int DisabledUsers { get; set; }
    public int LockedUsers { get; set; }
    public int OnlineToday { get; set; }
    public int NewToday { get; set; }
    public List<DailyStat> DailyStats { get; set; } = new();
}

public class DailyStat
{
    public string Date { get; set; } = string.Empty;
    public int LoginCount { get; set; }
    public int NewUserCount { get; set; }
}