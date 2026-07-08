using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

public class TenantInterceptor : ISaveChangesInterceptor
{
    private readonly ITenantProvider? _tenantProvider;

    public TenantInterceptor(ITenantProvider? tenantProvider = null)
    {
        _tenantProvider = tenantProvider;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyTenant(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyTenant(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void ApplyTenant(DbContext? context)
    {
        if (context == null || _tenantProvider == null) return;

        var tenantId = _tenantProvider.GetTenantId();

        foreach (var entry in context.ChangeTracker.Entries<BaseFullEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = tenantId;
            }
        }
    }
}

public interface ITenantProvider
{
    long GetTenantId();
}