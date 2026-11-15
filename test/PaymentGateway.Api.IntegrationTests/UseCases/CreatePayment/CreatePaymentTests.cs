using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

public sealed class CreatePaymentTests
{
    [Fact]
    public async Task PostPaymentAsync_WhenDetailsFailPaymentRules_ShouldCreateRetrievableRejection()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await client.PostAsync("/api/payments", PaymentRequests.Json(amount: 0));
        using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
        JsonNode body = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            Created = HttpStatusCode.Created,
            Retrieved = HttpStatusCode.OK,
            Status = "Rejected",
            FailureReason = 6,
            Error = "amount_invalid"
        };
        var actual = new
        {
            Created = created.StatusCode,
            Retrieved = retrieved.StatusCode,
            Status = (string?)body["status"],
            FailureReason = (int)body["failure_reason"]!,
            Error = (string?)body["errors"]![0]!["code"]
        };
        actual.Should().BeEquivalentTo(expected);
    }
}