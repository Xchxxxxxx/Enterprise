using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

public class OptimisticLockInterceptor : ISaveChangesInterceptor
{
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
        try
        {
            return new ValueTask<InterceptionResult<int>>(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrentConflictException("数据已被其他用户修改，请刷新后重试");
        }
    }
}