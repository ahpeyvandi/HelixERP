namespace HelixERP.Shell.Api.Gateway;

public static class GatewayPolicies
{
    public const string AuthenticatedUser = "HelixAuthenticated";

    public const string PlatformAdmin = "HelixPlatformAdmin";

    public const string UserIdHeader = "X-Helix-User-Id";

    public const string UserLoginHeader = "X-Helix-User-Login";

    public const string GroupsHeader = "X-Helix-Groups";

    public const string SuperUserHeader = "X-Helix-Superuser";
}