using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace HelixERP.Shell.IntegrationTests;

public sealed class AuthEndpointTests
{
    [Fact]
    public async Task Admin_can_login_and_read_current_user()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);

        auth.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.User.IsSuperUser.Should().BeTrue();
        auth.User.Groups.Should().Contain("base.group_system");

        client.WithBearerToken(auth.Tokens.AccessToken);
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");

        me.Should().NotBeNull();
        me!.Login.Should().Be(factory.AdminLogin);
        me.IsSuperUser.Should().BeTrue();
        me.Groups.Should().Contain("base.group_system");
    }

    [Fact]
    public async Task Invalid_credentials_return_unauthorized()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { login = factory.AdminLogin, password = "wrong-password" });
        var unknownUser = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { login = "nobody", password = "whatever" });

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownUser.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_tokens_rotate_and_old_tokens_are_rejected()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = auth.Tokens.RefreshToken });
        refreshResponse.EnsureSuccessStatusCode();

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<HelixERP.Shell.Application.Security.AuthResult>();
        refreshed.Should().NotBeNull();
        refreshed!.Tokens.RefreshToken.Should().NotBe(auth.Tokens.RefreshToken);

        var reuse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = auth.Tokens.RefreshToken });

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = auth.Tokens.RefreshToken });
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = auth.Tokens.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record CurrentUserResponse(
        Guid Id,
        string Login,
        string DisplayName,
        bool IsSuperUser,
        IReadOnlyCollection<string> Groups);
}