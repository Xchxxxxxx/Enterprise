using EfCore.Enterprise.Domain.Events;

namespace EfCore.Enterprise.Domain.Events;

public interface IDomainEventBus
{
    Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task PublishRangeAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}