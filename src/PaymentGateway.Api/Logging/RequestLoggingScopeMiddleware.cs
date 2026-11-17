namespace PaymentGateway.Api.Logging;

public sealed class RequestLoggingScopeMiddleware(RequestDelegate next, ILogger<RequestLoggingScopeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        finally
        {
            if (context.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation(LogEvents.ClientDisconnected, "HTTP client disconnected");
            }
        }
    }
}
