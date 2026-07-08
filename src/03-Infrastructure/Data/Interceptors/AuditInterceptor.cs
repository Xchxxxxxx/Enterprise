using EfCore.Enterprise.Shared.DependencyInjection;
using System.Security.Claims;
using EfCore.Enterprise.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

public class AuditInterceptor : ISaveChangesInterceptor
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context == null) return;

        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is BaseAuditEntity entryEntity)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entryEntity.CreatedTime = now;
                        entryEntity.CreatedBy = userId;
                        if (entry.Entity is BaseComplianceEntity compliance)
                        {
                            compliance.OperationIp = GetClientIp();
                            compliance.OperationDevice = GetClientDevice();
                            compliance.DataTraceCode = GenerateTraceCode();
                        }
                        break;

                    case EntityState.Modified:
                        entryEntity.LastModifiedTime = now;
                        entryEntity.LastModifiedBy = userId;
                        if (entry.Entity is BaseComplianceEntity complianceMod)
                        {
                            complianceMod.OperationIp = GetClientIp();
                            complianceMod.OperationDevice = GetClientDevice();
                        }
                        break;
                }
            }
        }
    }

    private long GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor?.HttpContext?.User
            ?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetClientIp()
    {
        return _httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private string GetClientDevice()
    {
        return _httpContextAccessor?.HttpContext?.Request?.Headers?["User-Agent"].ToString() ?? "unknown";
    }

    private string GenerateTraceCode()
    {
        return $"TRACE-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }
}