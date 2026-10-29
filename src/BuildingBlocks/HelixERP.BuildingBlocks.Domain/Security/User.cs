using System.Net.Mail;
using System.Text.RegularExpressions;
using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Security;

public sealed class User : AggregateRoot<Guid>
{
    private static readonly Regex LoginPattern = new(
        "^[a-z0-9][a-z0-9._-]{2,63}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly List<UserGroup> _memberships = [];

    private User()
    {
    }

    private User(
        Guid id,
        string login,
        string email,
        string displayName,
        string passwordHash,
        bool isSuperUser,
        Guid? companyId,
        DateTime createdAtUtc)
        : base(id)
    {
        Login = login;
        Email = email;
        DisplayName = displayName;
        PasswordHash = passwordHash;
        IsSuperUser = isSuperUser;
        IsActive = true;
        CompanyId = companyId;
        CreatedAtUtc = createdAtUtc;
    }

    public string Login { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsSuperUser { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? CompanyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public ICollection<UserGroup> Memberships => _memberships;

    public static User Create(
        string login,
        string email,
        string displayName,
        string passwordHash,
        bool isSuperUser = false,
        Guid? companyId = null,
        DateTime? createdAtUtc = null)
    {
        var normalizedLogin = NormalizeLogin(login);
        var emailValue = NormalizeEmail(email);
        var displayNameValue = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim();
        var passwordHashValue = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash));
        var createdAt = createdAtUtc ?? DateTime.UtcNow;

        EnsureUtc(createdAt, nameof(createdAtUtc));

        return new User(
            Guid.NewGuid(),
            normalizedLogin,
            emailValue,
            displayNameValue,
            passwordHashValue,
            isSuperUser,
            companyId,
            createdAt);
    }

    public void AssignToGroup(Guid groupId, DateTime assignedAtUtc)
    {
        Guard.Against(groupId == Guid.Empty, "Group id must not be empty.");
        EnsureUtc(assignedAtUtc, nameof(assignedAtUtc));

        if (_memberships.Any(membership => membership.GroupId == groupId))
        {
            return;
        }

        _memberships.Add(new UserGroup(Guid.NewGuid(), Id, groupId, assignedAtUtc));
    }

    public bool RemoveFromGroup(Guid groupId)
        => _memberships.RemoveAll(membership => membership.GroupId == groupId) > 0;

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = Guard.NotNullOrWhiteSpace(passwordHash, nameof(passwordHash));
    }

    public void UpdateProfile(string email, string displayName)
    {
        Email = NormalizeEmail(email);
        DisplayName = Guard.NotNullOrWhiteSpace(displayName, nameof(displayName)).Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void RecordLogin(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));
        LastLoginAtUtc = utcNow;
    }

    public void GrantSuperUser() => IsSuperUser = true;

    public void RevokeSuperUser() => IsSuperUser = false;

    public static string NormalizeLogin(string login)
    {
        var value = Guard.NotNullOrWhiteSpace(login, nameof(login)).Trim().ToLowerInvariant();

        Guard.Against(
            !LoginPattern.IsMatch(value),
            $"Login '{login}' is invalid. Use 3-64 lowercase letters, digits, dots, dashes or underscores.");

        return value;
    }

    private static string NormalizeEmail(string email)
    {
        var value = Guard.NotNullOrWhiteSpace(email, nameof(email)).Trim().ToLowerInvariant();

        Guard.Against(
            !MailAddress.TryCreate(value, out var address) || address.Address != value,
            $"Email '{email}' is invalid.");

        return value;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}