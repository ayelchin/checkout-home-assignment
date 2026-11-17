using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

[Collection("Bank simulator")]
public sealed class AuthorizedTests(BankSimulatorFixture simulator)
{
    [Fact]
    [Trait("Dependency", "Docker")]
    public async Task PostPaymentAsync_WhenBankAuthorizes_ShouldCreateRetrievableAuthorization()
    {
        // ARRANGE
        using GatewayFactory factory = new(await simulator.GetAddressAsync());
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await client.PostAsync("/api/payments", PaymentRequests.Json());
        JsonNode posted = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;
        using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
        JsonNode stored = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            Created = HttpStatusCode.Created,
            Retrieved = HttpStatusCode.OK,
            Location = $"/api/payments/{posted["id"]}",
            Status = "Authorized",
            FailureReason = 0,
            LastFour = "0011",
            SamePayment = true,
            ExposesCardOrCvv = false
        };
        var actual = new
        {
            Created = created.StatusCode,
            Retrieved = retrieved.StatusCode,
            Location = created.Headers.Location?.ToString(),
            Status = (string?)stored["status"],
            FailureReason = (int)stored["failure_reason"]!,
            LastFour = (string?)stored["cardNumberLastFour"],
            SamePayment = JsonNode.DeepEquals(posted, stored),
            ExposesCardOrCvv = stored.AsObject().ContainsKey("cardNumber") || stored.AsObject().ContainsKey("cvv")
        };
        actual.Should().BeEquivalentTo(expected);
    }
}
