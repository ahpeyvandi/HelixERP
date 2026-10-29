using HelixERP.BuildingBlocks.Application.Events;
using HelixERP.BuildingBlocks.Application.Persistence;
using HelixERP.BuildingBlocks.Domain.Common;
using HelixERP.BuildingBlocks.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.BuildingBlocks.Infrastructure.Persistence;

public abstract class HelixDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    protected HelixDbContext(DbContextOptions options)
        : base(options)
    {
    }

    protected HelixDbContext(
        DbContextOptions options,
        IDomainEventDispatcher? domainEventDispatcher)
        : base(options)
        => _domainEventDispatcher = domainEventDispatcher;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        configurationBuilder.Properties<decimal>().HavePrecision(18, 4);

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (typeof(IAggregateRoot).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IAggregateRoot.DomainEvents));
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker.Entries<IAggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .Distinct()
            .ToArray();

        var affectedRows = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (aggregatesWithEvents.Length == 0)
        {
            return affectedRows;
        }

        var domainEvents = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToArray();

        if (_domainEventDispatcher is not null)
        {
            await _domainEventDispatcher
                .DispatchAsync(domainEvents, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        return affectedRows;
    }
}