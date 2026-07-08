using EfCore.Enterprise.Domain.Events;
using EfCore.Enterprise.Shared.DependencyInjection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfCore.Enterprise.Infrastructure.Services;

[Injectable(ServiceLifetime.Singleton, ExposeAs = typeof(IDomainEventBus))]
public class MediatorDomainEventBus : IDomainEventBus
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatorDomainEventBus> _logger;

    public MediatorDomainEventBus(IMediator mediator, ILogger<MediatorDomainEventBus> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("发布领域事件 {EventName}({Id})", domainEvent.EventName, domainEvent.Id);
        await _mediator.Publish(domainEvent, cancellationToken);
    }

    public async Task PublishRangeAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await PublishAsync(domainEvent, cancellationToken);
        }
    }
}