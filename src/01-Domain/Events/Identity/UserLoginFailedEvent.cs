namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserLoginFailedEvent : DomainEvent
{
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? IpAddress { get; set; }
    public string FailReason { get; set; } = string.Empty;
}