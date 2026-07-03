using EfCore.Enterprise.Shared.DependencyInjection;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IResilienceService
{
    ResiliencePipeline GetRetryPipeline(int retryCount = 3, int retryDelayMs = 1000);
    ResiliencePipeline GetCircuitBreakerPipeline(int exceptionsAllowed = 5, int durationSeconds = 30);
    ResiliencePipeline GetTimeoutPipeline(int seconds = 30);
    ResiliencePipeline GetBulkheadPipeline(int maxParallelization = 10, int maxQueuingActions = 50);
    ResiliencePipeline<T> GetFallbackPipeline<T>(Func<T> fallbackAction);
    ResiliencePipeline GetCombinedPipeline();
}

[Injectable(ServiceLifetime.Singleton)]
public class ResilienceService : IResilienceService
{
    private readonly ILogger<ResilienceService> _logger;

    public ResilienceService(ILogger<ResilienceService> logger)
    {
        _logger = logger;
    }

    public ResiliencePipeline GetRetryPipeline(int retryCount = 3, int retryDelayMs = 1000)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = retryCount,
                Delay = TimeSpan.FromMilliseconds(retryDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning(args.Outcome.Exception,
                        "重试第{RetryAttempt}次, 等待 {Delay}ms",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public ResiliencePipeline GetCircuitBreakerPipeline(int exceptionsAllowed = 5, int durationSeconds = 30)
    {
        return new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = exceptionsAllowed,
                BreakDuration = TimeSpan.FromSeconds(durationSeconds),
                OnOpened = args =>
                {
                    _logger.LogError("熔断器打开, 持续时间: {Duration}s", args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("熔断器已关闭");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("熔断器半开状态");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public ResiliencePipeline GetTimeoutPipeline(int seconds = 30)
    {
        return new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(seconds))
            .Build();
    }

    public ResiliencePipeline GetBulkheadPipeline(int maxParallelization = 10, int maxQueuingActions = 50)
    {
        return new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(maxParallelization, maxQueuingActions)
            .Build();
    }

    public ResiliencePipeline<T> GetFallbackPipeline<T>(Func<T> fallbackAction)
    {
        return new ResiliencePipelineBuilder<T>()
            .AddFallback(new()
            {
                FallbackAction = _ => Outcome.FromResultAsValueTask(fallbackAction()),
                OnFallback = args =>
                {
                    _logger.LogWarning("服务降级: 使用兜底数据");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public ResiliencePipeline GetCombinedPipeline()
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }
}