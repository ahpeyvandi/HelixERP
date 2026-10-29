using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Security;

public sealed class UserGroup : Entity<Guid>
{
    private UserGroup()
    {
    }

    internal UserGroup(Guid id, Guid userId, Guid groupId, DateTime assignedAtUtc)
        : base(id)
    {
        UserId = userId;
        GroupId = groupId;
        AssignedAtUtc = assignedAtUtc;
    }

    public Guid UserId { get; private set; }

    public Guid GroupId { get; private set; }

    public DateTime AssignedAtUtc { get; private set; }
}