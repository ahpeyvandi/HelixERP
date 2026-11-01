using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Security;

/// <summary>
/// Maps an external identity (OIDC issuer + subject) to an internal HelixERP user.
/// Used for IdentityServer / OpenID Connect (and later LDAP) external logins.
/// </summary>
public sealed class ExternalLogin : Entity<Guid>
{
    private ExternalLogin()
    {
    }

    private ExternalLogin(
        Guid id,
        Guid userId,
        string provider,
        string subject,
        DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        Provider = NormalizeProvider(provider);
        Subject = NormalizeSubject(subject);
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    /// <summary>
    /// Stable provider key, e.g. "identityserver". Used together with <see cref="Subject"/>.
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// The provider-specific subject identifier (OIDC "sub" claim).
    /// </summary>
    public string Subject { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static ExternalLogin Create(
        Guid userId,
        string provider,
        string subject,
        DateTime createdAtUtc)
    {
        Guard.Against(userId == Guid.Empty, "User id must not be empty.");
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        return new ExternalLogin(Guid.NewGuid(), userId, provider, subject, createdAtUtc);
    }

    public static string NormalizeProvider(string provider)
        => Guard.NotNullOrWhiteSpace(provider, nameof(provider)).Trim().ToLowerInvariant();

    public static string NormalizeSubject(string subject)
        => Guard.NotNullOrWhiteSpace(subject, nameof(subject)).Trim();

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}
