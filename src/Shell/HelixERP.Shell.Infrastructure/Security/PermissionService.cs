using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.Infrastructure.Security;

public sealed class PermissionService : IPermissionService
{
    private readonly ShellDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public PermissionService(ShellDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<bool> HasAccessAsync(
        string model,
        OperationType operation,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
        {
            return false;
        }

        var normalizedModel = AccessRight.NormalizeModel(model);

        var account = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.IsActive, user.IsSuperUser })
            .SingleOrDefaultAsync(cancellationToken);

        if (account is null || !account.IsActive)
        {
            return false;
        }

        if (account.IsSuperUser)
        {
            return true;
        }

        var groups = await GroupResolver.ResolveForUserAsync(_dbContext, userId, cancellationToken);
        var groupIds = groups.GroupIds.ToList();
        var requiredFlag = ToFlag(operation);

        var rights = await _dbContext.AccessRights
            .AsNoTracking()
            .Where(right => right.Model == normalizedModel && groupIds.Contains(right.GroupId))
            .Select(right => right.Rights)
            .ToListAsync(cancellationToken);

        return rights.Any(rightsValue => (rightsValue & requiredFlag) == requiredFlag);
    }

    public async Task<IReadOnlyCollection<string>> GetGroupsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var groups = await GroupResolver.ResolveForUserAsync(_dbContext, userId, cancellationToken);

        return groups.GroupNames;
    }

    public async Task<IReadOnlyCollection<RecordRuleDefinition>> GetRecordRulesAsync(
        string model,
        OperationType operation,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is not { } userId)
        {
            return [];
        }

        var normalizedModel = AccessRight.NormalizeModel(model);
        var requiredFlag = ToFlag(operation);
        var groups = await GroupResolver.ResolveForUserAsync(_dbContext, userId, cancellationToken);
        var groupIds = groups.GroupIds.ToList();

        var rules = await _dbContext.RecordRules
            .AsNoTracking()
            .Where(rule => rule.Model == normalizedModel && rule.IsActive)
            .Where(rule => rule.GroupId == null || groupIds.Contains(rule.GroupId!.Value))
            .OrderBy(rule => rule.GroupId != null)
            .ThenBy(rule => rule.Name)
            .ToListAsync(cancellationToken);

        return rules
            .Where(rule => (rule.Permissions & requiredFlag) == requiredFlag)
            .Select(rule => new RecordRuleDefinition(
                rule.Id,
                rule.Name,
                rule.Model,
                rule.Domain,
                rule.IsGlobal,
                rule.IsActive,
                rule.CompanyId))
            .ToList();
    }

    private static AccessRightFlags ToFlag(OperationType operation)
        => operation switch
        {
            OperationType.Read => AccessRightFlags.Read,
            OperationType.Write => AccessRightFlags.Write,
            OperationType.Create => AccessRightFlags.Create,
            OperationType.Unlink => AccessRightFlags.Unlink,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unsupported operation.")
        };
}