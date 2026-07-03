namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseComplianceEntity<TKey> : BaseFullEntity<TKey>
{
    public bool IsArchived { get; internal set; } = false;
    public DateTimeOffset? ArchivedTime { get; internal set; }
    public string? ArchivedBy { get; internal set; }

    public string? OperationIp { get; internal set; }
    public string? OperationDevice { get; internal set; }

    public string? DataTraceCode { get; internal set; }

    public DateTimeOffset? ComplianceArchiveTime { get; internal set; }
}

public abstract class BaseComplianceEntity : BaseComplianceEntity<long>
{
}