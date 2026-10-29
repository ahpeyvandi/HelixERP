using HelixERP.Shell.Application.Modules;
using Yarp.ReverseProxy.Configuration;

namespace HelixERP.Shell.Api.Gateway;

public static class ModuleRouteBuilder
{
    public const string RoutePrefix = "/modules";

    public static string GetModulePrefix(string moduleKey) => $"{RoutePrefix}/{moduleKey}";

    public static IReadOnlyList<RouteConfig> BuildRoutes(IEnumerable<ModuleRegistrationDto> modules)
        => modules
            .Where(module => module.IsActive)
            .OrderBy(module => module.ModuleKey, StringComparer.Ordinal)
            .Select(module =>
            {
                var prefix = GetModulePrefix(module.ModuleKey);

                return new RouteConfig
                {
                    RouteId = $"module.{module.ModuleKey}",
                    ClusterId = ClusterIdFor(module.ModuleKey),
                    Order = 100,
                    Match = new RouteMatch
                    {
                        Path = $"{prefix}/{{**catch-all}}"
                    },
                    AuthorizationPolicy = GatewayPolicies.AuthenticatedUser,
                    Transforms = new List<IReadOnlyDictionary<string, string>>
                    {
                        new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["PathRemovePrefix"] = prefix
                        }
                    }
                };
            })
            .ToList();

    public static IReadOnlyList<ClusterConfig> BuildClusters(IEnumerable<ModuleRegistrationDto> modules)
        => modules
            .Where(module => module.IsActive)
            .OrderBy(module => module.ModuleKey, StringComparer.Ordinal)
            .Select(module => new ClusterConfig
            {
                ClusterId = ClusterIdFor(module.ModuleKey),
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
                {
                    ["primary"] = new DestinationConfig
                    {
                        Address = module.BaseUrl
                    }
                }
            })
            .ToList();

    public static string ClusterIdFor(string moduleKey) => $"module.{moduleKey}";
}