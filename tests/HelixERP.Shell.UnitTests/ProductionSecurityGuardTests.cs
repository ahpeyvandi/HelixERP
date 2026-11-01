using FluentAssertions;
using HelixERP.Shell.Api.Infrastructure;
using HelixERP.Shell.Application.Bootstrap;
using HelixERP.Shell.Application.Security;

namespace HelixERP.Shell.UnitTests;

public sealed class ProductionSecurityGuardTests
{
    private static JwtOptions CreateJwt(string signingKey)
        => new()
        {
            Issuer = "HelixERP.Shell",
            Audience = "HelixERP.Clients",
            SigningKey = signingKey,
            AccessTokenMinutes = 60,
            RefreshTokenDays = 14
        };

    [Fact]
    public void Development_environment_is_not_blocked_by_default_secrets()
    {
        var jwt = CreateJwt(ProductionSecurityGuard.DefaultDevSigningKey);
        var bootstrap = new BootstrapOptions { AdminPassword = ProductionSecurityGuard.DefaultDevAdminPassword };

        var action = () => ProductionSecurityGuard.Validate("Development", jwt, bootstrap);

        action.Should().NotThrow();
    }

    [Fact]
    public void Production_blocks_default_jwt_signing_key()
    {
        var jwt = CreateJwt(ProductionSecurityGuard.DefaultDevSigningKey);
        var bootstrap = new BootstrapOptions
        {
            SeedOnStartup = false,
            AdminPassword = "an-independent-secret"
        };

        var action = () => ProductionSecurityGuard.Validate("Production", jwt, bootstrap);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:SigningKey*default*");
    }

    [Fact]
    public void Production_blocks_default_admin_password_when_seeding()
    {
        var jwt = CreateJwt("a-real-independent-production-signing-key-with-sufficient-length");
        var bootstrap = new BootstrapOptions
        {
            SeedOnStartup = true,
            AdminPassword = ProductionSecurityGuard.DefaultDevAdminPassword
        };

        var action = () => ProductionSecurityGuard.Validate("Production", jwt, bootstrap);

        action.Should().Throw<InvalidOperationException>().WithMessage("*Bootstrap:AdminPassword*default*");
    }

    [Fact]
    public void Production_allows_strong_non_default_secrets()
    {
        var jwt = CreateJwt("a-real-independent-production-signing-key-with-sufficient-length");
        var bootstrap = new BootstrapOptions
        {
            SeedOnStartup = true,
            AdminPassword = "A-Strong+Admin!Secret#99"
        };

        var action = () => ProductionSecurityGuard.Validate("Production", jwt, bootstrap);

        action.Should().NotThrow();
    }
}
