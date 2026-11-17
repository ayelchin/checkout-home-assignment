using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.UnitTests.Infrastructure;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.UnitTests.Services.Bank;

public sealed class AcquiringBankClientTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenCalled_ShouldSendBankContract()
    {
        // ARRANGE
        JsonNode? sent = null;
        string? method = null;
        string? path = null;
        using BankHttpHandlerStub transport = new(async (request, _) =>
        {
            method = request.Method.Method;
            path = request.RequestUri!.AbsolutePath;
            sent = JsonNode.Parse(await request.Content!.ReadAsStringAsync());
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"authorized\":true}") };
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("http://bank.test") };
        AcquiringBankService client = CreateClient(http);
        const string expectedJson = """{"card_number":"0000000000000011","cvv":"123","expiry_date":"12/2031","currency":"GBP","amount":100}""";

        // ACT
        await client.AuthorizeAsync(Payments.Details());

        // ASSERT
        var expected = new
        {
            Method = "POST",
            Path = "/payments",
            BodyMatches = true
        };
        var actual = new
        {
            Method = method,
            Path = path,
            BodyMatches = JsonNode.DeepEquals(JsonNode.Parse(expectedJson), sent)
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(true, PaymentStatus.Authorized, PaymentFailureReason.None)]
    [InlineData(false, PaymentStatus.Declined, PaymentFailureReason.BankDeclined)]
    public async Task AuthorizeAsync_WhenBankResponds_ShouldReturnWholeResponse(bool authorized, PaymentStatus status, PaymentFailureReason failureReason)
    {
        // ARRANGE
        using BankHttpHandlerStub transport = new((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{{\"authorized\":{authorized.ToString().ToLowerInvariant()},\"authorization_code\":\"approval\"}}")
        }));
        using HttpClient http = new(transport) { BaseAddress = new Uri("http://bank.test") };
        AcquiringBankService client = CreateClient(http);

        // ACT
        BankAuthorizationResult actual = await client.AuthorizeAsync(Payments.Details());

        // ASSERT
        BankAuthorizationResult expected = new(status, failureReason, new AuthorizationResponse(authorized, "approval"));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("transport")]
    [InlineData("timeout")]
    [InlineData("unexpected")]
    [InlineData("invalid-json")]
    [InlineData("null-json")]
    [InlineData("http-error")]
    public async Task AuthorizeAsync_WhenBankFails_ShouldReturnBankUnavailable(string failure)
    {
        // ARRANGE
        using BankHttpHandlerStub transport = new((_, _) => failure switch
        {
            "transport" => throw new HttpRequestException("Connection failed"),
            "timeout" => throw new TaskCanceledException("Timed out"),
            "unexpected" => throw new InvalidOperationException("Unexpected failure"),
            "invalid-json" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("invalid") }),
            "null-json" => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") }),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))
        });
        using HttpClient http = new(transport) { BaseAddress = new Uri("http://bank.test") };
        AcquiringBankService client = CreateClient(http);

        // ACT
        BankAuthorizationResult actual = await client.AuthorizeAsync(Payments.Details());

        // ASSERT
        BankAuthorizationResult expected = new(PaymentStatus.Declined, PaymentFailureReason.BankUnavailable);
        Assert.Equal(expected, actual);
    }

    private static AcquiringBankService CreateClient(HttpClient http)
    {
        return new(http, Options.Create(new BankOptions()), new FixedTimeProvider(), NullLogger<AcquiringBankService>.Instance);
    }
}