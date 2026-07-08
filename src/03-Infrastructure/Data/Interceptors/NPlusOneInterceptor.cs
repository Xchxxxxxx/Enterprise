using EfCore.Enterprise.Shared.DependencyInjection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

public class NPlusOneInterceptor : DbCommandInterceptor
{
    private readonly ILogger<NPlusOneInterceptor> _logger;
    private readonly Dictionary<string, int> _queryCounts = new();
    private readonly int _warningThreshold;

    public NPlusOneInterceptor(ILogger<NPlusOneInterceptor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _warningThreshold = configuration.GetValue<int>("Monitoring:NPlusOneWarningThreshold", 10);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        TrackQuery(command);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    private const int MaxTrackedQueries = 500;

    private void TrackQuery(DbCommand command)
    {
        var sql = command.CommandText.Trim();

        if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            var normalized = NormalizeSql(sql);
            if (_queryCounts.ContainsKey(normalized))
            {
                _queryCounts[normalized]++;
            }
            else
            {
                if (_queryCounts.Count >= MaxTrackedQueries)
                {
                    _logger.LogWarning("N+1检测达到容量上限 {MaxCount}，跳过新SQL模式追踪", MaxTrackedQueries);
                    return;
                }
                _queryCounts[normalized] = 1;
            }

            if (_queryCounts[normalized] >= _warningThreshold)
            {
                _logger.LogWarning(
                    "N+1查询告警: 相同模式SQL执行了 {Count} 次, SQL: {Sql}",
                    _queryCounts[normalized],
                    sql);
            }
        }
    }

    private string NormalizeSql(string sql)
    {
        return sql.Split("WHERE")[0].Trim();
    }
}