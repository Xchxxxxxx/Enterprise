namespace EfCore.Enterprise.Domain.Interfaces;

public interface ICurrentUser
{
    long? UserId { get; }
    string? UserName { get; }
    IEnumerable<long> Roles { get; }
    IEnumerable<string> Permissions { get; }
    bool IsAuthenticated { get; }
    long? TenantId { get; }
    string? ClientIp { get; }
}