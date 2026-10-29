using HelixERP.BuildingBlocks.Domain.Events;

namespace HelixERP.BuildingBlocks.Application.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}