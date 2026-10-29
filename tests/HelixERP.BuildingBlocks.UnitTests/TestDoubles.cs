using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Domain.Common;
using HelixERP.BuildingBlocks.Domain.Events;

namespace HelixERP.BuildingBlocks.UnitTests;

internal sealed class TestEntity : Entity<Guid>
{
    public TestEntity(Guid id)
        : base(id)
    {
    }

    private TestEntity()
    {
    }
}

internal sealed class OtherTestEntity : Entity<Guid>
{
    public OtherTestEntity(Guid id)
        : base(id)
    {
    }
}

internal sealed class TransientTestEntity : Entity<Guid>
{
}

internal sealed class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate(Guid id)
        : base(id)
    {
    }

    public TestAggregate()
    {
    }

    public string Name { get; set; } = string.Empty;

    public void AddEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
}

internal sealed record TestDomainEvent(Guid Id, DateTime OccurredOnUtc) : IDomainEvent;

internal sealed class RecordingDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> Events { get; } = [];

    public int CallCount { get; private set; }

    public Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        Events.AddRange(domainEvents);
        return Task.CompletedTask;
    }
}

internal sealed class TestEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public List<TestDomainEvent> Handled { get; } = [];

    public CancellationToken LastToken { get; private set; }

    public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Handled.Add(domainEvent);
        LastToken = cancellationToken;
        return Task.CompletedTask;
    }
}

internal sealed class SecondTestEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public List<TestDomainEvent> Handled { get; } = [];

    public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Handled.Add(domainEvent);
        return Task.CompletedTask;
    }
}