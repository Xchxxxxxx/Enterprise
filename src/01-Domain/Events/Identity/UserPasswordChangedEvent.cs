namespace EfCore.Enterprise.Domain.Events.Identity;

public class UserPasswordChangedEvent : DomainEvent
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsResetByAdmin { get; set; }
    public long OperatedBy { get; set; }
}