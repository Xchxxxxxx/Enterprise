using Microsoft.EntityFrameworkCore.Storage;

namespace EfCore.Enterprise.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    Task<T> ExecuteStrategyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
    
    Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork<TContext> : IUnitOfWork
    where TContext : class
{
    TContext Context { get; }
}