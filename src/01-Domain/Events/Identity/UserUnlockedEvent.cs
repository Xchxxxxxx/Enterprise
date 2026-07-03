namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserUnlockedEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public long UnlockedBy { get; set; }
}