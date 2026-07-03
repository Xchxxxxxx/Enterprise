using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;

namespace EfCore.Enterprise.Infrastructure.Data.DomesticDatabase;

public enum DomesticDatabaseType
{
    SqlServer,
    Dameng,
    Kingbase,
    OceanBase,
    TiDB,
    GaussDB
}

public static class DomesticDatabaseProvider
{
    public static DbContextOptionsBuilder UseDomesticDatabase(
        this DbContextOptionsBuilder optionsBuilder,
        DomesticDatabaseType dbType,
        string connectionString,
        ILogger logger)
    {
        switch (dbType)
        {
            case DomesticDatabaseType.Dameng:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                    opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                logger.LogInformation("达梦数据库适配已启用");
                break;

            case DomesticDatabaseType.Kingbase:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                    opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                logger.LogInformation("人大金仓数据库适配已启用");
                break;

            case DomesticDatabaseType.OceanBase:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                    opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                logger.LogInformation("OceanBase数据库适配已启用");
                break;

            case DomesticDatabaseType.TiDB:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                    opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                logger.LogInformation("TiDB数据库适配已启用");
                break;

            case DomesticDatabaseType.GaussDB:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                    opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                logger.LogInformation("GaussDB数据库适配已启用");
                break;

            default:
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), opts =>
                    opts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
                break;
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