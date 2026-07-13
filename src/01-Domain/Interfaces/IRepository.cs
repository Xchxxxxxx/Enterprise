using System.Linq.Expressions;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Shared.Models;

namespace EfCore.Enterprise.Domain.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> Query();

    Task<TEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetSortedListAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> orderBy,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetSortedListAsync(
        Expression<Func<TEntity, bool>> predicate,
        string sortField,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultSortedAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> orderBy,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetPagedSortedAsync(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, object>> orderBy,
        bool ascending,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<List<TEntity>> AddRangeAsync(
        List<TEntity> entities,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default);

    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(List<TEntity> entities, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IRepository<TEntity, TKey> : IRepository<TEntity>
    where TEntity : BaseEntity<TKey>
    where TKey : struct
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task DeleteAsync(TKey id, CancellationToken cancellationToken = default);
}