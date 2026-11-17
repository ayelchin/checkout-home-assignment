using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.IntegrationTests.Infrastructure;
using PaymentGateway.Api.Repositories.Payments;
using PaymentGateway.Api.Services.PaymentValidation;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

public sealed class IdempotencyTests
{
    [Theory]
    [InlineData("123", 100)]
    [InlineData("invalid", 100)]
    [InlineData("123", 200)]
    public async Task PostPaymentAsync_WhenKeyMatches_ShouldReplayOriginalPayment(string cvv, int amount)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();
        JsonNode retry = JsonNode.Parse(PaymentRequests.Valid)!;
        retry["cvv"] = cvv;
        retry["amount"] = amount;

        // ACT
        using HttpResponseMessage first = await PaymentRequests.SendAsync(client);
        using HttpResponseMessage second = await PaymentRequests.SendAsync(client, body: retry.ToJsonString());
        string firstBody = await first.Content.ReadAsStringAsync();
        string secondBody = await second.Content.ReadAsStringAsync();

        // ASSERT
        var expected = new
        {
            First = HttpStatusCode.Created,
            Second = HttpStatusCode.Created,
            Location = first.Headers.Location,
            Body = firstBody,
            BankRequests = 1
        };
        var actual = new
        {
            First = first.StatusCode,
            Second = second.StatusCode,
            Location = second.Headers.Location,
            Body = secondBody,
            BankRequests = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenRejectedPaymentKeyIsReused_ShouldReturnOriginalPayment()
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();
        JsonNode invalidRequest = JsonNode.Parse(PaymentRequests.Valid)!;
        invalidRequest["cvv"] = "12";

        // ACT
        using HttpResponseMessage rejected = await PaymentRequests.SendAsync(client, body: invalidRequest.ToJsonString());
        using HttpResponseMessage retry = await PaymentRequests.SendAsync(client);
        string originalBody = await rejected.Content.ReadAsStringAsync();
        string retryBody = await retry.Content.ReadAsStringAsync();

        // ASSERT
        var expected = new
        {
            Status = HttpStatusCode.Created,
            Location = rejected.Headers.Location,
            Body = originalBody,
            BankRequests = 0
        };
        var actual = new
        {
            Status = retry.StatusCode,
            Location = retry.Headers.Location,
            Body = retryBody,
            BankRequests = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task PostPaymentAsync_WhenKeyIsMissingOrBlank_ShouldCreateIndependentAttempts(string? key)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage first = await PaymentRequests.SendAsync(client, key);
        using HttpResponseMessage second = await PaymentRequests.SendAsync(client, key);

        // ASSERT
        var expected = new
        {
            First = HttpStatusCode.Created,
            Second = HttpStatusCode.Created,
            DifferentLocations = true,
            BankRequests = 2
        };
        var actual = new
        {
            First = first.StatusCode,
            Second = second.StatusCode,
            DifferentLocations = first.Headers.Location != second.Headers.Location,
            BankRequests = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(255, HttpStatusCode.Created)]
    [InlineData(256, HttpStatusCode.UnprocessableEntity)]
    public async Task PostPaymentAsync_WhenKeyIsAtLengthBoundary_ShouldApplyLimit(int length, HttpStatusCode expected)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client, new string('k', length));

        // ASSERT
        HttpStatusCode actual = response.StatusCode;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenDifferentMerchantsUseSameKey_ShouldCreateIndependentPayments()
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient firstMerchant = factory.CreateMerchantClient();
        using HttpClient secondMerchant = factory.CreateMerchantClient("merchant-two-key");

        // ACT
        using HttpResponseMessage first = await PaymentRequests.SendAsync(firstMerchant);
        using HttpResponseMessage second = await PaymentRequests.SendAsync(secondMerchant);

        // ASSERT
        var expected = new
        {
            First = HttpStatusCode.Created,
            Second = HttpStatusCode.Created,
            DifferentLocations = true,
            BankRequests = 2
        };
        var actual = new
        {
            First = first.StatusCode,
            Second = second.StatusCode,
            DifferentLocations = first.Headers.Location != second.Headers.Location,
            BankRequests = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenMatchingRequestsOverlap_ShouldWaitAndReturnOnePayment()
    {
        // ARRANGE
        TaskCompletionSource bankArrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource duplicateReachedActionPipeline = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            bankArrived.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await context.Response.WriteAsJsonAsync(new { authorized = true });
        });
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Configure<MvcOptions>(options => options.Filters.Add(new RequestArrivalFilter(duplicateReachedActionPipeline)))));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        Task<HttpResponseMessage> first = PaymentRequests.SendAsync(client);
        try
        {
            await bankArrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Task<HttpResponseMessage> duplicate = PaymentRequests.SendAsync(client);
            await duplicateReachedActionPipeline.Task.WaitAsync(TimeSpan.FromSeconds(10));
            bool duplicateIsStillWaiting = !duplicate.IsCompleted;
            release.TrySetResult();
            using HttpResponseMessage original = await first.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage duplicateResponse = await duplicate.WaitAsync(TimeSpan.FromSeconds(10));
            string originalBody = await original.Content.ReadAsStringAsync();
            string duplicateBody = await duplicateResponse.Content.ReadAsStringAsync();

            // ASSERT
            var expected = new
            {
                DuplicateIsStillWaiting = true,
                Original = HttpStatusCode.Created,
                Duplicate = HttpStatusCode.Created,
                Location = original.Headers.Location,
                Body = originalBody,
                BankRequests = 1
            };
            var actual = new
            {
                DuplicateIsStillWaiting = duplicateIsStillWaiting,
                Original = original.StatusCode,
                Duplicate = duplicateResponse.StatusCode,
                Location = duplicateResponse.Headers.Location,
                Body = duplicateBody,
                BankRequests = bank.Requests.Count
            };
            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostPaymentAsync_WhenRequestValidationFails_ShouldLeaveKeyReusable()
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bankAddress: bank.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage invalid = await PaymentRequests.SendAsync(client, body: "{}");
        using HttpResponseMessage corrected = await PaymentRequests.SendAsync(client);

        // ASSERT
        var expected = new
        {
            Invalid = HttpStatusCode.UnprocessableEntity,
            Corrected = HttpStatusCode.Created,
            BankRequests = 1
        };
        var actual = new
        {
            Invalid = invalid.StatusCode,
            Corrected = corrected.StatusCode,
            BankRequests = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenValidatorThrows_ShouldRetainInitialPaymentAndReleaseKeyForRetry()
    {
        // ARRANGE
        // This unexpected failure cannot be triggered by ordinary HTTP input.
        InMemoryPaymentsRepository storage = new(TimeProvider.System);
        List<PaymentRecord> retained = [];
        Mock<IPaymentsRepository> repository = new();
        repository.Setup(store => store.Add(It.IsAny<PaymentRecord>()))
            .Callback<PaymentRecord>(payment =>
            {
                retained.Add(payment);
                storage.Add(payment);
            });
        repository.Setup(store => store.Update(It.IsAny<PaymentRecord>()))
            .Callback<PaymentRecord>(storage.Update);
        repository.Setup(store => store.Get(It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns((string merchantId, Guid id) => storage.Get(merchantId, id));
        Mock<IPaymentDetailsValidator> validator = new();
        validator.SetupSequence(service => service.Validate(It.IsAny<PaymentDetailsInput>()))
            .Throws(new InvalidOperationException("Simulated validation failure"))
            .Returns(new List<ErrorState>().AsReadOnly());
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentsRepository>();
                services.AddSingleton(repository.Object);
                services.RemoveAll<IPaymentDetailsValidator>();
                services.AddSingleton(validator.Object);
            }));
        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage failed = await PaymentRequests.SendAsync(client);
        using HttpResponseMessage retry = await PaymentRequests.SendAsync(client);
        using HttpResponseMessage original = await client.GetAsync($"/api/payments/{retained[0].Id}");
        string originalBody = await original.Content.ReadAsStringAsync();

        // ASSERT
        var expected = new
        {
            First = HttpStatusCode.InternalServerError,
            Retry = HttpStatusCode.Created,
            RetainedCount = 2,
            Original = HttpStatusCode.OK,
            OriginalStatus = "InProgress",
            DifferentIds = true,
            BankCalls = 1
        };
        var actual = new
        {
            First = failed.StatusCode,
            Retry = retry.StatusCode,
            RetainedCount = retained.Count,
            Original = original.StatusCode,
            OriginalStatus = (string?)JsonNode.Parse(originalBody)!["status"],
            DifferentIds = retained[0].Id != retained[1].Id,
            BankCalls = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }
}
