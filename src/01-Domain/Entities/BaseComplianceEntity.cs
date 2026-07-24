namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseComplianceEntity<TKey> : BaseFullEntity<TKey>
    where TKey : struct
{
    public bool IsArchived { get; set; } = false;
    public DateTimeOffset? ArchivedTime { get; set; }
    public string? ArchivedBy { get; set; }

    public string? OperationIp { get; set; }
    public string? OperationDevice { get; set; }

    public string? DataTraceCode { get; set; }

    public DateTimeOffset? ComplianceArchiveTime { get; set; }
}

public abstract class BaseComplianceEntity : BaseComplianceEntity<long>
{
}