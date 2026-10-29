using FluentAssertions;
using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Domain.Events;
using HelixERP.BuildingBlocks.Infrastructure.Events;
using Microsoft.Extensions.DependencyInjection;

namespace HelixERP.BuildingBlocks.UnitTests;

public sealed class DomainEventDispatcherTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Utc);

    private sealed record UnrelatedDomainEvent(Guid Id, DateTime OccurredOnUtc) : IDomainEvent;

    private sealed class UnrelatedEventHandler : IDomainEventHandler<UnrelatedDomainEvent>
    {
        public int CallCount { get; private set; }

        public Task HandleAsync(UnrelatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IDomainEventHandler<TestDomainEvent>
    {
        public Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("handler failed");
    }

    [Fact]
    public async Task Dispatches_event_to_registered_handler()
    {
        var handler = new TestEventHandler();
        await using var provider = new ServiceCollection()
            .AddSingleton<IDomainEventHandler<TestDomainEvent>>(handler)
            .BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider);
        var domainEvent = new TestDomainEvent(Guid.NewGuid(), Now);

        await dispatcher.DispatchAsync([domainEvent]);

        handler.Handled.Should().ContainSingle().Which.Should().Be(domainEvent);
    }

    [Fact]
    public async Task Dispatches_to_all_handlers_of_the_same_event()
    {
        var first = new TestEventHandler();
        var second = new SecondTestEventHandler();
        await using var provider = new ServiceCollection()
            .AddSingleton<IDomainEventHandler<TestDomainEvent>>(first)
            .AddSingleton<IDomainEventHandler<TestDomainEvent>>(second)
            .BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider);

        await dispatcher.DispatchAsync([new TestDomainEvent(Guid.NewGuid(), Now)]);

        first.Handled.Should().ContainSingle();
        second.Handled.Should().ContainSingle();
    }

    [Fact]
    public async Task Does_not_invoke_handlers_of_other_event_types()
    {
        var unrelated = new UnrelatedEventHandler();
        await using var provider = new ServiceCollection()
            .AddSingleton<IDomainEventHandler<UnrelatedDomainEvent>>(unrelated)
            .BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider);

        await dispatcher.DispatchAsync([new TestDomainEvent(Guid.NewGuid(), Now)]);

        unrelated.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Passes_cancellation_token_to_handler()
    {
        var handler = new TestEventHandler();
        await using var provider = new ServiceCollection()
            .AddSingleton<IDomainEventHandler<TestDomainEvent>>(handler)
            .BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);
        using var cts = new CancellationTokenSource();

        await dispatcher.DispatchAsync([new TestDomainEvent(Guid.NewGuid(), Now)], cts.Token);

        handler.LastToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task No_handlers_is_a_no_op()
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        var act = async () => await dispatcher.DispatchAsync([new TestDomainEvent(Guid.NewGuid(), Now)]);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handler_exceptions_surface_to_the_caller()
    {
        await using var provider = new ServiceCollection()
            .AddSingleton<IDomainEventHandler<TestDomainEvent>, ThrowingHandler>()
            .BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        var act = async () => await dispatcher.DispatchAsync([new TestDomainEvent(Guid.NewGuid(), Now)]);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failed");
    }
}