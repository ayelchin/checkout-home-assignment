using System.Net;
using System.Text.Json.Nodes;

using AwesomeAssertions;

using PaymentGateway.Api.IntegrationTests.Infrastructure;

namespace PaymentGateway.Api.IntegrationTests.UseCases.CreatePayment;

public sealed class ClientDisconnectTests
{
    [Fact]
    public async Task PostPaymentAsync_WhenOriginalClientDisconnects_ShouldCompletePaymentForReplay()
    {
        // ARRANGE
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await context.Response.WriteAsJsonAsync(new { authorized = true });
        });
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();
        using CancellationTokenSource disconnect = new();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client, cancellationToken: disconnect.Token);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            disconnect.Cancel();
            Task<HttpResponseMessage> retry = PaymentRequests.SendAsync(client);
            release.TrySetResult();
            Exception? cancellation = await Record.ExceptionAsync(async () => await pending.WaitAsync(TimeSpan.FromSeconds(10)));
            using HttpResponseMessage replay = await retry.WaitAsync(TimeSpan.FromSeconds(10));
            JsonNode body = JsonNode.Parse(await replay.Content.ReadAsStringAsync())!;

            // ASSERT
            var expected = new
            {

                StatusCode = HttpStatusCode.Created,
                Status = "Authorized"
            };
            var actual = new
            {

                StatusCode = replay.StatusCode,
                Status = (string?)body["status"]
            };
            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostPaymentAsync_WhenOriginalClientDisconnects_ShouldCancelOriginalRequest()
    {
        // ARRANGE
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await context.Response.WriteAsJsonAsync(new { authorized = true });
        });
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();
        using CancellationTokenSource disconnect = new();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client, cancellationToken: disconnect.Token);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            disconnect.Cancel();
            Task<HttpResponseMessage> retry = PaymentRequests.SendAsync(client);
            release.TrySetResult();
            Exception? cancellation = await Record.ExceptionAsync(async () => await pending.WaitAsync(TimeSpan.FromSeconds(10)));
            using HttpResponseMessage replay = await retry.WaitAsync(TimeSpan.FromSeconds(10));
            JsonNode body = JsonNode.Parse(await replay.Content.ReadAsStringAsync())!;

            // ASSERT
            bool expected = true;
            bool actual = cancellation is OperationCanceledException;
            Assert.Equal(expected, actual);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostPaymentAsync_WhenOriginalClientDisconnects_ShouldSendOneBankRequest()
    {
        // ARRANGE
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await context.Response.WriteAsJsonAsync(new { authorized = true });
        });
        using GatewayFactory factory = new(bank.Address);
        using HttpClient client = factory.CreateMerchantClient();
        using CancellationTokenSource disconnect = new();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client, cancellationToken: disconnect.Token);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            disconnect.Cancel();
            Task<HttpResponseMessage> retry = PaymentRequests.SendAsync(client);
            release.TrySetResult();
            Exception? cancellation = await Record.ExceptionAsync(async () => await pending.WaitAsync(TimeSpan.FromSeconds(10)));
            using HttpResponseMessage replay = await retry.WaitAsync(TimeSpan.FromSeconds(10));


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