using HelixERP.Shell.Api.Gateway;
using HelixERP.Shell.Application.Modules;

namespace HelixERP.Shell.Api.Endpoints;

public static class ModuleEndpoints
{
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/modules").WithTags("Module Registry");

        group.MapGet("/", async (IModuleRegistry registry, CancellationToken cancellationToken) =>
                Results.Ok(await registry.ListAsync(cancellationToken)))
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("ListModules");

        group.MapGet("/{moduleKey}", async (
                string moduleKey,
                IModuleRegistry registry,
                CancellationToken cancellationToken) =>
            {
                var module = await registry.FindAsync(moduleKey, cancellationToken);

                return module is null
                    ? Results.NotFound(new { moduleKey, message = "Module is not registered." })
                    : Results.Ok(module);
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("GetModule");

        group.MapPost("/", async (
                RegisterModuleRequest request,
                IModuleRegistry registry,
                IModuleRouteSynchronizer synchronizer,
                CancellationToken cancellationToken) =>
            {
                var module = await registry.RegisterAsync(request, cancellationToken);
                await synchronizer.ReloadAsync(cancellationToken);

                return Results.Created($"/api/modules/{module.ModuleKey}", module);
            })
            .RequireAuthorization(GatewayPolicies.PlatformAdmin)
            .WithName("RegisterModule");

        group.MapPut("/{moduleKey}/endpoint", async (
                string moduleKey,
                UpdateModuleEndpointRequest request,
                IModuleRegistry registry,
                IModuleRouteSynchronizer synchronizer,
                CancellationToken cancellationToken) =>
            {
                var module = await registry.UpdateEndpointAsync(moduleKey, request, cancellationToken);
                await synchronizer.ReloadAsync(cancellationToken);

                return Results.Ok(module);
            })
            .RequireAuthorization(GatewayPolicies.PlatformAdmin)
            .WithName("UpdateModuleEndpoint");

        group.MapPost("/{moduleKey}/activate", async (
                string moduleKey,
                IModuleRegistry registry,
                IModuleRouteSynchronizer synchronizer,
                CancellationToken cancellationToken) =>
            {
                var module = await registry.SetActiveAsync(moduleKey, true, cancellationToken);
                await synchronizer.ReloadAsync(cancellationToken);

                return Results.Ok(module);
            })
            .RequireAuthorization(GatewayPolicies.PlatformAdmin)
            .WithName("ActivateModule");

        group.MapPost("/{moduleKey}/deactivate", async (
                string moduleKey,
                IModuleRegistry registry,
                IModuleRouteSynchronizer synchronizer,
                CancellationToken cancellationToken) =>
            {
                var module = await registry.SetActiveAsync(moduleKey, false, cancellationToken);
                await synchronizer.ReloadAsync(cancellationToken);

                return Results.Ok(module);
            })
            .RequireAuthorization(GatewayPolicies.PlatformAdmin)
            .WithName("DeactivateModule");

        group.MapDelete("/{moduleKey}", async (
                string moduleKey,
                IModuleRegistry registry,
                IModuleRouteSynchronizer synchronizer,
                CancellationToken cancellationToken) =>
            {
                var removed = await registry.RemoveAsync(moduleKey, cancellationToken);

                if (!removed)
                {
                    return Results.NotFound(new { moduleKey, message = "Module is not registered." });
                }

                await synchronizer.ReloadAsync(cancellationToken);

                return Results.NoContent();
            })
            .RequireAuthorization(GatewayPolicies.PlatformAdmin)
            .WithName("RemoveModule");

        return endpoints;
    }
}