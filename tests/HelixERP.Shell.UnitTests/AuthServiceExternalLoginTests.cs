using FluentAssertions;
using HelixERP.BuildingBlocks.Domain.Security;
using HelixERP.Shell.Application.Security;
using HelixERP.Shell.Infrastructure.Persistence;
using HelixERP.Shell.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelixERP.Shell.UnitTests;

public sealed class AuthServiceExternalLoginTests
{
    private static readonly System.DateTime Now = new(2026, 11, 1, 9, 0, 0, System.DateTimeKind.Utc);

    private static JwtOptions CreateJwtOptions()
        => new()
        {
            Issuer = "HelixERP.Test",
            Audience = "HelixERP.Test.Clients",
            SigningKey = "unit-test-signing-key-with-more-than-32-chars",
            AccessTokenMinutes = 30,
            RefreshTokenDays = 7
        };

    private static AuthenticationOptions CreateAuthOptions()
        => new()
        {
            LocalEnabled = true,
            LdapEnabled = false,
            OidcEnabled = true,
            Oidc = new OidcAuthenticationOptions
            {
                AutoProvisionUsers = true,
                DefaultGroup = "base.group_user",
                GroupClaim = "groups",
                GroupClaimMappings = new Dictionary<string, string>
                {
                    ["ERP_Admins"] = "base.group_erp_manager"
                }
            }
        };

    private static AuthService CreateAuthService(ShellDbContext db, bool autoProvision = true)
    {
        var options = CreateAuthOptions();
        options.Oidc.AutoProvisionUsers = autoProvision;

        return new AuthService(
            db,
            new Pbkdf2PasswordHasher(),
            new JwtTokenService(Options.Create(CreateJwtOptions())),
            new TestClock(Now),
            Options.Create(options));
    }

    private static async Task<(ShellDbContext db, AuthService auth)> SetupAsync()
    {
        var db = TestDb.Create();
        db.Groups.Add(Group.Create("base.group_user", "Internal User", "Administration", createdAtUtc: Now));
        db.Groups.Add(Group.Create("base.group_erp_manager", "Access Rights", "Administration", isSystem: true, createdAtUtc: Now));
        await db.SaveChangesAsync();

        return (db, CreateAuthService(db));
    }

    [Fact]
    public async Task External_login_provisions_a_new_user_with_default_and_mapped_groups()
    {
        var (db, auth) = await SetupAsync();

        var result = await auth.LoginWithExternalIdentityAsync(new ExternalLoginAttempt(
            "identityserver",
            "sub-8130",
            "Alice External",
            "alice@helix.local",
            ["ERP_Admins"]));

        result.Should().NotBeNull();
        result!.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Groups.Should().Contain("base.group_user");
        result.User.Groups.Should().Contain("base.group_erp_manager");

        var user = await db.Users
            .Include(candidate => candidate.ExternalLogins)
            .Include(candidate => candidate.Memberships)
            .SingleAsync();
        user.Login.Should().NotBeNullOrWhiteSpace();
        user.DisplayName.Should().Be("Alice External");
        user.ExternalLogins.Should().ContainSingle(login =>
            login.Provider == "identityserver" && login.Subject == "sub-8130");
        user.Memberships.Should().HaveCount(2);
    }

    [Fact]
    public async Task External_login_is_idempotent_and_reuses_an_existing_external_mapping()
    {
        var (db, auth) = await SetupAsync();

        var first = await auth.LoginWithExternalIdentityAsync(new ExternalLoginAttempt(
            "identityserver",
            "sub-8130",
            "Alice External",
            "alice@helix.local",
            []));
        var second = await auth.LoginWithExternalIdentityAsync(new ExternalLoginAttempt(
            "identityserver",
            "sub-8130",
            "Alice External",
            "alice@helix.local",
            []));

        first.Should().NotBeNull();
        second.Should().NotBeNull();

        db.Users.Count().Should().Be(1);
        db.ExternalLogins.Count().Should().Be(1);
    }

    [Fact]
    public async Task External_login_links_an_existing_user_by_email_when_provisioning_is_off()
    {
        var db = TestDb.Create();
        var user = User.Create(
            "bob",
            "bob@helix.local",
            "Bob",
            new Pbkdf2PasswordHasher().Hash("bob-password"),
            createdAtUtc: Now);
        db.Users.Add(user);
        db.Groups.Add(Group.Create("base.group_user", "Internal User", "Administration", createdAtUtc: Now));
        await db.SaveChangesAsync();

        var auth = CreateAuthService(db, autoProvision: false);

        var result = await auth.LoginWithExternalIdentityAsync(new ExternalLoginAttempt(
            "identityserver",
            "bob-sub",
            "Bob",
            "bob@helix.local",
            []));

        result.Should().NotBeNull();
        result!.User.Login.Should().Be("bob");

        var linked = await db.ExternalLogins.SingleOrDefaultAsync(login => login.Subject == "bob-sub");
        linked.Should().NotBeNull();
        linked!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task External_login_is_rejected_when_provisioning_is_off_and_no_email_matches()
    {
        var db = TestDb.Create();
        db.Groups.Add(Group.Create("base.group_user", "Internal User", "Administration", createdAtUtc: Now));
        await db.SaveChangesAsync();

        var strictAuth = CreateAuthService(db, autoProvision: false);

        var result = await strictAuth.LoginWithExternalIdentityAsync(new ExternalLoginAttempt(
            "identityserver",
            "nobody-sub",
            "Nobody",
            "nobody@helix.local",
            []));

        result.Should().BeNull();
        db.Users.Count().Should().Be(0);
    }
}
