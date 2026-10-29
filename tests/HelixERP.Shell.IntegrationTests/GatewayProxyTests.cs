using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace HelixERP.Shell.IntegrationTests;

public sealed class GatewayProxyTests
{
    [Fact]
    public async Task Gateway_forwards_to_the_registered_endpoint_and_injects_user_context()
    {
        using var factory = new ShellApiFactory(useFakeUpstream: true);
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        var created = await client.PostAsJsonAsync(
            "/api/modules",
            new { moduleKey = "demo", displayName = "Demo", host = "127.0.0.1", port = 5099 });
        created.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/modules/demo/hello?x=1");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("upstream-ok");

        factory.LastUpstreamPath.Should().Be("/hello");
        factory.LastUpstreamQuery.Should().Be("?x=1");
        factory.LastUpstreamUserId.Should().Be(auth.User.Id.ToString());
        factory.LastUpstreamGroups.Should().Contain("base.group_system");
        factory.LastUpstreamSuperUser.Should().Be("true");
    }

    [Fact]
    public async Task Gateway_route_requires_authentication()
    {
        using var factory = new ShellApiFactory(useFakeUpstream: true);
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        var created = await client.PostAsJsonAsync(
            "/api/modules",
            new { moduleKey = "demo", displayName = "Demo", host = "127.0.0.1", port = 5099 });
        created.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Authorization = null;

        var anonymous = await client.GetAsync("/modules/demo/hello");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var invalidToken = await client.GetAsync("/modules/demo/hello?access_token=invalid");
        invalidToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}