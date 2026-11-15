using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

public sealed class BankFailureTests
{
    [Theory]
    [InlineData(503)]
    [InlineData(500)]
    [InlineData(307)]
    public async Task PostPaymentAsync_WhenBankFailsOrRedirects_ShouldStoreUnavailable(int statusCode)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync(context =>
        {
            context.Response.StatusCode = statusCode;
            context.Response.Headers.Location = $"http://{context.Request.Host}/payments";
            return Task.CompletedTask;
        });
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await PaymentRequests.SendAsync(client);
        using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
        JsonNode stored = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            Created = HttpStatusCode.Created,
            Retrieved = HttpStatusCode.OK,
            Status = "Declined",
            FailureReason = 2
        };
        var actual = new
        {
            Created = created.StatusCode,
            Retrieved = retrieved.StatusCode,
            Status = (string?)stored["status"],
            FailureReason = (int)stored["failure_reason"]!
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(503)]
    [InlineData(500)]
    [InlineData(307)]
    public async Task PostPaymentAsync_WhenBankFailsOrRedirects_ShouldSendOneBankRequest(int statusCode)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync(context =>
        {
            context.Response.StatusCode = statusCode;
            context.Response.Headers.Location = $"http://{context.Request.Host}/payments";
            return Task.CompletedTask;
        });
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        using HttpResponseMessage created = await PaymentRequests.SendAsync(client);

        // ASSERT
        int expected = 1;
        int actual = bank.Requests.Count;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankConnectionFails_ShouldStoreUnavailable()
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync();
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();
        await bank.StopAsync();

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client);
        JsonNode body = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

        // ASSERT
        var expected = new
        {
            HttpStatus = HttpStatusCode.Created,
            Status = "Declined",
            FailureReason = 2
        };
        var actual = new
        {
            HttpStatus = response.StatusCode,
            Status = (string?)body["status"],
            FailureReason = (int)body["failure_reason"]!
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankRespondsAfterTimeout_ShouldPreserveStoredTimeout()
    {
        // ARRANGE
        FakeTimeProvider clock = new(new DateTimeOffset(2030, 11, 15, 0, 0, 0, TimeSpan.Zero));
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            try
            {
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await context.Response.WriteAsJsonAsync(new { authorized = true });
            }
            finally
            {
                finished.TrySetResult();
            }
        });
        using GatewayFactory factory = new(bank.Address, clock);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            clock.Advance(TimeSpan.FromSeconds(5));
            using HttpResponseMessage created = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            release.TrySetResult();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
            using HttpResponseMessage replay = await PaymentRequests.SendAsync(client);
            JsonNode stored = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;
            JsonNode replayed = JsonNode.Parse(await replay.Content.ReadAsStringAsync())!;

            // ASSERT
            var expected = new
            {
                Created = HttpStatusCode.Created,
                Retrieved = HttpStatusCode.OK,
                Status = "Declined",
                FailureReason = 2
            };
            var actual = new
            {
                Created = created.StatusCode,
                Retrieved = retrieved.StatusCode,
                Status = (string?)stored["status"],
                FailureReason = (int)stored["failure_reason"]!
            };
            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankRespondsAfterTimeout_ShouldReplayStoredPayment()
    {
        // ARRANGE
        FakeTimeProvider clock = new(new DateTimeOffset(2030, 11, 15, 0, 0, 0, TimeSpan.Zero));
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            try
            {
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await context.Response.WriteAsJsonAsync(new { authorized = true });
            }
            finally
            {
                finished.TrySetResult();
            }
        });
        using GatewayFactory factory = new(bank.Address, clock);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            clock.Advance(TimeSpan.FromSeconds(5));
            using HttpResponseMessage created = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            release.TrySetResult();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage retrieved = await client.GetAsync(created.Headers.Location);
            using HttpResponseMessage replay = await PaymentRequests.SendAsync(client);
            JsonNode stored = JsonNode.Parse(await retrieved.Content.ReadAsStringAsync())!;
            JsonNode replayed = JsonNode.Parse(await replay.Content.ReadAsStringAsync())!;

            // ASSERT
            bool expected = true;
            bool actual = JsonNode.DeepEquals(stored, replayed);
            Assert.Equal(expected, actual);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankRespondsAfterTimeout_ShouldSendOneBankRequest()
    {
        // ARRANGE
        FakeTimeProvider clock = new(new DateTimeOffset(2030, 11, 15, 0, 0, 0, TimeSpan.Zero));
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource finished = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            try
            {
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await context.Response.WriteAsJsonAsync(new { authorized = true });
            }
            finally
            {
                finished.TrySetResult();
            }
        });
        using GatewayFactory factory = new(bank.Address, clock);
        using HttpClient client = factory.CreateMerchantClient();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            clock.Advance(TimeSpan.FromSeconds(5));
            using HttpResponseMessage created = await pending.WaitAsync(TimeSpan.FromSeconds(10));
            release.TrySetResult();
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage replay = await PaymentRequests.SendAsync(client);

            // ASSERT
            int expected = 1;
            int actual = bank.Requests.Count;
            Assert.Equal(expected, actual);
        }
        finally
        {
            release.TrySetResult();
        }
    }
}