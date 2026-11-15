using System.Diagnostics;

namespace PaymentGateway.Api.Logging;

public sealed class RequestLoggingScopeMiddleware(RequestDelegate next, ILogger<RequestLoggingScopeMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier
        });
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