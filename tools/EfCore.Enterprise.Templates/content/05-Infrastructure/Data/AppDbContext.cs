using EfCore.Enterprise.Infrastructure.Data;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using EfCore.Enterprise.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace CompanyName.Infrastructure.Data;

public class AppDbContext : EfCore.Enterprise.Infrastructure.Data.AppDbContext
{

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        AuditInterceptor auditInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor,
        TenantInterceptor tenantInterceptor,
        ComplianceInterceptor complianceInterceptor,
        IDomainEventBus domainEventBus)
        : base(options, auditInterceptor, softDeleteInterceptor, tenantInterceptor, complianceInterceptor, domainEventBus)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}