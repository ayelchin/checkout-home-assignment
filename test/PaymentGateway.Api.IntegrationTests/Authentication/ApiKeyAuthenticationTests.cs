using System.Net;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.Authentication;

public sealed class ApiKeyAuthenticationTests
{
    [Theory]
    [InlineData("POST", null)]
    [InlineData("POST", "wrong-key")]
    [InlineData("GET", null)]
    [InlineData("GET", "wrong-key")]
    [InlineData("GET", "MERCHANT-ONE-KEY")]
    public async Task AuthenticateAsync_WhenKeyIsMissingOrInvalid_ShouldReturnUnauthorized(string method, string? key)
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = new(new HttpMethod(method), method == "POST" ? "/api/payments" : "/api/payments/11111111-1111-1111-1111-111111111111");
        request.Content = PaymentRequests.Body(PaymentRequests.Valid);
        if (key is not null)
        {
            request.Headers.Add("X-Api-Key", key);
        }

        // ACT
        using HttpResponseMessage response = await client.SendAsync(request);

        // ASSERT
        HttpStatusCode expected = HttpStatusCode.Unauthorized;
        HttpStatusCode actual = response.StatusCode;
        Assert.Equal(expected, actual);
    }
}