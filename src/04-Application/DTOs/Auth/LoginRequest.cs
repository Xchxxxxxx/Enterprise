using System.ComponentModel.DataAnnotations;

namespace EfCore.Enterprise.Application.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    public string? CaptchaId { get; set; }
    public string? CaptchaCode { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserInfo User { get; set; } = null!;
}

public class UserInfo
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "AccessToken不能为空")]
    public string AccessToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "RefreshToken不能为空")]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "旧密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(8, ErrorMessage = "密码至少8位")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        ErrorMessage = "密码必须包含大小写字母、数字和特殊字符")]
    public string NewPassword { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "用户ID不能为空")]
    public long UserId { get; set; }

    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(8, ErrorMessage = "密码至少8位")]
    public string NewPassword { get; set; } = string.Empty;
}