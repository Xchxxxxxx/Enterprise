using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using EfCore.Enterprise.Domain.Events;

namespace CompanyName.Infrastructure.Data;

/// <summary>
/// EF Core 设计时工厂，用于 dotnet ef migrations 数据迁移
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("在 appsettings.json 中找不到 connectionString DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        });

        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        var auditInterceptor = new AuditInterceptor();
        var softDeleteInterceptor = new SoftDeleteInterceptor();
        var tenantInterceptor = new TenantInterceptor();
        var complianceInterceptor = new ComplianceInterceptor();

        optionsBuilder.AddInterceptors(
            auditInterceptor,
            softDeleteInterceptor,
            tenantInterceptor,
            complianceInterceptor);

        return new AppDbContext(
            optionsBuilder.Options,
            auditInterceptor,
            softDeleteInterceptor,
            tenantInterceptor,
            complianceInterceptor,
            null!);
    }
}