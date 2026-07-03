namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserCreatedEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public List<long> RoleIds { get; set; } = new();
    public long CreatedBy { get; set; }
}