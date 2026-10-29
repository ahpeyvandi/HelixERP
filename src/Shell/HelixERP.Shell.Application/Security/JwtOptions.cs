namespace HelixERP.Shell.Application.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumSigningKeyLength = 32;

    public string Issuer { get; set; } = "HelixERP.Shell";

    public string Audience { get; set; } = "HelixERP.Clients";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;

    public int RefreshTokenDays { get; set; } = 14;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(SigningKey ?? string.Empty) < MinimumSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumSigningKeyLength} bytes. Configure a strong secret.");
        }

        if (AccessTokenMinutes is < 1 or > 1440)
        {
            throw new InvalidOperationException("Jwt:AccessTokenMinutes must be between 1 and 1440.");
        }

        if (RefreshTokenDays is < 1 or > 365)
        {
            throw new InvalidOperationException("Jwt:RefreshTokenDays must be between 1 and 365.");
        }
    }
}