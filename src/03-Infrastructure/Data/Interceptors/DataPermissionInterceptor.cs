using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

[Injectable(ServiceLifetime.Singleton)]
public class DataPermissionInterceptor : ISaveChangesInterceptor
{
    private readonly IDataPermissionProvider? _permissionProvider;

    public DataPermissionInterceptor(IDataPermissionProvider? permissionProvider = null)
    {
        _permissionProvider = permissionProvider;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<InterceptionResult<int>>(result);
    }
}

public interface IDataPermissionProvider
{
    IQueryable<T> ApplyDataPermission<T>(IQueryable<T> query) where T : BaseEntity<long>;
}