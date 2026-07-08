namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseFullEntity<TKey> : BaseAuditEntity<TKey>
    where TKey : struct
{
    public long TenantId { get; internal set; }
    public byte[] RowVersion { get; internal set; } = Array.Empty<byte>();
    public string? Remark { get; internal set; }
}

public abstract class BaseFullEntity : BaseFullEntity<long>
{
}