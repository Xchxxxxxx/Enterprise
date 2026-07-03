using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data.Sharding;

public interface IShardingRouter
{
    string GetShardConnection<T>(T entity, string key) where T : class;
    string GetShardTableName<T>(string baseTableName, string shardKey) where T : class;
}

[IgnoreInjectable]
public class ShardingRouter : IShardingRouter
{
    private readonly Dictionary<string, List<string>> _shardConnections;
    private readonly int _shardCount;
    private readonly ILogger<ShardingRouter> _logger;

    public ShardingRouter(
        Dictionary<string, List<string>> shardConnections,
        int shardCount,
        ILogger<ShardingRouter> logger)
    {
        _shardConnections = shardConnections;
        _shardCount = shardCount;
        _logger = logger;
    }

    public string GetShardConnection<T>(T entity, string key) where T : class
    {
        var shardIndex = ComputeShardIndex(key);
        var entityType = typeof(T).Name;

        if (_shardConnections.TryGetValue(entityType, out var connections))
        {
            var connection = connections[shardIndex % connections.Count];
            _logger.LogDebug("分片路由: {Entity} -> Shard{Index}", entityType, shardIndex);
            return connection;
        }

        return _shardConnections["default"][0];
    }

    public string GetShardTableName<T>(string baseTableName, string shardKey) where T : class
    {
        var shardIndex = ComputeShardIndex(shardKey);
        return $"{baseTableName}_{shardIndex:D4}";
    }

    private int ComputeShardIndex(string key)
    {
        var hash = Math.Abs(key.GetHashCode());
        return hash % _shardCount;
    }
}