using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DistributedChat.Api.Http;

public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment
) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        LogUnhandledException(logger, exception, httpContext.TraceIdentifier);

        var problemDetails = new ProblemDetails
        {
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
        };

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails,
                Exception = exception,
            });
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled exception while processing request {TraceIdentifier}.")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string traceIdentifier);
}
