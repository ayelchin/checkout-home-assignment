using Microsoft.AspNetCore.Diagnostics;

namespace PaymentGateway.Api.Logging;

public sealed class ApplicationExceptionHandler(ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Exception messages may contain submitted card data. Log diagnostic metadata instead.
        logger.LogError(LogEvents.UnhandledException,
            "Unhandled {ExceptionType}. Stack trace: {StackTrace}", exception.GetType().FullName, exception.StackTrace);
        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.").ExecuteAsync(httpContext);
        return true;
    }
}