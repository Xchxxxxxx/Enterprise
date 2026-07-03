namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserLoginSuccessEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset LoginTime { get; set; }
}