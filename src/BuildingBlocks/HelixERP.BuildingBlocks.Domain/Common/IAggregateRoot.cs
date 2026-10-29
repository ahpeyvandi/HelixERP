using HelixERP.BuildingBlocks.Domain.Events;

namespace HelixERP.BuildingBlocks.Domain.Common;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}