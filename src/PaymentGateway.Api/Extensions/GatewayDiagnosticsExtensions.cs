using Microsoft.AspNetCore.HttpLogging;

using PaymentGateway.Api.Logging;

namespace PaymentGateway.Api.Extensions;

public static class GatewayDiagnosticsExtensions
{
    public static IServiceCollection AddGatewayDiagnostics(this IServiceCollection services)
    {
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });
        services.AddProblemDetails();
        services.AddExceptionHandler<ApplicationExceptionHandler>();

        return services;
    }
}