namespace HelixERP.Shell.Application.Security;

public sealed record LoginRequest(string Login, string Password);

public sealed record AuthUserInfo(
    Guid Id,
    string Login,
    string DisplayName,
    bool IsSuperUser,
    IReadOnlyCollection<string> Groups);

public sealed record AuthTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

public sealed record AuthResult(AuthUserInfo User, AuthTokens Tokens);

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

public sealed record RefreshTokenValue(string Value, string Hash, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AccessToken CreateAccessToken(
        Guid userId,
        string login,
        string displayName,
        bool isSuperUser,
        IReadOnlyCollection<string> groups,
        DateTime issuedAtUtc);

    RefreshTokenValue CreateRefreshToken(DateTime issuedAtUtc);

    string HashRefreshToken(string refreshToken);
}

public interface IAuthService
{
    Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
}