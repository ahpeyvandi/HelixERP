namespace HelixERP.BuildingBlocks.Application.Security;

public interface IPermissionService
{
    Task<bool> HasAccessAsync(
        string model,
        OperationType operation,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RecordRuleDefinition>> GetRecordRulesAsync(
        string model,
        OperationType operation,
        CancellationToken cancellationToken = default);
}