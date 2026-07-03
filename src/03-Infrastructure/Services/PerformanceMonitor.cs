using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IPerformanceMonitor
{
    void TrackRequest(string endpoint, long elapsedMs, int statusCode);
    void TrackSqlExecution(string sql, long elapsedMs);
    PerformanceReport GetReport();
}

[Injectable(ServiceLifetime.Singleton)]
public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, RequestMetrics> _requestMetrics = new();
    private readonly ConcurrentBag<SqlMetric> _sqlMetrics = new();
    private readonly ILogger<PerformanceMonitor> _logger;

    public PerformanceMonitor(ILogger<PerformanceMonitor> logger)
    {
        _logger = logger;
    }

    public void TrackRequest(string endpoint, long elapsedMs, int statusCode)
    {
        var metrics = _requestMetrics.GetOrAdd(endpoint, _ => new RequestMetrics());
        metrics.AddRequest(elapsedMs, statusCode >= 400);

        if (elapsedMs > 1000)
        {
            _logger.LogWarning("慢接口告�? {Endpoint} 耗时 {ElapsedMs}ms", endpoint, elapsedMs);
        }

        if (metrics.ErrorRate > 0.1)
        {
            _logger.LogError("接口错误率告�? {Endpoint} 错误�?{ErrorRate:P}", endpoint, metrics.ErrorRate);
        }
    }

    public void TrackSqlExecution(string sql, long elapsedMs)
    {
        _sqlMetrics.Add(new SqlMetric { Sql = sql, ElapsedMs = elapsedMs });
    }

    public PerformanceReport GetReport()
    {
        return new PerformanceReport
        {
            Timestamp = DateTimeOffset.UtcNow,
            RequestMetrics = _requestMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            SqlMetrics = new List<SqlMetric>(_sqlMetrics)
        };
    }
}

public class RequestMetrics
{
    private long _totalRequests;
    private long _totalElapsedMs;
    private long _errorRequests;

    public long TotalRequests => _totalRequests;
    public long AverageMs => _totalRequests == 0 ? 0 : _totalElapsedMs / _totalRequests;
    public double ErrorRate => _totalRequests == 0 ? 0 : (double)_errorRequests / _totalRequests;

    public void AddRequest(long elapsedMs, bool isError)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Add(ref _totalElapsedMs, elapsedMs);
        if (isError) Interlocked.Increment(ref _errorRequests);
    }
}

public class SqlMetric
{
    public string Sql { get; set; } = string.Empty;
    public long ElapsedMs { get; set; }
}

public class PerformanceReport
{
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, RequestMetrics> RequestMetrics { get; set; } = new();
    public List<SqlMetric> SqlMetrics { get; set; } = new();
}
