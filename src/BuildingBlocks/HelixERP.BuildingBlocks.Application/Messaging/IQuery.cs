using HelixERP.BuildingBlocks.Shared.Results;

namespace HelixERP.BuildingBlocks.Application.Messaging;

public interface IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}