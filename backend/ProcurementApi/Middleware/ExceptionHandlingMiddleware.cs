using System.Net;
using System.Text.Json;
using ProcurementApi.Services;

namespace ProcurementApi.Middleware;

/// <summary>
/// Single place all unhandled exceptions are translated into HTTP responses. Nothing else
/// in the app should write try/catch-and-map-to-status-code code (Section 8, docs/ANALYSIS.md).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            var (status, title) = ex switch
            {
                NotFoundException => (HttpStatusCode.NotFound, ex.Message),
                ValidationException => (HttpStatusCode.BadRequest, ex.Message),
                InvalidCredentialsException => (HttpStatusCode.Unauthorized, ex.Message),
                ForbiddenException => (HttpStatusCode.Forbidden, ex.Message),
                InvalidStateTransitionException => (HttpStatusCode.Conflict, ex.Message),
                ConcurrencyConflictException => (HttpStatusCode.Conflict, ex.Message),
                ExternalServiceException => (HttpStatusCode.ServiceUnavailable, ex.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            if (status == HttpStatusCode.InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            }
            else
            {
                _logger.LogWarning("{ExceptionType} on {Method} {Path}: {Message}", ex.GetType().Name, context.Request.Method, context.Request.Path, ex.Message);
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)status;

            var problem = new
            {
                status = (int)status,
                title,
                traceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
