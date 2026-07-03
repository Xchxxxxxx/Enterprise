using EfCore.Enterprise.Shared.DependencyInjection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IComplianceAuditService
{
    Task LogOperationAsync(string entityName, string operation, string data, string? userId = null);
    Task<bool> VerifyLogIntegrityAsync(string logId);
    Task<string> GenerateComplianceReportAsync(DateTime startDate, DateTime endDate);
    Task ArchiveLogsAsync(DateTime beforeDate);
}

[IgnoreInjectable]
public class ComplianceAuditService : IComplianceAuditService
{
    private readonly ILogger<ComplianceAuditService> _logger;
    private readonly string _logStoragePath;

    public ComplianceAuditService(ILogger<ComplianceAuditService> logger, string logStoragePath)
    {
        _logger = logger;
        _logStoragePath = logStoragePath;

        if (!Directory.Exists(logStoragePath))
        {
            Directory.CreateDirectory(logStoragePath);
        }
    }

    public async Task LogOperationAsync(
        string entityName,
        string operation,
        string data,
        string? userId = null)
    {
        var logEntry = new ComplianceLogEntry
        {
            LogId = Guid.NewGuid().ToString("N"),
            EntityName = entityName,
            Operation = operation,
            Data = data,
            UserId = userId ?? "system",
            Timestamp = DateTimeOffset.UtcNow,
            Hash = string.Empty
        };

        logEntry.Hash = ComputeHash(logEntry);

        var logFile = Path.Combine(_logStoragePath,
            $"compliance-{DateTimeOffset.UtcNow:yyyyMMdd}.log");

        var logLine = JsonSerializer.Serialize(logEntry) + Environment.NewLine;
        await File.AppendAllTextAsync(logFile, logLine);

        _logger.LogInformation("合规审计日志已记�? {LogId}", logEntry.LogId);
    }

    public async Task<bool> VerifyLogIntegrityAsync(string logId)
    {
        var logFiles = Directory.GetFiles(_logStoragePath, "compliance-*.log");
        foreach (var file in logFiles)
        {
            var lines = await File.ReadAllLinesAsync(file);
            foreach (var line in lines)
            {
                var entry = JsonSerializer.Deserialize<ComplianceLogEntry>(line);
                if (entry?.LogId == logId)
                {
                    var expectedHash = ComputeHash(new ComplianceLogEntry
                    {
                        LogId = entry.LogId,
                        EntityName = entry.EntityName,
                        Operation = entry.Operation,
                        Data = entry.Data,
                        UserId = entry.UserId,
                        Timestamp = entry.Timestamp,
                        Hash = string.Empty
                    });

                    return expectedHash == entry.Hash;
                }
            }
        }
        return false;
    }

    public async Task<string> GenerateComplianceReportAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("合规审计报告");
        report.AppendLine($"时间范围: {startDate:yyyy-MM-dd} �?{endDate:yyyy-MM-dd}");
        report.AppendLine();

        var logFiles = Directory.GetFiles(_logStoragePath, "compliance-*.log");

        foreach (var file in logFiles)
        {
            var lines = await File.ReadAllLinesAsync(file);
            foreach (var line in lines)
            {
                var entry = JsonSerializer.Deserialize<ComplianceLogEntry>(line);
                if (entry != null &&
                    entry.Timestamp >= startDate &&
                    entry.Timestamp <= endDate)
                {
                    report.AppendLine(
                        $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Operation} - {entry.EntityName} - {entry.UserId}");
                }
            }
        }
        return report.ToString();
    }

    public async Task ArchiveLogsAsync(DateTime beforeDate)
    {
        var archiveDir = Path.Combine(_logStoragePath, "archive",
            beforeDate.ToString("yyyyMM"));
        Directory.CreateDirectory(archiveDir);

        var logFiles = Directory.GetFiles(_logStoragePath, "compliance-*.log");
        foreach (var file in logFiles)
        {
            var fileDate = ParseDateFromFileName(file);
            if (fileDate < beforeDate)
            {
                var destFile = Path.Combine(archiveDir, Path.GetFileName(file));
                File.Move(file, destFile);
                _logger.LogInformation("合规日志已归�? {File}", destFile);
            }
        }
    }

    private string ComputeHash(ComplianceLogEntry entry)
    {
        var raw = $"{entry.LogId}|{entry.EntityName}|{entry.Operation}|{entry.Data}|{entry.UserId}|{entry.Timestamp:O}";
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private DateTime ParseDateFromFileName(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var datePart = fileName.Replace("compliance-", "");
        return DateTime.TryParseExact(datePart, "yyyyMMdd", null,
            System.Globalization.DateTimeStyles.None, out var date) ? date : DateTime.MinValue;
    }
}

public class ComplianceLogEntry
{
    public string LogId { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Hash { get; set; } = string.Empty;
}
