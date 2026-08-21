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
        // A client that navigates away cancels the request, which cancels the query. That is
        // normal traffic, not a failure, and there is no longer a connection to answer on.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request {Method} {Path} was cancelled by the client.",
                httpContext.Request.Method,
                httpContext.Request.Path);

            return true;
        }

        _logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        // Once writing has begun the status code is already on the wire and changing it
        // throws. Returning false lets the framework abort the connection instead of raising
        // a second exception inside the exception handler.
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

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
