using System.Text.Json;
using HelixERP.BuildingBlocks.Domain.Common;
using HelixERP.BuildingBlocks.Domain.Exceptions;

namespace HelixERP.BuildingBlocks.Domain.Security;

public sealed class RecordRule : AggregateRoot<Guid>
{
    private RecordRule()
    {
    }

    private RecordRule(
        Guid id,
        string name,
        string model,
        string domain,
        Guid? groupId,
        AccessRightFlags permissions,
        Guid? companyId,
        DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        Model = model;
        Domain = domain;
        GroupId = groupId;
        Permissions = permissions;
        CompanyId = companyId;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public string Name { get; private set; } = string.Empty;

    public string Model { get; private set; } = string.Empty;

    public string Domain { get; private set; } = "[]";

    public Guid? GroupId { get; private set; }

    public bool IsGlobal => GroupId is null;

    public AccessRightFlags Permissions { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? CompanyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public static RecordRule Create(
        string name,
        string model,
        string domain,
        Guid? groupId,
        AccessRightFlags permissions,
        Guid? companyId = null,
        DateTime? createdAtUtc = null)
    {
        var nameValue = Guard.NotNullOrWhiteSpace(name, nameof(name)).Trim();
        var domainValue = NormalizeDomain(domain);
        var createdAt = createdAtUtc ?? DateTime.UtcNow;

        EnsureUtc(createdAt, nameof(createdAtUtc));
        EnsurePermissions(permissions);

        return new RecordRule(
            Guid.NewGuid(),
            nameValue,
            AccessRight.NormalizeModel(model),
            domainValue,
            groupId,
            permissions,
            companyId,
            createdAt);
    }

    public void UpdateDomain(string domain, DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        Domain = NormalizeDomain(domain);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetActive(bool isActive, DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        IsActive = isActive;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static string NormalizeDomain(string domain)
    {
        var value = Guard.NotNullOrWhiteSpace(domain, nameof(domain)).Trim();

        try
        {
            using var document = JsonDocument.Parse(value);

            Guard.Against(
                document.RootElement.ValueKind != JsonValueKind.Array,
                "A record rule domain must be a JSON array, e.g. [[\"company_id\", \"=\", 1]].");
        }
        catch (JsonException exception)
        {
            throw new DomainException("A record rule domain must be valid JSON.", exception);
        }

        return value;
    }

    private static void EnsurePermissions(AccessRightFlags permissions)
    {
        Guard.Against(permissions == AccessRightFlags.None, "A record rule must apply to at least one operation.");
        Guard.Against(
            (permissions & ~AccessRightFlags.All) != AccessRightFlags.None,
            "Unknown record rule permission flags were supplied.");
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}