namespace EfCore.Enterprise.Application.DTOs;

public abstract class BaseDto
{
    public long Id { get; set; }
}

public abstract class BaseAuditDto : BaseDto
{
    public DateTimeOffset CreatedTime { get; set; }
    public long CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedTime { get; set; }
    public long? LastModifiedBy { get; set; }
}