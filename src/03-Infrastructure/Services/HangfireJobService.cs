using EfCore.Enterprise.Shared.DependencyInjection;
using System.Linq.Expressions;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace EfCore.Enterprise.Infrastructure.Services;

public interface IBackgroundJobService
{
    string Enqueue(Expression<Action> methodCall);
    string Enqueue(Expression<Func<Task>> methodCall);
    string Schedule(Expression<Action> methodCall, TimeSpan delay);
    string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay);
    void AddRecurring(string jobId, Expression<Action> methodCall, string cronExpression);
    void AddRecurring(string jobId, Expression<Func<Task>> methodCall, string cronExpression);
    bool Delete(string jobId);
    bool Requeue(string jobId);
}

[Injectable(ServiceLifetime.Scoped, ExposeAs = typeof(IBackgroundJobService))]
public class HangfireJobService : IBackgroundJobService
{
    private readonly ILogger<HangfireJobService> _logger;

    public HangfireJobService(ILogger<HangfireJobService> logger)
    {
        _logger = logger;
    }

    public string Enqueue(Expression<Action> methodCall)
    {
        var jobId = BackgroundJob.Enqueue(methodCall);
        _logger.LogInformation("任务已入�? {JobId}", jobId);
        return jobId;
    }

    public string Enqueue(Expression<Func<Task>> methodCall)
    {
        var jobId = BackgroundJob.Enqueue(methodCall);
        _logger.LogInformation("异步任务已入�? {JobId}", jobId);
        return jobId;
    }

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
    {
        var jobId = BackgroundJob.Schedule(methodCall, delay);
        _logger.LogInformation("延时任务已调�? {JobId}, 延时: {Delay}", jobId, delay);
        return jobId;
    }

    public string Schedule(Expression<Func<Task>> methodCall, TimeSpan delay)
    {
        var jobId = BackgroundJob.Schedule(methodCall, delay);
        _logger.LogInformation("异步延时任务已调�? {JobId}, 延时: {Delay}", jobId, delay);
        return jobId;
    }

    public void AddRecurring(string jobId, Expression<Action> methodCall, string cronExpression)
    {
        RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
        _logger.LogInformation("周期任务已注�? {JobId}, Cron: {Cron}", jobId, cronExpression);
    }

    public void AddRecurring(string jobId, Expression<Func<Task>> methodCall, string cronExpression)
    {
        RecurringJob.AddOrUpdate(jobId, methodCall, cronExpression);
        _logger.LogInformation("异步周期任务已注�? {JobId}, Cron: {Cron}", jobId, cronExpression);
    }

    public bool Delete(string jobId)
    {
        var result = BackgroundJob.Delete(jobId);
        _logger.LogInformation("任务已删�? {JobId}, 结果: {Result}", jobId, result);
        return result;
    }

    public bool Requeue(string jobId)
    {
        var result = BackgroundJob.Requeue(jobId);
        _logger.LogInformation("任务已重新入�? {JobId}, 结果: {Result}", jobId, result);
        return result;
    }
}