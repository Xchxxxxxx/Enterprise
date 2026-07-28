namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseAuditEntity<TKey> : BaseEntity<TKey>
    where TKey : struct
{
    public bool IsDeleted { get; set; } = false;
    public DateTimeOffset CreatedTime { get; set; } = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "China Standard Time");
    public long CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedTime { get; set; }
    public long? LastModifiedBy { get; set; }
}

public abstract class BaseAuditEntity : BaseAuditEntity<long>
{
}