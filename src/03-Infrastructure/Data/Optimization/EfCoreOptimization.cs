using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data.Optimization;

[Injectable(ServiceLifetime.Singleton)]
public class QueryPrecompilationCache
{
    private readonly ConcurrentDictionary<string, object> _compiledQueries = new();
    private readonly ILogger<QueryPrecompilationCache> _logger;

    public QueryPrecompilationCache(ILogger<QueryPrecompilationCache> logger)
    {
        _logger = logger;
    }

    public Func<TContext, IEnumerable<TResult>> GetOrAddCompiledQuery<TContext, TResult>(
        string cacheKey,
        Func<TContext, IEnumerable<TResult>> queryFactory)
        where TContext : DbContext
    {
        if (_compiledQueries.TryGetValue(cacheKey, out var cached))
        {
            _logger.LogDebug("命中预编译查询缓存: {Key}", cacheKey);
            return (Func<TContext, IEnumerable<TResult>>)cached;
        }

        _compiledQueries[cacheKey] = queryFactory;
        _logger.LogInformation("预编译查询已缓存: {Key}", cacheKey);
        return queryFactory;
    }

    public void InvalidateCache()
    {
        _compiledQueries.Clear();
        _logger.LogInformation("预编译查询缓存已清空");
    }
}

public class ModelCacheManager
{
    private readonly string _cachePath;
    private readonly ILogger<ModelCacheManager> _logger;

    public ModelCacheManager(string cachePath, ILogger<ModelCacheManager> logger)
    {
        _cachePath = cachePath;
        _logger = logger;
    }

    public bool TryLoadModelCache(out byte[]? modelCache)
    {
        var cacheFile = Path.Combine(_cachePath, "ef-model-cache.bin");
        if (File.Exists(cacheFile))
        {
            modelCache = File.ReadAllBytes(cacheFile);
            _logger.LogInformation("EF模型缓存已加载: {Path}", cacheFile);
            return true;
        }

        modelCache = null;
        return false;
    }

    public void SaveModelCache(byte[] modelCache)
    {
        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
        }

        var cacheFile = Path.Combine(_cachePath, "ef-model-cache.bin");
        File.WriteAllBytes(cacheFile, modelCache);
        _logger.LogInformation("EF模型缓存已保存: {Path}", cacheFile);
    }
}