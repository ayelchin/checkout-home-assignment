using System.Net;

using AwesomeAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
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

public sealed class PaymentLifecycleTests
{
    [Fact]
    public async Task PostPaymentAsync_WhenValidatorThrows_ShouldRetainInitialPaymentAndReleaseKeyForRetry()
    {
        // ARRANGE
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
            OriginalStatus = (string?)System.Text.Json.Nodes.JsonNode.Parse(originalBody)!["status"],
            DifferentIds = retained[0].Id != retained[1].Id,
            BankCalls = bank.Requests.Count
        };
        actual.Should().BeEquivalentTo(expected);
    }
}