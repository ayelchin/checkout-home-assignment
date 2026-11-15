namespace PaymentGateway.Api.UnitTests.Infrastructure;

internal sealed class BankHttpHandlerStub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return send(request, cancellationToken);
    }
}