using FluentAssertions;
using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Application.Persistence;
using HelixERP.BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class HelixDbContextTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    private sealed class TestDbContext : HelixDbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options, IDomainEventDispatcher? dispatcher)
            : base(options, dispatcher)
        {
        }

        public DbSet<TestAggregate> Aggregates => Set<TestAggregate>();
    }

    private static TestDbContext CreateContext(IDomainEventDispatcher? dispatcher)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(options, dispatcher);
    }

    [Fact]
    public void Context_is_a_unit_of_work()
    {
        using var context = CreateContext(null);

        context.Should().BeAssignableTo<IUnitOfWork>();
    }

    [Fact]
    public void Domain_events_are_not_mapped_as_a_property()
    {
        using var context = CreateContext(null);

        var entityType = context.Model.FindEntityType(typeof(TestAggregate));

        entityType.Should().NotBeNull();
        entityType!.GetProperties().Should().NotContain(property => property.Name == "DomainEvents");
    }

    [Fact]
    public async Task SaveChanges_dispatches_and_clears_domain_events()
    {
        var dispatcher = new RecordingDispatcher();
        await using var context = CreateContext(dispatcher);
        var aggregate = new TestAggregate(Guid.NewGuid()) { Name = "first" };
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), Now);
        aggregate.AddEvent(domainEvent);
        context.Aggregates.Add(aggregate);

        await context.SaveChangesAsync();

        dispatcher.CallCount.Should().Be(1);
        dispatcher.Events.Should().ContainSingle().Which.Should().Be(domainEvent);
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_without_events_does_not_call_dispatcher()
    {
        var dispatcher = new RecordingDispatcher();
        await using var context = CreateContext(dispatcher);
        context.Aggregates.Add(new TestAggregate(Guid.NewGuid()) { Name = "no-events" });

        await context.SaveChangesAsync();

        dispatcher.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task SaveChanges_dispatches_events_from_all_tracked_aggregates()
    {
        var dispatcher = new RecordingDispatcher();
        await using var context = CreateContext(dispatcher);
        var first = new TestAggregate(Guid.NewGuid()) { Name = "first" };
        var second = new TestAggregate(Guid.NewGuid()) { Name = "second" };
        first.AddEvent(new TestDomainEvent(Guid.NewGuid(), Now));
        second.AddEvent(new TestDomainEvent(Guid.NewGuid(), Now));
        context.Aggregates.AddRange(first, second);

        await context.SaveChangesAsync();

        dispatcher.Events.Should().HaveCount(2);
        first.DomainEvents.Should().BeEmpty();
        second.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChanges_without_dispatcher_still_clears_events()
    {
        await using var context = CreateContext(null);
        var aggregate = new TestAggregate(Guid.NewGuid()) { Name = "no-dispatcher" };
        aggregate.AddEvent(new TestDomainEvent(Guid.NewGuid(), Now));
        context.Aggregates.Add(aggregate);

        await context.SaveChangesAsync();

        aggregate.DomainEvents.Should().BeEmpty();
    }
}