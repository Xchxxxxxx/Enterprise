using EfCore.Enterprise.Shared.DependencyInjection;
using System.Diagnostics.Metrics;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IPerformanceMetrics
{
    void RecordRequest(string path, long milliseconds);
    void RecordQuery(string operation, long milliseconds);
    void RecordCacheHit(string cacheType);
    void RecordCacheMiss(string cacheType);
    void IncrementErrorCount(string source, string errorType);
    void SetGauge(string name, double value);
    Meter Meter { get; }
}

public class PerformanceMetrics : IPerformanceMetrics
{
    public Meter Meter { get; }

    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _queryDuration;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _errorCount;

    public PerformanceMetrics()
    {
        Meter = new Meter("EfCore.Enterprise", "1.0.0");

        _requestDuration = Meter.CreateHistogram<double>(
            "efcore.request.duration",
            "ms",
            "Request processing duration");

        _queryDuration = Meter.CreateHistogram<double>(
            "efcore.query.duration",
            "ms",
            "Database query duration");

        _cacheHits = Meter.CreateCounter<long>(
            "efcore.cache.hits",
            "count",
            "Cache hit count");

        _cacheMisses = Meter.CreateCounter<long>(
            "efcore.cache.misses",
            "count",
            "Cache miss count");

        _errorCount = Meter.CreateCounter<long>(
            "efcore.errors.count",
            "count",
            "Error count by type");
    }

    public void RecordRequest(string path, long milliseconds)
    {
        _requestDuration.Record(milliseconds, new KeyValuePair<string, object?>("path", path));
    }

    public void RecordQuery(string operation, long milliseconds)
    {
        _queryDuration.Record(milliseconds, new KeyValuePair<string, object?>("operation", operation));
    }

    public void RecordCacheHit(string cacheType)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("type", cacheType));
    }

    public void RecordCacheMiss(string cacheType)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("type", cacheType));
    }

    public void IncrementErrorCount(string source, string errorType)
    {
        _errorCount.Add(1,
            new KeyValuePair<string, object?>("source", source),
            new KeyValuePair<string, object?>("error_type", errorType));
    }

    public void SetGauge(string name, double value)
    {
        var observableGauge = Meter.CreateObservableGauge($"efcore.{name}", () => value);
    }
}