using EfCore.Enterprise.Domain.Entities;
using EfCore.Enterprise.Domain.Interfaces;
using EfCore.Enterprise.Shared.DependencyInjection;
using EfCore.Enterprise.Shared.Extends;
using EfCore.Enterprise.Shared.Models;
using System.Linq.Expressions;

namespace EfCore.Enterprise.Infrastructure.Services;

[Injectable(ServiceLifetime.Scoped)]
public class OpLogService
{
    private readonly ISuperRepository<OpLog> _repository;

    public OpLogService(ISuperRepository<OpLog> repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(string module, string action, string? detail, string? operatorName, long? operatorId, string? ip, string? userAgent, long? elapsed, bool success)
    {
        var log = new OpLog(module, action, detail, operatorName, operatorId, ip, userAgent, elapsed, success);
        await _repository.AddAsync(log);
        await _repository.SaveChangesAsync();
    }

    public async Task<PagedResult<OpLog>> GetPageAsync(
        PagedRequest request,
        string? module = null,
        string? action = null,
        long? operatorId = null,
        DateTimeOffset? startTime = null,
        DateTimeOffset? endTime = null)
    {
        Expression<Func<OpLog, bool>> predicate = _ => true;

        if (!string.IsNullOrWhiteSpace(module))
            predicate = predicate.AndAlso(e => e.Module.Contains(module));
        if (!string.IsNullOrWhiteSpace(action))
            predicate = predicate.AndAlso(e => e.Action == action);
        if (operatorId.HasValue)
            predicate = predicate.AndAlso(e => e.OperatorId == operatorId);
        if (startTime.HasValue)
            predicate = predicate.AndAlso(e => e.OperateTime >= startTime);
        if (endTime.HasValue)
            predicate = predicate.AndAlso(e => e.OperateTime <= endTime);

        return await _repository.GetPagedAsync(predicate, request);
    }
}