namespace HelixERP.Shell.Application.Security;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool LocalEnabled { get; set; } = true;

    public bool LdapEnabled { get; set; }

    public bool OidcEnabled { get; set; }

    public LdapAuthenticationOptions Ldap { get; set; } = new();

    public OidcAuthenticationOptions Oidc { get; set; } = new();

    public void Validate()
    {
        if (OidcEnabled && string.IsNullOrWhiteSpace(Oidc.Authority))
        {
            throw new InvalidOperationException(
                "Authentication:Oidc:Authority must be configured when OIDC logins are enabled.");
        }
    }
}

public sealed class LdapAuthenticationOptions
{
    public string? Server { get; set; }

    public int Port { get; set; } = 389;

    public string? BaseDn { get; set; }

    public string? Domain { get; set; }
}

public sealed class OidcAuthenticationOptions
{
    public const string ProviderKey = "identityserver";

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string Scopes { get; set; } = "openid profile email";

    public string CallbackPath { get; set; } = "/api/auth/oidc/callback";

    public string FrontendRedirectUri { get; set; } = "http://localhost:5173";

    public bool AutoProvisionUsers { get; set; } = true;

    public string DefaultGroup { get; set; } = "base.group_user";

    public string? GroupClaim { get; set; }

    public IDictionary<string, string> GroupClaimMappings { get; set; } = new Dictionary<string, string>();

    public IReadOnlyCollection<string> GetScopes() => string.IsNullOrWhiteSpace(Scopes)
        ? ["openid", "profile", "email"]
        : Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
