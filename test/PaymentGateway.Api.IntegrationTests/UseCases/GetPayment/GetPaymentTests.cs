using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.IntegrationTests.Infrastructure;
using PaymentGateway.Api.Repositories.Payments;

namespace PaymentGateway.Api.IntegrationTests.UseCases.GetPayment;

public sealed class GetPaymentTests
{
    [Fact]
    public async Task GetPaymentAsync_WhenApiKeyIsMissing_ShouldReturnUnauthorized()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();

        // ACT
        using HttpResponseMessage response = await client.GetAsync("/api/payments/11111111-1111-1111-1111-111111111111");

        // ASSERT
        HttpStatusCode expected = HttpStatusCode.Unauthorized;
        HttpStatusCode actual = response.StatusCode;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetAsync_WhenUrlDoesNotMatchAnyRoute_ShouldReturnNotFound()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage response = await client.GetAsync("/api/does-not-exist");

        // ASSERT
        HttpStatusCode expected = HttpStatusCode.NotFound;
        HttpStatusCode actual = response.StatusCode;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetPayment_WhenBankAuthorizationIsPending_ShouldReturnInProgress()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, new DateTimeOffset(2030, 11, 15, 0, 0, 0, TimeSpan.Zero));
        factory.Services.GetRequiredService<IPaymentsRepository>().Add(payment);

        // ACT
        JsonNode? body = await client.GetFromJsonAsync<JsonNode>($"/api/payments/{payment.Id}");

        // ASSERT
        string expected = "InProgress";
        string? actual = (string?)body?["status"];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetPayment_WhenPaymentIsOwned_ShouldReturnSafeContract()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();
        using HttpResponseMessage created = await client.PostAsync("/api/payments", PaymentRequests.Json(amount: 0));

        // ACT
        using HttpResponseMessage response = await client.GetAsync(created.Headers.Location);
        JsonNode body = (await response.Content.ReadFromJsonAsync<JsonNode>())!;

        // ASSERT
        var expected = new
        {
            HttpStatus = HttpStatusCode.OK,
            CardNumberLastFour = "0011",
            ExpiryMonth = 12,
            ExpiryYear = 2031,
            Currency = "GBP",
            Amount = 0,
            Status = "Rejected",
            FailureReason = 6,
            Fields = "amount,cardNumberLastFour,currency,errors,expiryMonth,expiryYear,failure_reason,id,status"
        };
        var actual = new
        {
            HttpStatus = response.StatusCode,
            CardNumberLastFour = (string?)body["cardNumberLastFour"],
            ExpiryMonth = (int)body["expiryMonth"]!,
            ExpiryYear = (int)body["expiryYear"]!,
            Currency = (string?)body["currency"],
            Amount = (int)body["amount"]!,
            Status = (string?)body["status"],
            FailureReason = (int)body["failure_reason"]!,
            Fields = string.Join(",", body.AsObject().Select(property => property.Key).OrderBy(key => key, StringComparer.Ordinal))
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPayment_WhenPaymentIsForeign_ShouldReturnNotFound()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient owner = factory.CreateMerchantClient();
        using HttpClient other = factory.CreateMerchantClient("merchant-two-key");
        using HttpResponseMessage created = await owner.PostAsync("/api/payments", PaymentRequests.Json(amount: 0));
        other.DefaultRequestHeaders.Add("X-Merchant-Id", "merchant-one");

        // ACT
        using HttpResponseMessage foreign = await other.GetAsync($"{created.Headers.Location}?merchantId=merchant-one");

        // ASSERT
        HttpStatusCode expected = HttpStatusCode.NotFound;
        HttpStatusCode actual = foreign.StatusCode;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetPayment_WhenPaymentIsMissing_ShouldReturnNotFound()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage response = await client.GetAsync("/api/payments/22222222-2222-2222-2222-222222222222");

        // ASSERT
        HttpStatusCode expected = HttpStatusCode.NotFound;
        HttpStatusCode actual = response.StatusCode;
        Assert.Equal(expected, actual);
    }
}
