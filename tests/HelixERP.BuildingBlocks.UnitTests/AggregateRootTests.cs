using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Events;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class AggregateRootTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void New_aggregate_has_no_domain_events()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raised_events_are_exposed_in_order()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var first = new TestDomainEvent(Guid.NewGuid(), Now);
        var second = new TestDomainEvent(Guid.NewGuid(), Now.AddMinutes(1));

        aggregate.AddEvent(first);
        aggregate.AddEvent(second);

        aggregate.DomainEvents.Should().ContainInOrder(first, second);
    }

    [Fact]
    public void ClearDomainEvents_removes_all_events()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.AddEvent(new TestDomainEvent(Guid.NewGuid(), Now));

        aggregate.ClearDomainEvents();

        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Raising_null_event_throws()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());

        var act = () => aggregate.AddEvent(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Domain_events_collection_is_read_only()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.AddEvent(new TestDomainEvent(Guid.NewGuid(), Now));

        var collection = aggregate.DomainEvents;

        collection.Should().BeAssignableTo<IReadOnlyCollection<IDomainEvent>>();
    }
}