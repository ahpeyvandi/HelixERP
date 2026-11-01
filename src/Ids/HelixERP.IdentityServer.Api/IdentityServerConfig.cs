using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace HelixERP.IdentityServer.Api;

/// <summary>
/// In-memory IdentityServer client and resource configuration for the HelixERP shell.
/// The shell acts as a confidential OpenID Connect client (code flow + PKCE) so its
/// configured ClientId/ClientSecret must match the values in this class.
/// </summary>
public static class IdentityServerConfig
{
    public const string ShellClientId = "helix-shell";
    public const string ShellClientSecret = "helix-shell-secret-2026";

    public static IEnumerable<IdentityResource> IdentityResources { get; } =
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResources.Email(),
        new IdentityResource("groups", "User group memberships", ["groups"])
    ];

    public static IEnumerable<ApiScope> ApiScopes { get; } =
    [
        new ApiScope("helix.api", "HelixERP Shell API")
    ];

    public static IEnumerable<Client> Clients { get; } =
    [
        new Client
        {
            ClientId = ShellClientId,
            ClientName = "HelixERP Shell",
            ClientSecrets = { new Secret(ShellClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = true,
            RedirectUris =
            {
                "http://localhost:5080/api/auth/oidc/callback",
                "http://helixerp-shell:5080/api/auth/oidc/callback"
            },
            PostLogoutRedirectUris =
            {
                "http://localhost:5173",
                "http://helixerp-shell:5080"
            },
            AllowedScopes =
            {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.Email,
                "groups",
                "helix.api"
            },
            AllowOfflineAccess = true,
            AlwaysIncludeUserClaimsInIdToken = true
        }
    ];
}
