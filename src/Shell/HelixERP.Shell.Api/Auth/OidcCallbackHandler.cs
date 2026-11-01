using System.Security.Claims;
using HelixERP.Shell.Application.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using AuthOptions = HelixERP.Shell.Application.Security.AuthenticationOptions;

namespace HelixERP.Shell.Api.Auth;

/// <summary>
/// Completes a successful IdentityServer sign-in by mapping the external identity to an
/// internal HelixERP user, issuing internal JWT + refresh tokens and redirecting the browser
/// back to the SPA with the tokens in the URL fragment.
/// </summary>
public static class OidcCallbackHandler
{
    public static Func<TicketReceivedContext, Task> BuildTicketReceivedHandler()
        => HandleTicketReceived;

    public static async Task HandleTicketReceived(TicketReceivedContext ticket)
    {
        var serviceProvider = ticket.HttpContext.RequestServices;
        var authService = serviceProvider.GetRequiredService<IAuthService>();
        var authOptions = serviceProvider
            .GetRequiredService<IOptions<AuthOptions>>().Value;

        var oidc = authOptions.Oidc;
        var principal = ticket.Principal;

        var subject = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal?.FindFirstValue("sub");

        if (principal is null || string.IsNullOrWhiteSpace(subject))
        {
            RedirectToFrontend(ticket, oidc.FrontendRedirectUri, failed: true);
            return;
        }

        var attempt = new ExternalLoginAttempt(
            OidcAuthenticationOptions.ProviderKey,
            subject,
            principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.FindFirstValue("name")
                ?? principal.FindFirstValue(ClaimTypes.Email),
            principal.FindFirstValue(ClaimTypes.Email),
            string.IsNullOrWhiteSpace(oidc.GroupClaim)
                ? []
                : principal.FindAll(oidc.GroupClaim).Select(claim => claim.Value).ToList());

        var authResult = await authService.LoginWithExternalIdentityAsync(
            attempt,
            ticket.HttpContext.RequestAborted);

        ticket.HandleResponse();

        if (authResult is null)
        {
            RedirectToFrontend(ticket, oidc.FrontendRedirectUri, failed: true);
            return;
        }

        ticket.Response.Redirect(BuildFrontendUri(oidc.FrontendRedirectUri, authResult));
    }

    private static void RedirectToFrontend(
        TicketReceivedContext ticket,
        string uri,
        bool failed)
        => ticket.Response.Redirect(failed ? $"{uri}/#/login?auth=failed" : uri);

    private static string BuildFrontendUri(string baseUri, AuthResult result)
    {
        var tokens = result.Tokens;

        var fragment =
            $"access_token={Uri.EscapeDataString(tokens.AccessToken)}" +
            $"&refresh_token={Uri.EscapeDataString(tokens.RefreshToken)}" +
            $"&access_token_expires_at={Uri.EscapeDataString(tokens.AccessTokenExpiresAtUtc.ToString("o"))}" +
            $"&refresh_token_expires_at={Uri.EscapeDataString(tokens.RefreshTokenExpiresAtUtc.ToString("o"))}";

        return $"{baseUri}/#/oidc/callback?{fragment}";
    }
}
