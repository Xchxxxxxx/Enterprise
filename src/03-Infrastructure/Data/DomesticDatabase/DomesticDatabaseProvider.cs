using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;

namespace EfCore.Enterprise.Infrastructure.Data.DomesticDatabase;

public enum DomesticDatabaseType
{
    SqlServer,
    MySql,
    Dameng,
    Kingbase,
    OceanBase,
    TiDB,
    GaussDB
}

public static class DomesticDatabaseProvider
{
    private static readonly HashSet<DomesticDatabaseType> MySqlCompatible = new()
    {
        DomesticDatabaseType.MySql,
        DomesticDatabaseType.OceanBase,
        DomesticDatabaseType.TiDB
    };

    public static DbContextOptionsBuilder UseDomesticDatabase(
        this DbContextOptionsBuilder optionsBuilder,
        DomesticDatabaseType dbType,
        string connectionString,
        ILogger logger)
    {
        if (MySqlCompatible.Contains(dbType))
        {
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
            logger.LogInformation("{DbType} 数据库适配已启用（MySQL兼容模式）", dbType);
        }
        else
        {
            logger.LogWarning("{DbType} 数据库适配暂未实现，降级为MySQL兼容模式", dbType);
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
        }

        return optionsBuilder;
    }

    public static string EscapeIdentifier(this DomesticDatabaseType dbType, string identifier)
    {
        return dbType switch
        {
            DomesticDatabaseType.Dameng => $"\"{identifier}\"",
            DomesticDatabaseType.Kingbase => $"\"{identifier}\"",
            DomesticDatabaseType.OceanBase => $"`{identifier}`",
            DomesticDatabaseType.TiDB => $"`{identifier}`",
            DomesticDatabaseType.GaussDB => $"\"{identifier}\"",
            _ => $"[{identifier}]"
        };
    }

    public static string GetPaginationSql(
        this DomesticDatabaseType dbType,
        string sql,
        int pageIndex,
        int pageSize)
    {
        var offset = (pageIndex - 1) * pageSize;
        return dbType switch
        {
            DomesticDatabaseType.Dameng
                or DomesticDatabaseType.Kingbase
                or DomesticDatabaseType.GaussDB
                => $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY",

            DomesticDatabaseType.OceanBase
                or DomesticDatabaseType.TiDB
                => $"{sql} LIMIT {pageSize} OFFSET {offset}",

            _ => $"{sql} OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY"
        };
    }
}