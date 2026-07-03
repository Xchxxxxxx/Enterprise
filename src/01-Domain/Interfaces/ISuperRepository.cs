using System.Linq.Expressions;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Shared.Models;

namespace EfCore.Enterprise.Domain.Interfaces;

public interface ISuperRepository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    Task<PagedResult<TEntity>> GetPagedWithCursorAsync(
        Expression<Func<TEntity, bool>> predicate,
        long? lastId,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetByIdsAsync(
        List<long> ids,
        CancellationToken cancellationToken = default);

    Task BulkAddAsync(
        List<TEntity> entities,
        CancellationToken cancellationToken = default);

    Task BulkMergeAsync(
        List<TEntity> entities,
        Expression<Func<TEntity, object>> matchKey,
        CancellationToken cancellationToken = default);

    Task BulkDeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    Task SoftDeleteRangeAsync(
        List<long> ids,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByUniqueKeyAsync(
        Expression<Func<TEntity, bool>> uniquePredicate,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetExportDataAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<TEntity> StreamQueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task LockAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task UnlockAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task ArchiveRangeAsync(
        List<long> ids,
        CancellationToken cancellationToken = default);
}

public interface ISuperRepository<TEntity, TKey> : IRepository<TEntity>
    where TEntity : BaseEntity<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(TKey id, CancellationToken cancellationToken = default);

    Task RestoreAsync(TKey id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetByIdsAsync(
        List<TKey> ids,
        CancellationToken cancellationToken = default);
}