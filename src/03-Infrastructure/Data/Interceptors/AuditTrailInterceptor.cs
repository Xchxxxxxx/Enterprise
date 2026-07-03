using EfCore.Enterprise.Shared.DependencyInjection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

[Injectable(ServiceLifetime.Singleton)]
public class AuditTrailInterceptor : ISaveChangesInterceptor
{
    private readonly ILogger<AuditTrailInterceptor> _logger;

    public AuditTrailInterceptor(ILogger<AuditTrailInterceptor> logger)
    {
        _logger = logger;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        return result;
    }

    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context == null) return result;

        var auditEntries = new List<AuditEntry>();

        foreach (var entry in eventData.Context.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                var auditEntry = new AuditEntry
                {
                    EntityName = entry.Entity.GetType().Name,
                    State = entry.State.ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    KeyValues = JsonSerializer.Serialize(
                        entry.Properties.Where(p => p.Metadata.IsPrimaryKey())
                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue)),
                    OriginalValues = entry.State == EntityState.Modified
                        ? JsonSerializer.Serialize(
                            entry.Properties.Where(p => p.IsModified)
                                .ToDictionary(p => p.Metadata.Name, p => p.OriginalValue))
                        : null,
                    CurrentValues = JsonSerializer.Serialize(
                        entry.Properties.Where(p => !p.Metadata.IsPrimaryKey())
                            .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue))
                };
                auditEntries.Add(auditEntry);
            }
        }

        if (auditEntries.Count > 0)
        {
            _logger.LogInformation(
                "审计日志: {AuditData}",
                JsonSerializer.Serialize(auditEntries));
        }

        return result;
    }
}

public class AuditEntry
{
    public string EntityName { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string KeyValues { get; set; } = string.Empty;
    public string? OriginalValues { get; set; }
    public string CurrentValues { get; set; } = string.Empty;
}