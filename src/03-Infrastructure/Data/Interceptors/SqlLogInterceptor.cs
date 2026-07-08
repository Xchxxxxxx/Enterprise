using EfCore.Enterprise.Shared.DependencyInjection;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data.Interceptors;

public class SqlLogInterceptor : DbCommandInterceptor
{
    private readonly ILogger<SqlLogInterceptor> _logger;
    private readonly long _slowSqlThresholdMs;

    public SqlLogInterceptor(ILogger<SqlLogInterceptor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _slowSqlThresholdMs = configuration.GetValue<long>("Monitoring:SlowSqlThresholdMs", 200);
    }



    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SQL: {CommandText}", command.CommandText);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogSlowSql(command, eventData);
        return new ValueTask<DbDataReader>(result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogSlowSql(command, eventData);
        return new ValueTask<int>(result);
    }

    private void LogSlowSql(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration.TotalMilliseconds > _slowSqlThresholdMs)
        {
            _logger.LogWarning(
                "慢SQL告警: 耗时 {Duration}ms, SQL: {CommandText}, 参数: {Parameters}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText,
                string.Join(", ", command.Parameters.Cast<DbParameter>()
                    .Select(p => $"{p.ParameterName}={p.Value}")));
        }
    }
}