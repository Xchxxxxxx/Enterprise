namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseAuditEntity<TKey> : BaseEntity<TKey>
    where TKey : struct
{
    public bool IsDeleted { get; internal set; } = false;
    public DateTimeOffset CreatedTime { get; internal set; }
    public long CreatedBy { get; internal set; }
    public DateTimeOffset? LastModifiedTime { get; internal set; }
    public long? LastModifiedBy { get; internal set; }
}

public abstract class BaseAuditEntity : BaseAuditEntity<long>
{
}