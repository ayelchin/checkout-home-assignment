using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.IntegrationTests.Infrastructure;
using PaymentGateway.Api.Repositories.Payments;

namespace PaymentGateway.Api.IntegrationTests.UseCases.GetPayment;

public sealed class GetPaymentTests
{
    [Fact]
    public async Task GetPayment_WhenBankAuthorizationIsPending_ShouldReturnInProgressAndThenFinalOutcome()
    {
        // ARRANGE
        TaskCompletionSource releaseBank = new(TaskCreationOptions.RunContinuationsAsynchronously);
        InMemoryPaymentsRepository storage = new(TimeProvider.System);
        Mock<IPaymentsRepository> repository = new();
        TaskCompletionSource<PaymentRecord> initial = new(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.Setup(store => store.Add(It.IsAny<PaymentRecord>()))
            .Callback<PaymentRecord>(payment =>
            {
                storage.Add(payment);
                initial.TrySetResult(payment);
            });
        repository.Setup(store => store.Update(It.IsAny<PaymentRecord>()))
            .Callback<PaymentRecord>(storage.Update);
        repository.Setup(store => store.Get(It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns((string merchantId, Guid id) => storage.Get(merchantId, id));
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            await releaseBank.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await context.Response.WriteAsJsonAsync(new { authorized = true, authorization_code = "approval" });
        });
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentGateway.Api.Controllers.PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentsRepository>();
                services.AddSingleton<IPaymentsRepository>(repository.Object);
            }));
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        Task<HttpResponseMessage> postTask = PaymentRequests.SendAsync(client);
        try
        {
            PaymentRecord initialPayment = await initial.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage pendingResponse = await client.GetAsync($"/api/payments/{initialPayment.Id}");
            JsonNode pendingBody = (await pendingResponse.Content.ReadFromJsonAsync<JsonNode>())!;
            releaseBank.TrySetResult();
            using HttpResponseMessage created = await postTask.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage completedResponse = await client.GetAsync($"/api/payments/{initialPayment.Id}");
            JsonNode completedBody = (await completedResponse.Content.ReadFromJsonAsync<JsonNode>())!;
            PaymentRecord storedPayment = storage.Get(initialPayment.MerchantId, initialPayment.Id)!;

            // ASSERT
            var expected = new
            {
                Pending = "InProgress",
                Completed = "Authorized",
                SameId = initialPayment.Id.ToString(),
                CardNumberLastFour = "0011",
                ContainsFullCardNumber = false,
                ContainsSensitiveFields = false
            };
            var actual = new
            {
                Pending = (string?)pendingBody["status"],
                Completed = (string?)completedBody["status"],
                SameId = (string?)completedBody["id"],
                storedPayment.CardNumberLastFour,
                ContainsFullCardNumber = storedPayment.ToString()!.Contains("0000000000000011", StringComparison.Ordinal),
                ContainsSensitiveFields = storedPayment.GetType().GetProperties()
                    .Any(property => property.Name is "CardNumber" or "Cvv" or "Details")
            };
            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            releaseBank.TrySetResult();
        }
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
