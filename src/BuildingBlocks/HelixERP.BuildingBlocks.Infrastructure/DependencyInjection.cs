using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Infrastructure.Events;
using HelixERP.BuildingBlocks.Infrastructure.Time;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HelixERP.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}