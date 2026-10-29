using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.Infrastructure.Security;

public sealed record EffectiveGroups(
    IReadOnlyCollection<Guid> GroupIds,
    IReadOnlyCollection<string> GroupNames);

public static class GroupResolver
{
    public static async Task<EffectiveGroups> ResolveForUserAsync(
        ShellDbContext dbContext,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var directGroupIds = await dbContext.UserGroups
            .AsNoTracking()
            .Where(membership => membership.UserId == userId)
            .Select(membership => membership.GroupId)
            .ToListAsync(cancellationToken);

        var groups = await dbContext.Groups
            .AsNoTracking()
            .Select(group => new { group.Id, group.ParentId, group.Name })
            .ToListAsync(cancellationToken);

        return Expand(
            directGroupIds,
            groups.ToDictionary(group => group.Id, group => (group.ParentId, group.Name)));
    }

    public static EffectiveGroups Expand(
        IEnumerable<Guid> directGroupIds,
        IReadOnlyDictionary<Guid, (Guid? ParentId, string Name)> groups)
    {
        var ids = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        foreach (var groupId in directGroupIds)
        {
            if (ids.Add(groupId))
            {
                queue.Enqueue(groupId);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!groups.TryGetValue(current, out var group) || group.ParentId is not { } parentId)
            {
                continue;
            }

            if (ids.Add(parentId))
            {
                queue.Enqueue(parentId);
            }
        }

        var names = ids
            .Where(groups.ContainsKey)
            .Select(id => groups[id].Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return new EffectiveGroups(ids, names);
    }
}