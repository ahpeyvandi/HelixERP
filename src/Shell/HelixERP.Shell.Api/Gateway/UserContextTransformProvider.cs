using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HelixERP.Shell.Infrastructure.Security;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace HelixERP.Shell.Api.Gateway;

public sealed class UserContextTransformProvider : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(transform =>
        {
            var user = transform.HttpContext.User;
            var headers = transform.ProxyRequest.Headers;

            headers.Remove(GatewayPolicies.UserIdHeader);
            headers.Remove(GatewayPolicies.UserLoginHeader);
            headers.Remove(GatewayPolicies.GroupsHeader);
            headers.Remove(GatewayPolicies.SuperUserHeader);

            if (user.Identity?.IsAuthenticated != true)
            {
                return ValueTask.CompletedTask;
            }

            var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!string.IsNullOrEmpty(userId))
            {
                headers.TryAddWithoutValidation(GatewayPolicies.UserIdHeader, userId);
            }

            if (!string.IsNullOrEmpty(user.Identity.Name))
            {
                headers.TryAddWithoutValidation(GatewayPolicies.UserLoginHeader, user.Identity.Name);
            }

            var groups = user.FindAll(JwtTokenService.GroupClaim).Select(claim => claim.Value).ToArray();

            if (groups.Length > 0)
            {
                headers.TryAddWithoutValidation(GatewayPolicies.GroupsHeader, string.Join(',', groups));
            }

            headers.TryAddWithoutValidation(
                GatewayPolicies.SuperUserHeader,
                user.FindFirstValue(JwtTokenService.SuperUserClaim) == "true" ? "true" : "false");

            return ValueTask.CompletedTask;
        });
    }
}