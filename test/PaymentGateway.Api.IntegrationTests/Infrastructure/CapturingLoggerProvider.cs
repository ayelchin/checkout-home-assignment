using System.Collections.Concurrent;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

internal sealed class CapturingLoggerProvider(Action<CapturedLog>? onLogged = null) : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
    public ConcurrentQueue<CapturedLog> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries, () => _scopes, onLogged);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose() { }
}