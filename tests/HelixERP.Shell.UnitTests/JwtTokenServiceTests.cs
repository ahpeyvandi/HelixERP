using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using HelixERP.Shell.Application.Security;
using HelixERP.Shell.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HelixERP.Shell.UnitTests;

public sealed class JwtTokenServiceTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static JwtOptions CreateOptions() => new()
    {
        Issuer = "HelixERP.Test",
        Audience = "HelixERP.Test.Clients",
        SigningKey = "unit-test-signing-key-with-more-than-32-chars",
        AccessTokenMinutes = 30,
        RefreshTokenDays = 7
    };

    [Fact]
    public void Access_token_contains_identity_and_group_claims()
    {
        var options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options));
        var userId = Guid.NewGuid();

        var token = service.CreateAccessToken(
            userId,
            "admin",
            "Administrator",
            true,
            ["base.group_user", "base.group_system"],
            Now);

        token.ExpiresAtUtc.Should().Be(Now.AddMinutes(30));

        var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }.ValidateToken(
            token.Value,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.SigningKey)),
                ValidateLifetime = false,
                NameClaimType = JwtRegisteredClaimNames.UniqueName,
                RoleClaimType = JwtTokenService.GroupClaim
            },
            out _);

        principal.FindFirstValue(JwtRegisteredClaimNames.Sub).Should().Be(userId.ToString());
        principal.Identity!.Name.Should().Be("admin");
        principal.FindFirstValue("display_name").Should().Be("Administrator");
        principal.FindFirstValue(JwtTokenService.SuperUserClaim).Should().Be("true");
        principal.FindAll(JwtTokenService.GroupClaim).Select(claim => claim.Value)
            .Should().BeEquivalentTo(["base.group_user", "base.group_system"]);
        principal.IsInRole("base.group_system").Should().BeTrue();
    }

    [Fact]
    public void Expired_token_fails_lifetime_validation()
    {
        var options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options));

        var token = service.CreateAccessToken(Guid.NewGuid(), "user", "User", false, [], Now.AddHours(-2));

        var action = () => new JwtSecurityTokenHandler().ValidateToken(
            token.Value,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(options.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            },
            out _);

        action.Should().Throw<SecurityTokenExpiredException>();
    }

    [Fact]
    public void Refresh_tokens_are_random_and_hashable()
    {
        var service = new JwtTokenService(Options.Create(CreateOptions()));

        var first = service.CreateRefreshToken(Now);
        var second = service.CreateRefreshToken(Now);

        first.Value.Should().NotBe(second.Value);
        first.Hash.Should().Be(service.HashRefreshToken(first.Value));
        first.Hash.Should().HaveLength(64);
        first.ExpiresAtUtc.Should().Be(Now.AddDays(7));
    }

    [Fact]
    public void Short_signing_key_is_rejected()
    {
        var options = CreateOptions();
        options.SigningKey = "too-short";

        var action = () => new JwtTokenService(Options.Create(options));

        action.Should().Throw<InvalidOperationException>().WithMessage("*SigningKey*");
    }
}