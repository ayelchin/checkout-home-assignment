using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.OpenApi;

public sealed class OpenApiTests
{
    [Fact]
    public async Task GetSwagger_WhenRequested_ShouldDocumentApiKey()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();

        // ACT
        JsonNode document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;

        // ASSERT
        string expected = "X-Api-Key";
        string? actual = (string?)document["components"]!["securitySchemes"]!["ApiKey"]!["name"];
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetSwagger_WhenRequested_ShouldDocumentIdempotencyKey()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();

        // ACT
        JsonNode document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;
        JsonNode post = document["paths"]!["/api/Payments"]!["post"]!;
        JsonNode key = post["parameters"]!.AsArray().Single(parameter => (string?)parameter!["name"] == "Idempotency-Key")!;

        // ASSERT
        var expected = new
        {
            Location = "header",
            Required = false,
            MaxLength = 255
        };
        var actual = new
        {
            Location = (string?)key["in"],
            Required = (bool?)key["required"] ?? false,
            MaxLength = (int)key["schema"]!["maxLength"]!
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetSwagger_WhenRequested_ShouldDocumentPendingAndUnknownAttemptOutcomes()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();

        // ACT
        JsonNode document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;
        JsonNode post = document["paths"]!["/api/Payments"]!["post"]!;

        // ASSERT
        var expected = new { Pending = true, UnknownReason = true, KeyOnlyReplay = true };
        var actual = new
        {
            Pending = ((string?)post["responses"]!["201"]!["description"])!.Contains("InProgress", StringComparison.Ordinal),
            UnknownReason = document.ToJsonString().Contains("BankUnavailable", StringComparison.Ordinal),
            KeyOnlyReplay = ((string?)post["parameters"]!.AsArray()
                .Single(parameter => (string?)parameter!["name"] == "Idempotency-Key")!["description"])!
                .Contains("even if payment details differ", StringComparison.Ordinal)
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("201", "#/components/schemas/CreatePaymentResponse")]
    [InlineData("422", "#/components/schemas/RequestInvalidResponse")]
    public async Task GetSwagger_WhenRequested_ShouldDocumentResponseSchema(string status, string expected)
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();

        // ACT
        JsonNode document = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;

        // ASSERT
        string? actual = (string?)document["paths"]!["/api/Payments"]!["post"]!["responses"]![status]!["content"]!["application/json"]!["schema"]!["$ref"];
        Assert.Equal(expected, actual);
    }
}