namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserBatchEnabledEvent : DomainEvent
{
    public List<long> UserIds { get; set; } = new();
    public bool IsEnabled { get; set; }
    public long OperatedBy { get; set; }
}