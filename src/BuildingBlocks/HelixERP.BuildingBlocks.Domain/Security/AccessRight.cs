using System.Text.RegularExpressions;
using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Security;

public sealed class AccessRight : AggregateRoot<Guid>
{
    private static readonly Regex ModelPattern = new(
        "^[a-z][a-z0-9_]*\\.[a-z][a-z0-9_]*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private AccessRight()
    {
    }

    private AccessRight(
        Guid id,
        Guid groupId,
        string model,
        string? moduleKey,
        string? description,
        AccessRightFlags rights,
        DateTime createdAtUtc)
        : base(id)
    {
        GroupId = groupId;
        Model = model;
        ModuleKey = moduleKey;
        Description = description;
        Rights = rights;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid GroupId { get; private set; }

    public string Model { get; private set; } = string.Empty;

    public string? ModuleKey { get; private set; }

    public string? Description { get; private set; }

    public AccessRightFlags Rights { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static AccessRight Create(
        Guid groupId,
        string model,
        AccessRightFlags rights,
        string? moduleKey = null,
        string? description = null,
        DateTime? createdAtUtc = null)
    {
        Guard.Against(groupId == Guid.Empty, "Group id must not be empty.");

        var createdAt = createdAtUtc ?? DateTime.UtcNow;
        EnsureUtc(createdAt, nameof(createdAtUtc));
        Guard.Against(rights == AccessRightFlags.None, "At least one access right must be granted.");
        Guard.Against(
            (rights & ~AccessRightFlags.All) != AccessRightFlags.None,
            "Unknown access right flags were supplied.");

        return new AccessRight(
            Guid.NewGuid(),
            groupId,
            NormalizeModel(model),
            NormalizeModuleKey(moduleKey),
            description?.Trim(),
            rights,
            createdAt);
    }

    public void UpdateRights(AccessRightFlags rights, DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        Guard.Against(rights == AccessRightFlags.None, "At least one access right must be granted.");
        Guard.Against(
            (rights & ~AccessRightFlags.All) != AccessRightFlags.None,
            "Unknown access right flags were supplied.");

        Rights = rights;
        UpdatedAtUtc = updatedAtUtc;
    }

    public bool Allows(AccessRightFlags flag)
        => flag != AccessRightFlags.None && (Rights & flag) == flag;

    public static string NormalizeModel(string model)
    {
        var value = Guard.NotNullOrWhiteSpace(model, nameof(model)).Trim().ToLowerInvariant();

        Guard.Against(
            !ModelPattern.IsMatch(value),
            $"Model '{model}' is invalid. Expected a model name such as 'sales.order'.");

        return value;
    }

    private static string? NormalizeModuleKey(string? moduleKey)
        => string.IsNullOrWhiteSpace(moduleKey) ? null : moduleKey.Trim().ToLowerInvariant();

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}