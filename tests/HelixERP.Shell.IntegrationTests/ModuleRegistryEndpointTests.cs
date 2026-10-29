using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HelixERP.Shell.Application.Modules;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;

namespace HelixERP.Shell.IntegrationTests;

public sealed class ModuleRegistryEndpointTests
{
    [Fact]
    public async Task Regular_users_can_read_but_not_change_the_registry()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        await factory.CreateInternalUserAsync("seller", "Seller#12345");
        var auth = await IntegrationTestHelpers.LoginAsync(client, "seller", "Seller#12345");
        client.WithBearerToken(auth.Tokens.AccessToken);

        var list = await client.GetAsync("/api/modules");
        var register = await client.PostAsJsonAsync(
            "/api/modules",
            new { moduleKey = "sales", displayName = "Sales", host = "10.0.0.1", port = 8080 });

        list.StatusCode.Should().Be(HttpStatusCode.OK);
        register.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_requests_are_rejected()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/modules");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Module_lifecycle_updates_gateway_configuration()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        var created = await client.PostAsJsonAsync(
            "/api/modules",
            new { moduleKey = "sales", displayName = "Sales", host = "10.0.0.10", port = 8081 });

        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var module = await created.Content.ReadFromJsonAsync<ModuleRegistrationDto>();
        module!.ModuleKey.Should().Be("sales");
        module.BaseUrl.Should().Be("http://10.0.0.10:8081");

        var gatewayConfig = factory.Services.GetRequiredService<IProxyConfigProvider>().GetConfig();
        gatewayConfig.Routes.Should().ContainSingle(route => route.RouteId == "module.sales");
        gatewayConfig.Clusters.Should().ContainSingle(cluster => cluster.ClusterId == "module.sales");

        var updated = await client.PutAsJsonAsync(
            "/api/modules/sales/endpoint",
            new { host = "10.0.0.11", port = 8082 });
        updated.EnsureSuccessStatusCode();

        var deactivated = await client.PostAsync("/api/modules/sales/deactivate", null);
        deactivated.EnsureSuccessStatusCode();
        factory.Services.GetRequiredService<IProxyConfigProvider>().GetConfig().Routes.Should().BeEmpty();

        var activated = await client.PostAsync("/api/modules/sales/activate", null);
        activated.EnsureSuccessStatusCode();
        factory.Services.GetRequiredService<IProxyConfigProvider>().GetConfig().Routes.Should().ContainSingle();

        var removed = await client.DeleteAsync("/api/modules/sales");
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        factory.Services.GetRequiredService<IProxyConfigProvider>().GetConfig().Routes.Should().BeEmpty();

        var missing = await client.GetAsync("/api/modules/sales");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Duplicate_registration_returns_a_problem_response()
    {
        using var factory = new ShellApiFactory();
        using var client = factory.CreateClient();

        var auth = await IntegrationTestHelpers.LoginAsync(client, factory.AdminLogin, factory.AdminPassword);
        client.WithBearerToken(auth.Tokens.AccessToken);

        var payload = new { moduleKey = "inventory", displayName = "Inventory", host = "10.0.0.20", port = 8090 };

        (await client.PostAsJsonAsync("/api/modules", payload)).StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await client.PostAsJsonAsync("/api/modules", payload);

        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        duplicate.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}