using System.Text.RegularExpressions;
using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Security;

public sealed class Group : AggregateRoot<Guid>
{
    private static readonly Regex NamePattern = new(
        "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Group()
    {
    }

    private Group(
        Guid id,
        string name,
        string displayName,
        string category,
        Guid? parentId,
        bool isSystem,
        DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        DisplayName = displayName;
        Category = category;
        ParentId = parentId;
        IsSystem = isSystem;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public Guid? ParentId { get; private set; }

    public bool IsSystem { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Group Create(
        string name,
        string displayName,
        string category,
        Guid? parentId = null,
        bool isSystem = false,
        DateTime? createdAtUtc = null)
    {
        var normalizedName = NormalizeName(name);
        var displayNameValue = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim();
        var categoryValue = Guard.NotNullOrWhiteSpace(category, nameof(category)).Trim();
        var createdAt = createdAtUtc ?? DateTime.UtcNow;

        EnsureUtc(createdAt, nameof(createdAtUtc));

        var group = new Group(
            Guid.NewGuid(),
            normalizedName,
            displayNameValue,
            categoryValue,
            parentId,
            isSystem,
            createdAt);

        group.GuardParent(parentId);

        return group;
    }

    public void SetParent(Guid? parentId)
    {
        GuardParent(parentId);
        ParentId = parentId;
    }

    public void Rename(string displayName)
    {
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim();
    }

    public static string NormalizeName(string name)
    {
        var value = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim().ToLowerInvariant();

        Guard.Against(
            !NamePattern.IsMatch(value),
            $"Group name '{name}' is invalid. Expected a full external id such as 'base.group_system'.");

        return value;
    }

    private void GuardParent(Guid? parentId)
    {
        Guard.Against(parentId == Id, "A group cannot be its own parent.");
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}