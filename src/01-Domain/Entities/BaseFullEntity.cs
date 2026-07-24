namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseFullEntity<TKey> : BaseAuditEntity<TKey>
    where TKey : struct
{
    public long TenantId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public string? Remark { get; set; }
}

public abstract class BaseFullEntity : BaseFullEntity<long>
{
}