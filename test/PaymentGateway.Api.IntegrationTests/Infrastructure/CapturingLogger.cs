using System.Collections.Concurrent;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

internal sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLog> entries,
    Func<IExternalScopeProvider> scopes, Action<CapturedLog>? onLogged) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => scopes().Push(state);

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Dictionary<string, object?> fields = new();
        scopes().ForEachScope((scope, values) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
            {
                foreach (KeyValuePair<string, object?> property in properties)
                {
                    values[property.Key] = property.Value;
                }
            }
        }, fields);
        if (state is IEnumerable<KeyValuePair<string, object?>> properties)
        {
            foreach (KeyValuePair<string, object?> property in properties)
            {
                fields[property.Key] = property.Value;
            }
        }
        CapturedLog entry = new(category, logLevel, eventId, formatter(state, exception), exception, fields);
        entries.Enqueue(entry);
        onLogged?.Invoke(entry);
    }
}