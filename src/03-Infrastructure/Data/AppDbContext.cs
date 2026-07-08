using System.Reflection;
using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace EfCore.Enterprise.Infrastructure.Data;

public class AppDbContext : DbContext
{
    protected AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    private readonly AuditInterceptor _auditInterceptor;
    private readonly SoftDeleteInterceptor _softDeleteInterceptor;
    private readonly TenantInterceptor _tenantInterceptor;
    private readonly ComplianceInterceptor _complianceInterceptor;
    private readonly IDomainEventBus _domainEventBus;

    public AppDbContext(
        DbContextOptions options,
        AuditInterceptor auditInterceptor,
        SoftDeleteInterceptor softDeleteInterceptor,
        TenantInterceptor tenantInterceptor,
        ComplianceInterceptor complianceInterceptor,
        IDomainEventBus domainEventBus)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _softDeleteInterceptor = softDeleteInterceptor;
        _tenantInterceptor = tenantInterceptor;
        _complianceInterceptor = complianceInterceptor;
        _domainEventBus = domainEventBus;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Ignore<DomainEvent>();

        EntityConfigurationScanInterceptor.AutoConfigureEntities(
            modelBuilder,
            Assembly.GetExecutingAssembly());

        EntityConfigurationScanInterceptor.AutoConfigureEntities(
            modelBuilder,
            typeof(BaseEntity).Assembly);

        ConfigureGlobalFilters(modelBuilder);
    }

    protected override void ConfigureConventions(
        ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<string>()
            .HaveMaxLength(256);

        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 4);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        await DispatchDomainEventsAsync(cancellationToken);

        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEvents = ChangeTracker.Entries<BaseEntity<long>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        if (domainEvents.Count == 0)
            return;

        foreach (var entry in ChangeTracker.Entries<BaseEntity<long>>())
        {
            entry.Entity.ClearDomainEvents();
        }

        await _domainEventBus.PublishRangeAsync(domainEvents, cancellationToken);
    }

    private void ConfigureGlobalFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseAuditEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<bool>("IsDeleted")
                    .HasDefaultValue(false);
            }

            if (typeof(BaseFullEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<byte[]>("RowVersion")
                    .IsRowVersion();
            }

            if (typeof(BaseComplianceEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<bool>("IsArchived")
                    .HasDefaultValue(false);
            }
        }
    }
}