using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

[Injectable(ServiceLifetime.Singleton)]
public class ComplianceInterceptor : ISaveChangesInterceptor
{
    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateCompliance(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateCompliance(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void ValidateCompliance(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseComplianceEntity>())
        {
            if (entry.Entity.IsArchived &&
                entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new DataArchivedException(
                    $"数据已归档封存，禁止修改或删除: {entry.Entity.GetType().Name}");
            }
        }
    }
}