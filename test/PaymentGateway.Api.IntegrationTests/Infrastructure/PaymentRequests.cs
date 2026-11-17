using System.Text;
using System.Text.Json;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

internal static class PaymentRequests
{
    public const string Valid = """{"cardNumber":"0000000000000011","cvv":"123","expiryMonth":12,"expiryYear":2031,"currency":"GBP","amount":100}""";

    public static StringContent Json(string cardNumber = "0000000000000011", int amount = 100)
    {
        return Body(JsonSerializer.Serialize(new { cardNumber, cvv = "123", expiryMonth = 12, expiryYear = 2031, currency = "GBP", amount }));
    }

    public static StringContent Body(string body) => new(body, Encoding.UTF8, "application/json");

    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, string? key = "attempt", string body = Valid,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "/api/payments") { Content = Body(body) };
        if (key is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        }
        return await client.SendAsync(request, cancellationToken);
    }
}