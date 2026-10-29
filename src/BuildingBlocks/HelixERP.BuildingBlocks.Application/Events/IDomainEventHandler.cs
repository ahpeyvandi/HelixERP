using HelixERP.BuildingBlocks.Domain.Events;

namespace HelixERP.BuildingBlocks.Application.Events;

public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken = default);
}