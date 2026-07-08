using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Infrastructure.Data.Interceptors;
using Microsoft.Extensions.Configuration;

namespace EfCore.Enterprise.Infrastructure.Data;

/// <summary>
/// EF Core 设计时工厂（泛型），用于 dotnet ef migrations 数据迁移
/// 
/// 实战用法（一行继承）：
///   public class MyDbContextFactory : DesignTimeAppDbContextFactory&lt;MyDbContext&gt; { }
/// </summary>
public class DesignTimeAppDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : AppDbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var basePath = FindAppSettingsPath(Directory.GetCurrentDirectory());

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")!;

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sql =>
        {
            sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
            sql.MigrationsAssembly(typeof(TContext).Assembly.FullName);
        });

        var audit = new AuditInterceptor();
        var softDelete = new SoftDeleteInterceptor();
        var tenant = new TenantInterceptor();
        var compliance = new ComplianceInterceptor();

        optionsBuilder.AddInterceptors(audit, softDelete, tenant, compliance);

        return (TContext)Activator.CreateInstance(typeof(TContext),
            optionsBuilder.Options, audit, softDelete, tenant, compliance, (IDomainEventBus?)null)!;
    }

    private static string FindAppSettingsPath(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
                return dir.FullName;

            foreach (var sub in dir.GetDirectories())
            {
                if (File.Exists(Path.Combine(sub.FullName, "appsettings.json")))
                    return sub.FullName;
            }

            dir = dir.Parent;
        }
        return startDir;
    }
}