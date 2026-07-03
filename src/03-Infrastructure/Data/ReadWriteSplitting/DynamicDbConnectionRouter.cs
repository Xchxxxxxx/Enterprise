using EfCore.Enterprise.Shared.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Data.ReadWriteSplitting;

public interface IDbConnectionRouter
{
    string GetReadConnectionString();
    string GetWriteConnectionString();
    string GetConnectionStringForType(DbOperationType operationType);
}

public enum DbOperationType
{
    Read,
    Write,
    Transaction
}

[IgnoreInjectable]
public class DynamicDbConnectionRouter : IDbConnectionRouter
{
    private readonly string _writeConnection;
    private readonly List<string> _readConnections;
    private readonly ILogger<DynamicDbConnectionRouter> _logger;
    private readonly object _lock = new();
    private int _currentReadIndex = -1;
    private readonly Dictionary<string, bool> _faultyNodes = new();

    public DynamicDbConnectionRouter(
        string writeConnection,
        List<string> readConnections,
        ILogger<DynamicDbConnectionRouter> logger)
    {
        _writeConnection = writeConnection;
        _readConnections = readConnections;
        _logger = logger;
    }

    public string GetReadConnectionString()
    {
        if (_readConnections.Count == 0) return _writeConnection;

        for (int i = 0; i < _readConnections.Count; i++)
        {
            var connection = GetNextReadConnection();
            if (!_faultyNodes.ContainsKey(connection)) return connection;
        }

        _logger.LogWarning("所有从库故障，自动切换到主库读取");
        return _writeConnection;
    }

    public string GetWriteConnectionString() => _writeConnection;

    public string GetConnectionStringForType(DbOperationType operationType)
    {
        return operationType switch
        {
            DbOperationType.Read => GetReadConnectionString(),
            DbOperationType.Write => GetWriteConnectionString(),
            DbOperationType.Transaction => GetWriteConnectionString(),
            _ => GetWriteConnectionString()
        };
    }

    public void MarkNodeFaulty(string connectionString)
    {
        lock (_lock)
        {
            _faultyNodes[connectionString] = true;
            _logger.LogWarning("数据库节点标记为故障: {Connection}", connectionString);
        }
    }

    public void MarkNodeHealthy(string connectionString)
    {
        lock (_lock)
        {
            _faultyNodes.Remove(connectionString);
            _logger.LogInformation("数据库节点恢复健康: {Connection}", connectionString);
        }
    }

    private string GetNextReadConnection()
    {
        lock (_lock)
        {
            _currentReadIndex = (_currentReadIndex + 1) % _readConnections.Count;
            return _readConnections[_currentReadIndex];
        }
    }
}