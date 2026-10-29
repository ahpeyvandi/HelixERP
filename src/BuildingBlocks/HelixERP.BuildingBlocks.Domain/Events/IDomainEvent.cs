namespace HelixERP.BuildingBlocks.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}