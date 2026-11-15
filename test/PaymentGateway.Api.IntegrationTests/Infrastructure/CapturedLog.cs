namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

internal sealed record CapturedLog(string Category, LogLevel Level, EventId EventId, string Message,
    Exception? Exception, IReadOnlyDictionary<string, object?> Fields);