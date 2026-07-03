namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserRolesAssignedEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public List<long> RoleIds { get; set; } = new();
    public long AssignedBy { get; set; }
}