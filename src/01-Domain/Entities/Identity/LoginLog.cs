namespace EfCore.Enterprise.Domain.Entities.Identity;

public class LoginLog : BaseEntity
{
    private LoginLog() { }

    public LoginLog(long? userId, string? username, string? ipAddress, string? userAgent,
        bool isSuccess, string? failReason = null)
    {
        UserId = userId;
        Username = username;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsSuccess = isSuccess;
        FailReason = failReason;
        LoginTime = DateTimeOffset.UtcNow;
    }

    public long? UserId { get; private set; }
    public string? Username { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? Location { get; private set; }
    public bool IsSuccess { get; private set; }
    public string? FailReason { get; private set; }
    public DateTimeOffset LoginTime { get; private set; } = DateTimeOffset.UtcNow;
}