using HelixERP.BuildingBlocks.Application.Security;
using HelixERP.BuildingBlocks.Application.Storage;
using HelixERP.Shell.Api.Gateway;

namespace HelixERP.Shell.Api.Endpoints;

public static class FileEndpoints
{
    public const string Model = "base.attachment";

    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/files").WithTags("Files");

        group.MapPost("/", async (
                HttpContext context,
                IFileStorage fileStorage,
                IPermissionService permissionService,
                CancellationToken cancellationToken) =>
            {
                if (!await permissionService.HasAccessAsync(Model, OperationType.Create, cancellationToken))
                {
                    return Results.Forbid();
                }

                if (!context.Request.HasFormContentType)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status415UnsupportedMediaType,
                        title: "Multipart form required",
                        detail: "Upload files as multipart/form-data with a field named 'file'.");
                }

                var form = await context.Request.ReadFormAsync(cancellationToken);
                var file = form.Files.GetFile("file");

                if (file is null || file.Length == 0)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "File is required",
                        detail: "Provide a non-empty file in the 'file' form field.");
                }

                await using var stream = file.OpenReadStream();

                var metadata = await fileStorage.SaveAsync(
                    new FileUploadRequest(
                        stream,
                        file.FileName,
                        file.ContentType,
                        file.Length,
                        form["relatedEntity"].ToString(),
                        form["relatedId"].ToString()),
                    cancellationToken);

                return Results.Created($"/api/files/{metadata.Id}", metadata);
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("UploadFile");

        group.MapGet("/{fileId:guid}", async (
                Guid fileId,
                IFileStorage fileStorage,
                IPermissionService permissionService,
                CancellationToken cancellationToken) =>
            {
                if (!await permissionService.HasAccessAsync(Model, OperationType.Read, cancellationToken))
                {
                    return Results.Forbid();
                }

                var file = await fileStorage.OpenAsync(fileId, cancellationToken);

                if (file is null)
                {
                    return Results.NotFound(new { fileId, message = "File not found." });
                }

                return Results.File(
                    file.Content,
                    file.ContentType,
                    file.FileName,
                    enableRangeProcessing: true);
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("DownloadFile");

        group.MapGet("/", async (
                string? relatedEntity,
                string? relatedId,
                IFileStorage fileStorage,
                IPermissionService permissionService,
                CancellationToken cancellationToken) =>
            {
                if (!await permissionService.HasAccessAsync(Model, OperationType.Read, cancellationToken))
                {
                    return Results.Forbid();
                }

                return Results.Ok(await fileStorage.ListAsync(relatedEntity, relatedId, cancellationToken));
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("ListFiles");

        group.MapDelete("/{fileId:guid}", async (
                Guid fileId,
                IFileStorage fileStorage,
                IPermissionService permissionService,
                CancellationToken cancellationToken) =>
            {
                if (!await permissionService.HasAccessAsync(Model, OperationType.Unlink, cancellationToken))
                {
                    return Results.Forbid();
                }

                return await fileStorage.DeleteAsync(fileId, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound(new { fileId, message = "File not found." });
            })
            .RequireAuthorization(GatewayPolicies.AuthenticatedUser)
            .WithName("DeleteFile");

        return endpoints;
    }
}