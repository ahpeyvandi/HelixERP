using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HelixERP.IdentityServer.Api.Pages.Login;

public class LoginModel : PageModel
{
    private readonly IIdentityServerInteractionService _interaction;

    public LoginModel(IIdentityServerInteractionService interaction)
    {
        _interaction = interaction;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGet(string returnUrl)
    {
        ReturnUrl = returnUrl;
        var authContext = await _interaction.GetAuthorizationContextAsync(returnUrl);
        Input.Username = authContext?.LoginHint ?? string.Empty;

        return Page();
    }

    public async Task<IActionResult> OnPost(string returnUrl)
    {
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(Input.Username) ||
            string.IsNullOrWhiteSpace(Input.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            ReturnUrl = returnUrl;
            return Page();
        }

        var user = Database.TestUsers.SingleOrDefault(candidate =>
            candidate.Username == Input.Username.Trim());

        if (user is null || user.Password is null || user.Password != Input.Password)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            ReturnUrl = returnUrl;
            return Page();
        }

        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, user.SubjectId)
        };

        if (user.Claims is not null)
        {
            claims.AddRange(user.Claims);
        }

        var principal = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
                claims,
                IdentityServerConstants.DefaultCookieAuthenticationScheme));

        await HttpContext.SignInAsync(
            IdentityServerConstants.DefaultCookieAuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = Input.RememberLogin });

        var authContext = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (authContext is not null || Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("~/");
    }

    public class InputModel
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool RememberLogin { get; set; } = true;
    }
}
