using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SocialFeed.Api;

/// <summary>
/// Turns anything that escapes a controller into the same RFC 7807 body every other error
/// uses, so a client never has to parse a framework error page.
/// <para>
/// The response deliberately says nothing about what went wrong: exception messages leak
/// table names, file paths and connection details. The full exception goes to the log,
/// where it is useful and not public.
/// </para>
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Something went wrong",
                Detail = "The request could not be completed. Please try again."
            },
            cancellationToken);

        return true;
    }
}
