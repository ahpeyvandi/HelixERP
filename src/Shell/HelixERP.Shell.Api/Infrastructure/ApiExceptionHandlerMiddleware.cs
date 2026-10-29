using System.Text.Json;
using HelixERP.BuildingBlocks.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace HelixERP.Shell.Api.Infrastructure;

public sealed class ApiExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlerMiddleware> _logger;

    public ApiExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, title) = exception switch
            {
                DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Not found"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
            };

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception for {Method} {Path}.", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning(
                    "Request {Method} {Path} rejected with {StatusCode}: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    exception.Message);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred. Check the server logs for details."
                    : exception.Message,
                Instance = context.Request.Path
            };

            var payload = JsonSerializer.Serialize(
                problem,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(payload, context.RequestAborted);
        }
    }
}