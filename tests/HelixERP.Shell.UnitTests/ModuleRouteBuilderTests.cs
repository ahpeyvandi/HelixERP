using FluentAssertions;
using HelixERP.Shell.Api.Gateway;
using HelixERP.Shell.Application.Modules;

namespace HelixERP.Shell.UnitTests;

public sealed class ModuleRouteBuilderTests
{
    private static ModuleRegistrationDto Module(string key, bool isActive = true, string baseUrl = "http://10.0.0.5:8081") => new(
        Guid.NewGuid(),
        key,
        key,
        "10.0.0.5",
        8081,
        isActive,
        baseUrl,
        DateTime.UtcNow,
        null);

    [Fact]
    public void Active_modules_produce_routes_and_clusters()
    {
        var modules = new[] { Module("sales"), Module("inventory"), Module("accounting", isActive: false) };

        var routes = ModuleRouteBuilder.BuildRoutes(modules);
        var clusters = ModuleRouteBuilder.BuildClusters(modules);

        routes.Should().HaveCount(2);
        clusters.Should().HaveCount(2);

        var salesRoute = routes.Single(route => route.RouteId == "module.sales");
        salesRoute.ClusterId.Should().Be("module.sales");
        salesRoute.Match.Path.Should().Be("/modules/sales/{**catch-all}");
        salesRoute.AuthorizationPolicy.Should().Be(GatewayPolicies.AuthenticatedUser);
        salesRoute.Transforms.Should().NotBeNull();
        salesRoute.Transforms!.Single()["PathRemovePrefix"].Should().Be("/modules/sales");

        var salesCluster = clusters.Single(cluster => cluster.ClusterId == "module.sales");
        salesCluster.Destinations!["primary"].Address.Should().Be("http://10.0.0.5:8081");
    }

    [Fact]
    public void Inactive_modules_are_not_exposed()
    {
        var routes = ModuleRouteBuilder.BuildRoutes([Module("sales", isActive: false)]);
        var clusters = ModuleRouteBuilder.BuildClusters([Module("sales", isActive: false)]);

        routes.Should().BeEmpty();
        clusters.Should().BeEmpty();
    }

    [Fact]
    public void Ipv6_base_urls_are_preserved()
    {
        var module = Module("inventory", baseUrl: "http://[::1]:8090");

        var cluster = ModuleRouteBuilder.BuildClusters([module]).Single();

        cluster.Destinations!["primary"].Address.Should().Be("http://[::1]:8090");
    }
}