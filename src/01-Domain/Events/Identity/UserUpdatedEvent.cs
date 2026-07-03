namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserUpdatedEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public long UpdatedBy { get; set; }
    public List<string> ChangedFields { get; set; } = new();
}