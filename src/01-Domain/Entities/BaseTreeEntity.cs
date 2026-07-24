using System.ComponentModel.DataAnnotations.Schema;
using EfCore.Enterprise.Domain.Events;

namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseTreeEntity<TEntity, TKey> : BaseFullEntity<TKey>
    where TEntity : BaseTreeEntity<TEntity, TKey>
    where TKey : struct
{
    protected BaseTreeEntity() { }

    protected BaseTreeEntity(string name, TKey? parentId = default)
    {
        Name = name;
        ParentId = parentId;
        IsLeaf = true;
    }

    public string Name { get; set; } = string.Empty;
    public TKey? ParentId { get; set; }
    public string Path { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Sort { get; set; }
    public bool IsLeaf { get; set; }

    [NotMapped]
    public List<TEntity> Children { get; set; } = new();

    public void Move(TKey? newParentId)
    {
        var oldParentId = ParentId;
        ParentId = newParentId;

        AddDomainEvent(new TreeNodeMovedEvent<TKey>
        {
            NodeId = Id,
            NodeName = Name,
            OldParentId = oldParentId,
            NewParentId = newParentId
        });
    }

    public void Rename(string newName)
    {
        Name = newName;
    }

    public void SetSort(int sort)
    {
        Sort = sort;
    }
}

public abstract class BaseTreeEntity<TEntity> : BaseTreeEntity<TEntity, long>
    where TEntity : BaseTreeEntity<TEntity, long>
{
    protected BaseTreeEntity() { }

    protected BaseTreeEntity(string name, long? parentId = default)
        : base(name, parentId) { }
}

public class TreeNodeMovedEvent<TKey> : DomainEvent
    where TKey : struct
{
    public TKey NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public TKey? OldParentId { get; set; }
    public TKey? NewParentId { get; set; }
}