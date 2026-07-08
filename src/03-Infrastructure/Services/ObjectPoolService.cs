using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IObjectPoolService
{
    T Rent<T>() where T : class, new();
    void Return<T>(T obj) where T : class, new();
}

public class ObjectPoolService : IObjectPoolService
{
    private readonly ConcurrentDictionary<Type, object> _pools = new();
    private readonly ILogger<ObjectPoolService> _logger;

    public ObjectPoolService(ILogger<ObjectPoolService> logger)
    {
        _logger = logger;
    }

    public T Rent<T>() where T : class, new()
    {
        var pool = GetPool<T>();
        return pool.Get();
    }

    public void Return<T>(T obj) where T : class, new()
    {
        var pool = GetPool<T>();
        pool.Return(obj);
    }

    private ObjectPool<T> GetPool<T>() where T : class, new()
    {
        return (ObjectPool<T>)_pools.GetOrAdd(typeof(T), _ =>
        {
            var policy = new DefaultPooledObjectPolicy<T>();
            return new DefaultObjectPool<T>(policy, Environment.ProcessorCount * 2);
        });
    }
}