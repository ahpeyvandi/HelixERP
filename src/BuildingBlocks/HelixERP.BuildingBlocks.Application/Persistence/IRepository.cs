using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Application.Persistence;

public interface IRepository<TEntity, in TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    void Add(TEntity entity);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}