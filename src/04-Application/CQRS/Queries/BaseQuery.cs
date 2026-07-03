using MediatR;

namespace EfCore.Enterprise.Application.CQRS.Queries;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

public abstract class BaseQuery<TResponse> : IQuery<TResponse>
{
    public Guid QueryId { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}