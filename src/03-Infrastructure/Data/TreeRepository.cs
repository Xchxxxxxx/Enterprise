using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Shared.Enums;
using EfCore.Enterprise.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Enterprise.Infrastructure.Data;

[Injectable(ServiceLifetime.Scoped)]
public class TreeRepository<TEntity> : SuperRepository<TEntity>, ITreeRepository<TEntity>
    where TEntity : BaseTreeEntity<TEntity>
{
    public TreeRepository(AppDbContext context) : base(context) { }

    public async Task<List<TEntity>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var all = await Query().OrderBy(e => e.Sort).ThenBy(e => e.Name).ToListAsync(cancellationToken);
        return BuildTree(all, null);
    }

    public async Task<List<TEntity>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(e => e.ParentId != null && e.ParentId.Equals(parentId))
            .OrderBy(e => e.Sort)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetDescendantsAsync(long nodeId, CancellationToken cancellationToken = default)
    {
        var node = await GetByIdAsync(nodeId, cancellationToken)
            ?? throw new NotFoundException();
        return await Query()
            .Where(e => e.Path.StartsWith(node.Path + "/"))
            .OrderBy(e => e.Level)
            .ThenBy(e => e.Sort)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetAncestorsAsync(long nodeId, CancellationToken cancellationToken = default)
    {
        var node = await GetByIdAsync(nodeId, cancellationToken)
            ?? throw new NotFoundException();
        if (string.IsNullOrEmpty(node.Path)) return new List<TEntity>();

        var ancestorIds = node.Path.Trim('/').Split('/')
            .Select(long.Parse)
            .ToList();

        return await Query()
            .Where(e => ancestorIds.Contains(e.Id))
            .OrderBy(e => e.Level)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetFlatListAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .OrderBy(e => e.Path)
            .ThenBy(e => e.Sort)
            .ToListAsync(cancellationToken);
    }

    public async Task MoveAsync(long nodeId, long? newParentId, CancellationToken cancellationToken = default)
    {
        var node = await GetByIdAsync(nodeId, cancellationToken)
            ?? throw new NotFoundException();

        if (newParentId == nodeId)
            throw new AppException(ErrorCode.ValidationError, "不能将节点移动到自身下");

        if (newParentId.HasValue)
        {
            var parent = await GetByIdAsync(newParentId.Value, cancellationToken);
            if (parent == null)
                throw new NotFoundException("目标父节点不存在");

            if (!string.IsNullOrEmpty(parent.Path) && parent.Path.Contains($"/{nodeId}/"))
                throw new AppException(ErrorCode.ValidationError, "不能将节点移动到其子节点下");
        }

        var oldPath = node.Path;
        node.Move(newParentId);

        if (newParentId.HasValue)
        {
            var newParent = await GetByIdAsync(newParentId.Value, cancellationToken);
            var newPath = string.IsNullOrEmpty(newParent!.Path) ? $"/{newParentId}" : $"{newParent.Path}/{newParentId}";
            node.Path = $"{newPath}/{nodeId}";
            node.Level = newPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length + 1;
        }
        else
        {
            node.Path = $"/{nodeId}";
            node.Level = 1;
        }

        var descendants = await Query()
            .Where(e => e.Path.StartsWith(oldPath + "/"))
            .ToListAsync(cancellationToken);

        foreach (var descendant in descendants)
        {
            descendant.Path = descendant.Path.Replace(oldPath, node.Path);
            descendant.Level = descendant.Path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length;
            _dbSet.Update(descendant);
        }

        _dbSet.Update(node);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<TEntity>> GetRootNodesAsync(CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(e => e.ParentId == null || e.ParentId.Equals(default(long)))
            .OrderBy(e => e.Sort)
            .ThenBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    private List<TEntity> BuildTree(List<TEntity> all, long? parentId)
    {
        var children = all.Where(e => e.ParentId != null && e.ParentId.Equals(parentId)
            || (e.ParentId == null && parentId == null)
            || (e.ParentId != null && e.ParentId.Equals(default(long)) && parentId == null))
            .ToList();

        foreach (var child in children)
        {
            child.Children = BuildTree(all, child.Id);
        }

        return children;
    }
}