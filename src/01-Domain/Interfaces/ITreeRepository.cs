using EfCore.Enterprise.Domain.Entities;

namespace EfCore.Enterprise.Domain.Interfaces;

public interface ITreeRepository<TEntity> : ISuperRepository<TEntity>
    where TEntity : BaseTreeEntity<TEntity>
{
    Task<List<TEntity>> GetTreeAsync(CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetDescendantsAsync(long nodeId, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetAncestorsAsync(long nodeId, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetFlatListAsync(CancellationToken cancellationToken = default);

    Task MoveAsync(long nodeId, long? newParentId, CancellationToken cancellationToken = default);

    Task<List<TEntity>> GetRootNodesAsync(CancellationToken cancellationToken = default);
}