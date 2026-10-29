using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Application.Security;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelixERP.Shell.Infrastructure.Security;

public sealed class AuthService : IAuthService
{
    private readonly ShellDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public AuthService(
        ShellDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IClock clock)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<AuthResult?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        string login;

        try
        {
            login = User.NormalizeLogin(request.Login);
        }
        catch (Exception exception) when (exception is ArgumentException or DomainException)
        {
            return null;
        }

        var user = await _dbContext.Users
            .Include(candidate => candidate.Memberships)
            .SingleOrDefaultAsync(candidate => candidate.Login == login, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var now = _clock.UtcNow;
        var groups = await GroupResolver.ResolveForUserAsync(_dbContext, user.Id, cancellationToken);
        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            user.Login,
            user.DisplayName,
            user.IsSuperUser,
            groups.GroupNames,
            now);
        var refreshToken = _tokenService.CreateRefreshToken(now);

        user.RecordLogin(now);
        _dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            refreshToken.Hash,
            refreshToken.ExpiresAtUtc,
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            new AuthUserInfo(user.Id, user.Login, user.DisplayName, user.IsSuperUser, groups.GroupNames),
            new AuthTokens(
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                refreshToken.Value,
                refreshToken.ExpiresAtUtc));
    }

    public async Task<AuthResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var now = _clock.UtcNow;
        var hash = _tokenService.HashRefreshToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (storedToken is null || !storedToken.IsActiveAt(now))
        {
            return null;
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return null;
        }

        var groups = await GroupResolver.ResolveForUserAsync(_dbContext, user.Id, cancellationToken);
        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            user.Login,
            user.DisplayName,
            user.IsSuperUser,
            groups.GroupNames,
            now);
        var newRefreshToken = _tokenService.CreateRefreshToken(now);

        storedToken.Revoke(now, newRefreshToken.Hash);
        _dbContext.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            newRefreshToken.Hash,
            newRefreshToken.ExpiresAtUtc,
            now));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            new AuthUserInfo(user.Id, user.Login, user.DisplayName, user.IsSuperUser, groups.GroupNames),
            new AuthTokens(
                accessToken.Value,
                accessToken.ExpiresAtUtc,
                newRefreshToken.Value,
                newRefreshToken.ExpiresAtUtc));
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var now = _clock.UtcNow;
        var hash = _tokenService.HashRefreshToken(refreshToken);

        var storedToken = await _dbContext.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (storedToken is null || !storedToken.IsActiveAt(now))
        {
            return;
        }

        storedToken.Revoke(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}