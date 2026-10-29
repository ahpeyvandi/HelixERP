namespace HelixERP.BuildingBlocks.Shared.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? CompanyId { get; }

    string? UserName { get; }

    string? DisplayName { get; }

    bool IsSuperUser { get; }
}