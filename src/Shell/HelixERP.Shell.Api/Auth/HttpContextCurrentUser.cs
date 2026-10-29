using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelixERP.BuildingBlocks.Shared.Abstractions;

namespace HelixERP.Shell.Api.Auth;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var userId) ? userId : null;
        }
    }

    public Guid? CompanyId
    {
        get
        {
            var value = User?.FindFirstValue("company_id");
            return Guid.TryParse(value, out var companyId) ? companyId : null;
        }
    }

    public string? UserName => User?.Identity?.Name;

    public string? DisplayName
        => User?.FindFirstValue("display_name") ?? User?.Identity?.Name;

    public bool IsSuperUser
        => string.Equals(
            User?.FindFirstValue(HelixERP.Shell.Infrastructure.Security.JwtTokenService.SuperUserClaim),
            "true",
            StringComparison.Ordinal);

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
}