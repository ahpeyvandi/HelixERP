using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Shared.Abstractions;
using HelixERP.Shell.Api.Gateway;
using HelixERP.Shell.Application.Security;

namespace HelixERP.Shell.Api.Endpoints;

public sealed record RefreshTokenRequest(string RefreshToken);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
                LoginRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.LoginAsync(request, cancellationToken);

                return result is null
                    ? Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid credentials",
                        detail: "The login name or password is incorrect.")
                    : Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("Login");

        group.MapPost("/refresh", async (
                RefreshTokenRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);

                return result is null
                    ? Results.Problem(
                        statusCode: StatusCodes.Status401Unauthorized,
                        title: "Invalid refresh token",
                        detail: "The refresh token is unknown, revoked or expired.")
                    : Results.Ok(result);
            })
            .AllowAnonymous()
            .WithName("RefreshToken");

        group.MapPost("/logout", async (
                RefreshTokenRequest request,
                IAuthService authService,
                CancellationToken cancellationToken) =>
            {
                await authService.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("Logout");

        group.MapGet("/me", async (
                ICurrentUser currentUser,
                IPermissionService permissionService,
                CancellationToken cancellationToken) =>
            {
                if (currentUser.UserId is not { } userId)
                {
                    return Results.Unauthorized();
                }

                var groups = await permissionService.GetGroupsAsync(userId, cancellationToken);

                return Results.Ok(new AuthUserInfo(
                    userId,
                    currentUser.UserName ?? string.Empty,
                    currentUser.DisplayName ?? currentUser.UserName ?? string.Empty,
                    currentUser.IsSuperUser,
                    groups));
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("CurrentUser");

        return endpoints;
    }
}