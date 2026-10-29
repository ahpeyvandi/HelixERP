using HelixERP.BuildingBlocks.Domain.Common;

namespace HelixERP.BuildingBlocks.Domain.Security;

public sealed class RefreshToken : AggregateRoot<Guid>
{
    private RefreshToken()
    {
    }

    private RefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTime expiresAtUtc,
        DateTime createdAtUtc)
    {
        Guard.Against(userId == Guid.Empty, "User id must not be empty.");
        Guard.NotNullOrWhiteSpace(tokenHash, nameof(tokenHash));
        EnsureUtc(expiresAtUtc, nameof(expiresAtUtc));
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));
        Guard.Against(expiresAtUtc <= createdAtUtc, "A refresh token must expire after it is issued.");

        return new RefreshToken(Guid.NewGuid(), userId, tokenHash, expiresAtUtc, createdAtUtc);
    }

    public bool IsActiveAt(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;

    public void Revoke(DateTime revokedAtUtc, string? replacedByTokenHash = null)
    {
        EnsureUtc(revokedAtUtc, nameof(revokedAtUtc));
        Guard.Against(RevokedAtUtc is not null, "The refresh token is already revoked.");

        RevokedAtUtc = revokedAtUtc;
        ReplacedByTokenHash = replacedByTokenHash;
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        Guard.Against(value.Kind != DateTimeKind.Utc, $"{parameterName} must be a UTC timestamp.");
    }
}