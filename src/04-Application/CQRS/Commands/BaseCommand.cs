using MediatR;

namespace EfCore.Enterprise.Application.CQRS.Commands;

public interface ICommand : IRequest
{
}

public interface ICommand<TResponse> : IRequest<TResponse>
{
}

public abstract class BaseCommand : ICommand
{
    public Guid CommandId { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}

public abstract class BaseCommand<TResponse> : ICommand<TResponse>
{
    public Guid CommandId { get; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}