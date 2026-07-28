namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseAuditEntity<TKey> : BaseEntity<TKey>
    where TKey : struct
{
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedTime { get; set; } = DateTime.Now;
    public long CreatedBy { get; set; }
    public DateTime? LastModifiedTime { get; set; }
    public long? LastModifiedBy { get; set; }
}

public abstract class BaseAuditEntity : BaseAuditEntity<long>
{
}