using System.Reflection;
using System.Runtime.ExceptionServices;
using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace HelixERP.BuildingBlocks.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
        => _serviceProvider = serviceProvider;

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));

            if (handleMethod is null)
            {
                continue;
            }

            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                Task task;

                try
                {
                    task = (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                }
                catch (TargetInvocationException exception) when (exception.InnerException is not null)
                {
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    throw;
                }

                await task.ConfigureAwait(false);
            }
        }
    }
}