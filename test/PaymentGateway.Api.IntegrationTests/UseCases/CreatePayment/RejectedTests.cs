using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

public sealed class RejectedTests
{
    [Fact]
    public async Task PostPaymentAsync_WhenApiKeyIsMissing_ShouldReturnUnauthorized()
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateClient();

        // ACT
        using HttpResponseMessage response = await client.PostAsync("/api/payments", PaymentRequests.Json());

        // ASSERT
        HttpStatusCode expected = HttpStatusCode.Unauthorized;
        HttpStatusCode actual = response.StatusCode;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenDetailsFailPaymentRules_ShouldCreateRetrievableRejection()
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
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
            Error = "amount_invalid",
            BankRequests = 0
        };
        var actual = new
        {
            Created = created.StatusCode,
            Retrieved = retrieved.StatusCode,
            Status = (string?)body["status"],
            FailureReason = (int)body["failure_reason"]!,
            Error = (string?)body["errors"]![0]!["code"],
            BankRequests = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("0000000000011", HttpStatusCode.UnprocessableEntity, "payment_source_invalid")]
    [InlineData("00000000000011", HttpStatusCode.Created, null)]
    [InlineData("0000000000000000011", HttpStatusCode.Created, null)]
    [InlineData("00000000000000000011", HttpStatusCode.UnprocessableEntity, "payment_source_invalid")]
    [InlineData("000000000000001\u0661", HttpStatusCode.UnprocessableEntity, "payment_source_invalid")]
    public async Task PostPaymentAsync_WhenCardNumberFormatIsAtBoundary_ShouldApplyValidationContract(
        string cardNumber, HttpStatusCode status, string? errorCode)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage response = await client.PostAsync("/api/payments", PaymentRequests.Json(cardNumber));
        JsonNode body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            Status = status,
            ErrorCode = errorCode
        };
        var actual = new
        {
            Status = response.StatusCode,
            ErrorCode = (string?)body["error_codes"]?[0]
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("{\"cardNumber\":1234567890123456}")]
    public async Task PostPaymentAsync_WhenBodyCannotBind_ShouldReturnSafeEnvelope(string input)
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();
        const string expectedJson = """{"error_type":"request_invalid","error_codes":["request_body_invalid"]}""";

        // ACT
        using HttpResponseMessage response = await client.PostAsync("/api/payments", PaymentRequests.Body(input));
        JsonNode? body = JsonNode.Parse(await response.Content.ReadAsStringAsync());

        // ASSERT
        var expected = new
        {
            Status = HttpStatusCode.UnprocessableEntity,
            BodyMatches = true
        };
        var actual = new
        {
            Status = response.StatusCode,
            BodyMatches = JsonNode.DeepEquals(JsonNode.Parse(expectedJson), body)
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("cardNumber", null, "payment_source_required")]
    [InlineData("cardNumber", "\"123\"", "payment_source_invalid")]
    [InlineData("cardNumber", "\"0000000000000011\\n\"", "payment_source_invalid")]
    [InlineData("cvv", "\" \"", "cvv_required")]
    [InlineData("expiryMonth", null, "expiry_month_required")]
    [InlineData("expiryYear", null, "expiry_year_required")]
    [InlineData("currency", null, "currency_required")]
    [InlineData("amount", null, "amount_required")]
    [InlineData("amount", "\"100\"", "request_body_invalid")]
    [InlineData("amount", "1.5", "request_body_invalid")]
    [InlineData("amount", "2147483648", "request_body_invalid")]
    public async Task PostPaymentAsync_WhenRequiredFieldOrTypeIsInvalid_ShouldReturnSafeCode(string field, string? value, string code)
    {
        // ARRANGE
        using GatewayFactory factory = new();
        using HttpClient client = factory.CreateMerchantClient();
        JsonObject input = JsonNode.Parse(PaymentRequests.Valid)!.AsObject();
        if (value is null)
        {
            input.Remove(field);
        }
        else
        {
            input[field] = JsonNode.Parse(value);
        }

        // ACT
        using HttpResponseMessage response = await client.PostAsync("/api/payments", PaymentRequests.Body(input.ToJsonString()));
        JsonNode body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            Status = HttpStatusCode.UnprocessableEntity,
            ErrorType = "request_invalid",
            Code = code,
            Fields = 2
        };
        var actual = new
        {
            Status = response.StatusCode,
            ErrorType = (string?)body["error_type"],
            Code = string.Join(",", body["error_codes"]!.AsArray().Select(item => (string?)item)),
            Fields = body.AsObject().Count
        };
        actual.Should().BeEquivalentTo(expected);
    }
}
