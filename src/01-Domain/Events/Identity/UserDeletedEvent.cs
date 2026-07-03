namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserDeletedEvent : DomainEvent
{
    public List<long> UserIds { get; set; } = new();
    public long DeletedBy { get; set; }
}