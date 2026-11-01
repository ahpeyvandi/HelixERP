using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Application.Security;
using HelixERP.Shell.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.Infrastructure.Security;

public sealed class AuthService : IAuthService
{
    private readonly ShellDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;
    private readonly AuthenticationOptions _authenticationOptions;

    public AuthService(
        ShellDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IClock clock,
        IOptions<AuthenticationOptions> authenticationOptions)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
        _authenticationOptions = authenticationOptions.Value;
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

    public async Task<AuthResult?> LoginWithExternalIdentityAsync(
        ExternalLoginAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        if (attempt is null)
        {
            return null;
        }

        var provider = ExternalLogin.NormalizeProvider(attempt.Provider);
        var subject = ExternalLogin.NormalizeSubject(attempt.Subject);
        var now = _clock.UtcNow;

        var user = await _dbContext.Users
            .Include(candidate => candidate.ExternalLogins)
            .SingleOrDefaultAsync(candidate => candidate.ExternalLogins
                .Any(login => login.Provider == provider && login.Subject == subject), cancellationToken);

        if (user is null)
        {
            user = await ProvisionOrLinkExternalUserAsync(attempt, provider, subject, now, cancellationToken);

            if (user is null)
            {
                return null;
            }
        }

        if (!user.IsActive)
        {
            return null;
        }

        await ApplyGroupClaimMappingsAsync(user, attempt.GroupClaims, now, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

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

    private async Task<User?> ProvisionOrLinkExternalUserAsync(
        ExternalLoginAttempt attempt,
        string provider,
        string subject,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var oidc = _authenticationOptions.Oidc;
        var email = NormalizeEmailForExternal(attempt.Email);

        if (!string.IsNullOrWhiteSpace(email))
        {
            var matched = await _dbContext.Users
                .Include(user => user.ExternalLogins)
                .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (matched is not null)
            {
                matched.LinkExternalLogin(ExternalLogin.Create(matched.Id, provider, subject, now));
                return matched;
            }
        }

        if (!oidc.AutoProvisionUsers)
        {
            return null;
        }

        var login = await CreateExternalLoginAsync(attempt.DisplayName ?? email, cancellationToken);
        var user = User.Create(
            login,
            email ?? $"{login}@helix.remote",
            string.IsNullOrWhiteSpace(attempt.DisplayName) ? login : attempt.DisplayName.Trim(),
            _passwordHasher.Hash(Guid.NewGuid().ToString("N")),
            isSuperUser: false,
            companyId: null,
            createdAtUtc: now);

        user.LinkExternalLogin(ExternalLogin.Create(user.Id, provider, subject, now));

        var defaultGroup = await _dbContext.Groups
            .SingleOrDefaultAsync(group => group.Name == oidc.DefaultGroup, cancellationToken);

        if (defaultGroup is null)
        {
            throw new DomainException(
                $"The configured default external group '{oidc.DefaultGroup}' does not exist. " +
                "Seed the standard groups or fix Authentication:Oidc:DefaultGroup.");
        }

        user.AssignToGroup(defaultGroup.Id, now);
        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private async Task<string> CreateExternalLoginAsync(
        string? displayName,
        CancellationToken cancellationToken)
    {
        var prefix = BuildLoginPrefix(displayName) ?? "ext";

        for (var index = 0; ; index++)
        {
            var candidate = index == 0 ? prefix : $"{prefix}.{index}";

            if (candidate.Length is >= 3 and <= 64 &&
                await _dbContext.Users.AllAsync(user => user.Login != candidate, cancellationToken))
            {
                return candidate;
            }
        }
    }

    private static string? BuildLoginPrefix(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        var sanitized = new string(displayName
            .ToLowerInvariant()
            .Where(character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_')
            .ToArray());

        if (sanitized.Length == 0)
        {
            return null;
        }

        return sanitized.Length > 30 ? sanitized[..30] : sanitized;
    }

    private static string? NormalizeEmailForExternal(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmed = email.Trim().ToLowerInvariant();

        return System.Net.Mail.MailAddress.TryCreate(trimmed, out var address) && address.Address == trimmed
            ? trimmed
            : null;
    }

    private async Task ApplyGroupClaimMappingsAsync(
        User user,
        IReadOnlyCollection<string> groupClaims,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var oidc = _authenticationOptions.Oidc;

        if (groupClaims.Count == 0 || oidc.GroupClaimMappings.Count == 0)
        {
            return;
        }

        var claimGroups = groupClaims
            .Where(claim => oidc.GroupClaimMappings.TryGetValue(claim, out _))
            .Select(claim => oidc.GroupClaimMappings[claim])
            .Where(Group.IsValidName)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (claimGroups.Count == 0)
        {
            return;
        }

        var groups = await _dbContext.Groups
            .Where(group => claimGroups.Contains(group.Name))
            .ToListAsync(cancellationToken);

        foreach (var group in groups)
        {
            user.AssignToGroup(group.Id, now);
        }
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

        if (storedToken is null)
        {
            return null;
        }

        if (!storedToken.IsActiveAt(now))
        {
            if (storedToken.IsRevoked)
            {
                await RevokeFamilyAsync(storedToken.UserId, now, cancellationToken);
            }

            return null;
        }

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(candidate => candidate.Id == storedToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            await RevokeFamilyAsync(storedToken.UserId, now, cancellationToken);
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

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _dbContext.ChangeTracker.Clear();
            await RevokeFamilyAsync(user.Id, now, cancellationToken);
            return null;
        }

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

    private async Task RevokeFamilyAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(now);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}