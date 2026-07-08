using EfCore.Enterprise.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace EfCore.Enterprise.Domain.Entities;

public class OpLog : BaseEntity<long>
{
    private OpLog() { }

    public OpLog(string module, string action, string? detail, string? operatorName, long? operatorId, string? ip, string? userAgent, long? elapsed, bool success)
    {
        Module = module;
        Action = action;
        Detail = detail;
        OperatorName = operatorName;
        OperatorId = operatorId;
        Ip = ip;
        UserAgent = userAgent;
        Elapsed = elapsed;
        Success = success;
        OperateTime = DateTimeOffset.UtcNow;
    }

    [MaxLength(100)]
    public string Module { get; private set; } = string.Empty;

    [MaxLength(100)]
    public string Action { get; private set; } = string.Empty;

    [MaxLength(2000)]
    public string? Detail { get; private set; }

    [MaxLength(100)]
    public string? OperatorName { get; private set; }

    public long? OperatorId { get; private set; }

    [MaxLength(50)]
    public string? Ip { get; private set; }

    [MaxLength(500)]
    public string? UserAgent { get; private set; }

    public long? Elapsed { get; private set; }

    public bool Success { get; private set; }

    public DateTimeOffset OperateTime { get; private set; }
}