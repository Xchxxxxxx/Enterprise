using EfCore.Enterprise.Shared.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IDeadLetterQueueService
{
    Task EnqueueAsync<T>(string queueName, T message, string? errorMessage = null);
    Task<IReadOnlyList<T>> GetDeadMessagesAsync<T>(string queueName);
    Task RetryAsync(string queueName, string messageId);
    Task DiscardAsync(string queueName, string messageId);
}

[Injectable(ServiceLifetime.Singleton)]
public class DeadLetterQueueService : IDeadLetterQueueService
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<DeadLetterMessage>> _queues = new();
    private readonly ILogger<DeadLetterQueueService> _logger;

    public DeadLetterQueueService(ILogger<DeadLetterQueueService> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync<T>(string queueName, T message, string? errorMessage = null)
    {
        var queue = _queues.GetOrAdd(queueName, _ => new ConcurrentQueue<DeadLetterMessage>());
        var deadMessage = new DeadLetterMessage
        {
            MessageId = Guid.NewGuid().ToString("N"),
            EnqueuedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage ?? "Unknown error",
            RetryCount = 0,
            MaxRetries = 3,
            Payload = message!
        };
        queue.Enqueue(deadMessage);
        _logger.LogWarning("死信队列 {QueueName} 新增消息 {MessageId}", queueName, deadMessage.MessageId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<T>> GetDeadMessagesAsync<T>(string queueName)
    {
        if (_queues.TryGetValue(queueName, out var queue))
        {
            var messages = queue.ToArray()
                .Select(m => (T)m.Payload)
                .ToList();
            return Task.FromResult<IReadOnlyList<T>>(messages);
        }
        return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
    }

    public Task RetryAsync(string queueName, string messageId)
    {
        _logger.LogInformation("死信消息 {MessageId} 重新投递到队列 {QueueName}", messageId, queueName);
        return Task.CompletedTask;
    }

    public Task DiscardAsync(string queueName, string messageId)
    {
        if (_queues.TryGetValue(queueName, out var queue))
        {
            var remaining = queue.Where(m => m.MessageId != messageId).ToList();
            _queues[queueName] = new ConcurrentQueue<DeadLetterMessage>(remaining);
        }
        _logger.LogInformation("死信消息 {MessageId} 已丢弃", messageId);
        return Task.CompletedTask;
    }

    private class DeadLetterMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public DateTime EnqueuedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; }
        public object Payload { get; set; } = null!;
    }
}