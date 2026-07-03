using EfCore.Enterprise.Domain.Events;

namespace EfCore.Enterprise.Domain.Entities;

public abstract class BaseEntity<TKey>
{
    public TKey Id { get; private set; } = default!;

    private List<DomainEvent>? _domainEvents;

    public IReadOnlyCollection<DomainEvent> DomainEvents
        => (_domainEvents?.AsReadOnly()) ?? (IReadOnlyCollection<DomainEvent>)Array.Empty<DomainEvent>();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents ??= new List<DomainEvent>();
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents?.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }
}

public abstract class BaseEntity : BaseEntity<long>
{
}