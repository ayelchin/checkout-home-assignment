using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

[Collection("Bank simulator")]
[Trait("Dependency", "Docker")]
public sealed class BankSimulatorTests(BankSimulatorFixture simulator)
{
    [Theory]
    [InlineData("0000000000000011", "Authorized", 0)]
    [InlineData("0000000000000012", "Declined", 1)]
    [InlineData("0000000000000010", "Declined", 2)]
    public async Task PostPaymentAsync_WhenSimulatorReturnsOutcome_ShouldCreateRetrievablePayment(string card, string status, int reason)
    {
        // ARRANGE
        using GatewayFactory factory = new(simulator.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await client.PostAsync("/api/payments", PaymentRequests.Json(card));
        using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
        JsonNode stored = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            Created = HttpStatusCode.Created,
            Retrieved = HttpStatusCode.OK,
            Status = status,
            FailureReason = reason,
            LastFour = card[^4..]
        };
        var actual = new
        {
            Created = created.StatusCode,
            Retrieved = retrieved.StatusCode,
            Status = (string?)stored["status"],
            FailureReason = (int)stored["failure_reason"]!,
            LastFour = (string?)stored["cardNumberLastFour"]
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenCreated_ShouldReturnLocationOfPayment()
    {
        // ARRANGE
        using GatewayFactory factory = new(simulator.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await client.PostAsync("/api/payments", PaymentRequests.Json("0000000000000011"));
        JsonNode posted = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;

        // ASSERT
        string expected = $"/api/payments/{posted["id"]}";
        string? actual = created.Headers.Location?.ToString();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenCreated_ShouldReturnSamePaymentAsGet()
    {
        // ARRANGE
        using GatewayFactory factory = new(simulator.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await client.PostAsync("/api/payments", PaymentRequests.Json("0000000000000011"));
        JsonNode posted = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;
        using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
        JsonNode stored = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;

        // ASSERT
        bool expected = true;
        bool actual = JsonNode.DeepEquals(posted, stored);
        Assert.Equal(expected, actual);
    }
}