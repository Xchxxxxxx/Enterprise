using MediatR;

namespace EfCore.Enterprise.Domain.Events;

public class DomainEvent : INotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public string EventName => GetType().Name;
}