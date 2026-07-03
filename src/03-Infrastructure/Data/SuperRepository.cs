using EfCore.Enterprise.Shared.DependencyInjection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Shared.Exceptions;
using EfCore.Enterprise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Enterprise.Infrastructure.Data;

[Injectable(ServiceLifetime.Scoped)]
public class SuperRepository<TEntity> : ISuperRepository<TEntity>
    where TEntity : BaseEntity<long>
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public SuperRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual IQueryable<TEntity> Query()
    {
        if (typeof(BaseAuditEntity).IsAssignableFrom(typeof(TEntity)))
        {
            return _dbSet.Where(e => !((BaseAuditEntity)(object)e).IsDeleted);
        }
        return _dbSet;
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return await Query().FirstOrDefaultAsync(
            e => e.Id == id,
            cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await Query().ToListAsync(cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query().AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query().CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        Expression<Func<TEntity, bool>> predicate,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(predicate);
        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortField))
        {
            query = ApplySorting(query, request.SortField, request.IsAscending);
        }

        var items = await query
            .Skip((request.PageIndex - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, request.PageIndex, request.PageSize);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedWithCursorAsync(
        Expression<Func<TEntity, bool>> predicate,
        long? lastId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(predicate);

        if (lastId.HasValue)
        {
            query = query.Where(e => e.Id > lastId.Value);
        }

        var items = await query
            .OrderBy(e => e.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return new PagedResult<TEntity>(items, items.Count, 1, pageSize);
    }

    public virtual async Task<List<TEntity>> GetByIdsAsync(
        List<long> ids,
        CancellationToken cancellationToken = default)
    {
        return await Query().Where(e => ids.Contains(e.Id)).ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task<List<TEntity>> AddRangeAsync(
        List<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        return entities;
    }

    public virtual Task UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task UpdateRangeAsync(
        List<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null) _dbSet.Remove(entity);
    }

    public virtual Task DeleteRangeAsync(
        List<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public virtual async Task BulkAddAsync(
        List<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task BulkMergeAsync(
        List<TEntity> entities,
        Expression<Func<TEntity, object>> matchKey,
        CancellationToken cancellationToken = default)
    {
        var existingIds = await Query().Select(matchKey).ToListAsync(cancellationToken);
        var newItems = entities.Where(e => !existingIds.Contains(matchKey.Compile()(e)));
        var existingItems = entities.Where(e => existingIds.Contains(matchKey.Compile()(e)));

        if (newItems.Any()) await _dbSet.AddRangeAsync(newItems, cancellationToken);
        if (existingItems.Any()) _dbSet.UpdateRange(existingItems);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task BulkDeleteAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var entities = await Query().Where(predicate).ToListAsync(cancellationToken);
        _dbSet.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task SoftDeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException();

        if (entity is BaseAuditEntity auditEntity)
        {
            auditEntity.IsDeleted = true;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task SoftDeleteAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        if (entity is BaseAuditEntity auditEntity)
        {
            auditEntity.IsDeleted = true;
            _dbSet.Update(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task SoftDeleteRangeAsync(
        List<long> ids,
        CancellationToken cancellationToken = default)
    {
        var entities = await GetByIdsAsync(ids, cancellationToken);
        foreach (var entity in entities)
        {
            if (entity is BaseAuditEntity auditEntity)
            {
                auditEntity.IsDeleted = true;
            }
        }
        _dbSet.UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task RestoreAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null) throw new NotFoundException();
        if (entity is BaseAuditEntity auditEntity)
        {
            auditEntity.IsDeleted = false;
            _dbSet.Update(entity);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsByUniqueKeyAsync(
        Expression<Func<TEntity, bool>> uniquePredicate,
        CancellationToken cancellationToken = default)
    {
        return await Query().AnyAsync(uniquePredicate, cancellationToken);
    }

    public virtual async Task<List<TEntity>> GetExportDataAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await Query().Where(predicate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async IAsyncEnumerable<TEntity> StreamQueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = Query().Where(predicate).AsNoTracking().AsAsyncEnumerable();
        await foreach (var item in query.WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    public virtual async Task LockAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException();

        if (entity is BaseComplianceEntity compliance)
        {
            compliance.IsArchived = true;
            compliance.ArchivedTime = DateTimeOffset.UtcNow;
            _dbSet.Update(entity);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UnlockAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null) throw new NotFoundException();

        if (entity is BaseComplianceEntity compliance)
        {
            compliance.IsArchived = false;
            compliance.ArchivedTime = null;
            _dbSet.Update(entity);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task ArchiveAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException();

        if (entity is BaseComplianceEntity compliance)
        {
            compliance.IsArchived = true;
            compliance.ComplianceArchiveTime = DateTimeOffset.UtcNow;
            _dbSet.Update(entity);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task ArchiveRangeAsync(
        List<long> ids,
        CancellationToken cancellationToken = default)
    {
        var entities = await GetByIdsAsync(ids, cancellationToken);
        foreach (var entity in entities)
        {
            if (entity is BaseComplianceEntity compliance)
            {
                compliance.IsArchived = true;
                compliance.ComplianceArchiveTime = DateTimeOffset.UtcNow;
            }
        }
        _dbSet.UpdateRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<TEntity> ApplySorting(
        IQueryable<TEntity> query,
        string sortField,
        bool isAscending)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var property = Expression.Property(parameter, sortField);
        var lambda = Expression.Lambda(property, parameter);
        var methodName = isAscending ? "OrderBy" : "OrderByDescending";

        var result = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), property.Type)
            .Invoke(null, new object[] { query, lambda });

        return (IQueryable<TEntity>)result!;
    }
}