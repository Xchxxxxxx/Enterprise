namespace EfCore.Enterprise.Domain.Interfaces;

public interface ITenantProvider
{
    string? GetCurrentTenantId();
    long? GetCurrentTenantIdLong();
}