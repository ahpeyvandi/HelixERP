using HelixERP.Shell.Application.Bootstrap;
using HelixERP.Shell.Application.Security;
using Microsoft.Extensions.Hosting;

namespace HelixERP.Shell.Api.Infrastructure;

public static class ProductionSecurityGuard
{
    public const string DefaultDevSigningKey = "helix-dev-signing-key-change-me-in-production-2026";
    public const string DefaultDevAdminPassword = "Helix@12345";

    public static void Validate(string environmentName, JwtOptions jwt, BootstrapOptions bootstrap)
    {
        if (!string.Equals(environmentName, Environments.Production, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(jwt.SigningKey, DefaultDevSigningKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Production startup is blocked: Jwt:SigningKey still uses the development default. " +
                "Set a strong, unique Jwt:SigningKey before starting the Shell in a Production environment.");
        }

        if (bootstrap.SeedOnStartup &&
            string.Equals(bootstrap.AdminPassword, DefaultDevAdminPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Production startup is blocked: Bootstrap:AdminPassword still uses the development default. " +
                "Set a strong, unique Bootstrap:AdminPassword, or disable Bootstrap:SeedOnStartup before " +
                "starting the Shell in a Production environment.");
        }
    }
}
