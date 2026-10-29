using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HelixERP.Shell.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HelixERP.Shell.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    public const string GroupClaim = "helix:group";
    public const string SuperUserClaim = "helix:superuser";

    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _options.Validate();
    }

    public AccessToken CreateAccessToken(
        Guid userId,
        string login,
        string displayName,
        bool isSuperUser,
        IReadOnlyCollection<string> groups,
        DateTime issuedAtUtc)
    {
        if (issuedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Token issue time must be UTC.", nameof(issuedAtUtc));
        }

        var expiresAt = issuedAtUtc.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, login),
            new("display_name", displayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(SuperUserClaim, isSuperUser ? "true" : "false", ClaimValueTypes.Boolean)
        };

        claims.AddRange(groups.Select(group => new Claim(GroupClaim, group)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAtUtc,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public RefreshTokenValue CreateRefreshToken(DateTime issuedAtUtc)
    {
        if (issuedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Token issue time must be UTC.", nameof(issuedAtUtc));
        }

        var rawValue = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = issuedAtUtc.AddDays(_options.RefreshTokenDays);

        return new RefreshTokenValue(rawValue, HashRefreshToken(rawValue), expiresAt);
    }

    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}