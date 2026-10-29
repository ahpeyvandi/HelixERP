using HelixERP.BuildingBlocks.Domain.Events;

namespace HelixERP.BuildingBlocks.Domain.Platform.Events;

public sealed record ModuleRegisteredDomainEvent(
    Guid ModuleRegistrationId,
    string ModuleKey,
    DateTime OccurredOnUtc) : IDomainEvent;