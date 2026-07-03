namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserLockedEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTimeOffset LockoutEnd { get; set; }
    public string Reason { get; set; } = string.Empty;
}