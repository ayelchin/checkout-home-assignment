using System.Text.Json.Nodes;

using AwesomeAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.IntegrationTests.Infrastructure;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.Services.PaymentValidation;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.IntegrationTests.Logging;

public sealed class ApplicationLoggingTests
{
    [Theory]
    [InlineData(true, "Authorized", "None")]
    [InlineData(false, "Declined", "BankDeclined")]
    public async Task PostPaymentAsync_WhenBankResponds_ShouldLogBusinessOutcomeAndSuccessfulLookup(
        bool authorized, string status, string failureReason)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync(context => context.Response.WriteAsJsonAsync(new { authorized }));
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)
                .AddFilter(typeof(AcquiringBankService).FullName, LogLevel.Debug)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client);
        using HttpResponseMessage lookup = await client.GetAsync(response.Headers.Location);

        // ASSERT
        CapturedLog completed = logs.Entries.Single(entry => entry.EventId.Name == "PaymentAttemptCompleted");
        CapturedLog found = logs.Entries.Single(entry => entry.EventId.Name == "PaymentLookupCompleted");
        var expected = new
        {
            Level = LogLevel.Information,
            Status = status,
            FailureReason = failureReason,
            Found = (object)true,
            SamePayment = true,
            WarningsOrErrors = 0,
            Started = 0,
            HttpClientInformation = 0,
            BankStarted = 1,
            BankCompleted = new[] { new { Level = LogLevel.Debug, Authorized = (object)authorized } }
        };
        var actual = new
        {
            Level = completed.Level,
            Status = completed.Fields["PaymentStatus"]?.ToString(),
            FailureReason = completed.Fields["FailureReason"]?.ToString(),
            Found = found.Fields["Found"],
            SamePayment = Equals(completed.Fields["PaymentId"], found.Fields["PaymentId"]),
            WarningsOrErrors = logs.Entries.Count(entry => entry.Category.StartsWith("PaymentGateway.Api.") && entry.Level >= LogLevel.Warning),
            Started = logs.Entries.Count(entry => entry.EventId.Name == "PaymentAttemptStarted"),
            HttpClientInformation = logs.Entries.Count(entry => entry.Category.StartsWith("System.Net.Http.HttpClient") && entry.Level < LogLevel.Warning),
            BankStarted = logs.Entries.Count(entry => entry.EventId.Name == "BankAuthorizationStarted" && entry.Level == LogLevel.Debug),
            BankCompleted = logs.Entries.Where(entry => entry.EventId.Name == "BankAuthorizationCompleted")
                .Select(entry => new { Level = entry.Level, Authorized = entry.Fields["Authorized"] }).ToArray()
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenInputContainsSecrets_ShouldKeepBodiesAndCredentialsOutOfLogs()
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");
        string body = PaymentRequests.Valid.Replace("\"123\"", "\"secret-cvv-marker\"")
            .Replace("\"GBP\"", "\"private-body-marker\"");

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client, key: "private-idempotency-key", body: body);

        // ASSERT
        string allLogs = string.Join("\n", logs.Entries.Select(entry => entry.Message + entry.Exception
            + string.Join(" ", entry.Fields.Values)));
        string[] expected = [];
        string[] actual = new[] { "0000000000000011", "secret-cvv-marker", "private-body-marker",
                "merchant-one-key", "private-idempotency-key" }.Where(allLogs.Contains).ToArray();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenClientDisconnects_ShouldLogInformationAndStillCompletePayment()
    {
        // ARRANGE
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<CapturedLog> disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using BankStub bank = await BankStub.StartAsync(async context =>
        {
            arrived.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await context.Response.WriteAsJsonAsync(new { authorized = true });
        });
        using CapturingLoggerProvider logs = new(entry =>
        {
            if (entry.EventId.Name == "ClientDisconnected")
            {
                disconnected.TrySetResult(entry);
            }
        });
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");
        using CancellationTokenSource cancellation = new();

        // ACT
        Task<HttpResponseMessage> pending = PaymentRequests.SendAsync(client, cancellationToken: cancellation.Token);
        try
        {
            await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            release.TrySetResult();
            await Record.ExceptionAsync(async () => await pending.WaitAsync(TimeSpan.FromSeconds(10)));
            CapturedLog entry = await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(10));
            using HttpResponseMessage replay = await PaymentRequests.SendAsync(client);

            // ASSERT
            var expected = new { Level = LogLevel.Information, HasTrace = true, Outcome = "Authorized", Errors = 0 };
            var actual = new
            {
                Level = entry.Level,
                HasTrace = entry.Fields.ContainsKey("TraceId"),
                Outcome = JsonNode.Parse(await replay.Content.ReadAsStringAsync())?["status"]?.ToString(),
                Errors = logs.Entries.Count(item => item.Level >= LogLevel.Error && item.Category.StartsWith("PaymentGateway.Api."))
            };
            actual.Should().BeEquivalentTo(expected);
        }
        finally
        {
            release.TrySetResult();
        }
    }

    [Fact]
    public async Task PostPaymentAsync_WhenUnexpectedFailureOccurs_ShouldLogSafeErrorAndReturnGenericProblem()
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        Mock<IPaymentDetailsValidator> validator = new();
        validator.Setup(service => service.Validate(It.IsAny<PaymentDetailsInput>()))
            .Throws(new InvalidOperationException("Sensitive card 0000000000000011 CVV secret-cvv-value"));
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IPaymentDetailsValidator>();
                services.AddSingleton(validator.Object);
            });
        });
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client);
        string body = await response.Content.ReadAsStringAsync();

        // ASSERT
        string allLogs = string.Join("\n", logs.Entries.Select(entry => entry.Message + entry.Exception
            + string.Join(" ", entry.Fields.Values)));
        var expected = new
        {
            StatusCode = 500,
            MediaType = "application/problem+json",
            ProblemStatus = "500",
            ErrorEvents = 1,
            ErrorLevel = LogLevel.Error,
            HttpStatus = "500",
            LeaksSecrets = false,
            CompletedEvents = 0
        };
        var actual = new
        {
            StatusCode = (int)response.StatusCode,
            MediaType = response.Content.Headers.ContentType?.MediaType,
            ProblemStatus = body.StartsWith('{') ? JsonNode.Parse(body)?["status"]?.ToString() : null,
            ErrorEvents = logs.Entries.Count(entry => entry.EventId.Name == "UnhandledException" && entry.Category.StartsWith("PaymentGateway.Api.")),
            ErrorLevel = logs.Entries.FirstOrDefault(entry => entry.EventId.Name == "UnhandledException" && entry.Category.StartsWith("PaymentGateway.Api."))?.Level,
            HttpStatus = logs.Entries.Single(entry => entry.Category.EndsWith("HttpLoggingMiddleware")).Fields["StatusCode"]?.ToString(),
            LeaksSecrets = (allLogs + body).Contains("0000000000000011") || (allLogs + body).Contains("secret-cvv-value"),
            CompletedEvents = logs.Entries.Count(entry => entry.EventId.Name == "PaymentAttemptCompleted")
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenBankIsUnavailable_ShouldCorrelateTechnicalFailureAndOutcome()
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync(context =>
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Task.CompletedTask;
        });
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");
        client.DefaultRequestHeaders.Add("traceparent", "00-12345678901234567890123456789012-1234567890123456-01");

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client);
        string paymentId = (string)JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!;

        // ASSERT
        CapturedLog? failure = logs.Entries.FirstOrDefault(entry => entry.EventId.Name == "BankAuthorizationFailed");
        CapturedLog completed = logs.Entries.Single(entry => entry.EventId.Name == "PaymentAttemptCompleted");
        var expected = new
        {
            Level = LogLevel.Warning,
            Category = "ServiceUnavailable",
            PaymentId = paymentId,
            MerchantId = "merchant-one",
            Outcome = "BankUnavailable",
            Status = "Declined",
            TraceIds = new[] { "12345678901234567890123456789012" }
        };
        var actual = new
        {
            Level = failure?.Level,
            Category = failure?.Fields.GetValueOrDefault("FailureCategory"),
            PaymentId = completed.Fields.GetValueOrDefault("PaymentId")?.ToString(),
            MerchantId = completed.Fields.GetValueOrDefault("MerchantId"),
            Outcome = completed.Fields["FailureReason"]?.ToString(),
            Status = completed.Fields["PaymentStatus"]?.ToString(),
            TraceIds = logs.Entries.Where(entry => entry.EventId.Name is "PaymentAttemptStarted" or "PaymentAttemptCompleted" or "BankAuthorizationFailed"
                    || entry.Category.EndsWith("HttpLoggingMiddleware"))
                .Select(entry => entry.Fields.GetValueOrDefault("TraceId")?.ToString()).Distinct().ToArray()
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PostPaymentAsync_WhenRequestIsInvalid_ShouldLogRejectionWithoutAttempt(bool oversizedKey)
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client,
            key: oversizedKey ? new string('k', 256) : null, body: oversizedKey ? PaymentRequests.Valid : "{}");

        // ASSERT
        var expected = new { Rejected = 1, Started = 0, Level = LogLevel.Information, Status = "422" };
        var actual = new
        {
            Rejected = logs.Entries.Count(entry => entry.EventId.Name == "ValidationRejected"),
            Started = logs.Entries.Count(entry => entry.EventId.Name == "PaymentAttemptStarted"),
            Level = logs.Entries.FirstOrDefault(entry => entry.EventId.Name == "ValidationRejected")?.Level,
            Status = logs.Entries.Single(entry => entry.Category.EndsWith("HttpLoggingMiddleware")).Fields["StatusCode"]?.ToString()
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenReplayed_ShouldLogReplayWithoutAnotherAttempt()
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");
        string body = PaymentRequests.Valid.Replace("\"amount\":100", "\"amount\":0");

        // ACT
        using HttpResponseMessage first = await PaymentRequests.SendAsync(client, body: body);
        using HttpResponseMessage replay = await PaymentRequests.SendAsync(client, body: body);

        // ASSERT
        var expected = new
        {
            Completed = 1,
            Replayed = 1,
            HttpSummaries = 2,
            ReplayLevel = LogLevel.Information,
            ReplayMerchant = "merchant-one"
        };
        var actual = new
        {
            Completed = logs.Entries.Count(entry => entry.EventId.Name == "PaymentAttemptCompleted"),
            Replayed = logs.Entries.Count(entry => entry.EventId.Name == "IdempotencyReplayed"),
            HttpSummaries = logs.Entries.Count(entry => entry.Category.EndsWith("HttpLoggingMiddleware")),
            ReplayLevel = logs.Entries.FirstOrDefault(entry => entry.EventId.Name == "IdempotencyReplayed")?.Level,
            ReplayMerchant = logs.Entries.FirstOrDefault(entry => entry.EventId.Name == "IdempotencyReplayed")?.Fields.GetValueOrDefault("MerchantId")
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPayment_WhenUnauthenticated_ShouldLogCorrelatedHttpSummary()
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false });

        // ACT
        using HttpResponseMessage response = await client.GetAsync("/api/payments/11111111-1111-1111-1111-111111111111");

        // ASSERT
        var expected = new[] { new { Level = LogLevel.Information, StatusCode = "401", HasMethod = true,
            HasPath = true, HasDuration = true, HasTrace = true } };
        var actual = logs.Entries.Where(entry => entry.Category.EndsWith("HttpLoggingMiddleware"))
            .Select(entry => new
            {
                Level = entry.Level,
                StatusCode = entry.Fields.GetValueOrDefault("StatusCode")?.ToString(),
                HasMethod = entry.Fields.ContainsKey("Method"),
                HasPath = entry.Fields.ContainsKey("Path"),
                HasDuration = entry.Fields.ContainsKey("Duration"),
                HasTrace = !string.IsNullOrWhiteSpace(entry.Fields.GetValueOrDefault("TraceId")?.ToString())
            }).ToArray();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task GetPayment_WhenPaymentIsMissing_ShouldLogLookupOutcome()
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage response = await client.GetAsync("/api/payments/11111111-1111-1111-1111-111111111111");

        // ASSERT
        var expected = new[] { new { Event = "PaymentLookupCompleted", Level = LogLevel.Information,
            MerchantId = "merchant-one", PaymentId = "11111111-1111-1111-1111-111111111111", Found = (object)false } };
        var actual = logs.Entries.Where(entry => entry.Category.EndsWith("GetPaymentHandler"))
            .Select(entry => new
            {
                Event = entry.EventId.Name,
                Level = entry.Level,
                MerchantId = entry.Fields.GetValueOrDefault("MerchantId")?.ToString(),
                PaymentId = entry.Fields.GetValueOrDefault("PaymentId")?.ToString(),
                Found = entry.Fields.GetValueOrDefault("Found")
            }).ToArray();
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task PostPaymentAsync_WhenPaymentIsRejected_ShouldLogAttemptAndOutcome()
    {
        // ARRANGE
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new();
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)
                .AddFilter(typeof(CreatePaymentHandler).FullName, LogLevel.Debug)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage response = await client.PostAsync("/api/payments", PaymentRequests.Json(amount: 0));
        string paymentId = (string)JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!;

        // ASSERT
        var expected = new[]
        {
            new { Event = "PaymentAttemptStarted", Level = LogLevel.Debug, PaymentId = paymentId,
                MerchantId = "merchant-one", Status = (string?)null, FailureReason = (string?)null, ValidationCodes = (string?)null },
            new { Event = "PaymentDetailsRejected", Level = LogLevel.Warning, PaymentId = paymentId,
                MerchantId = "merchant-one", Status = (string?)null, FailureReason = (string?)null, ValidationCodes = (string?)"amount_invalid" },
            new { Event = "PaymentAttemptCompleted", Level = LogLevel.Information, PaymentId = paymentId,
                MerchantId = "merchant-one", Status = (string?)"Rejected", FailureReason = (string?)"InvalidPaymentDetails", ValidationCodes = (string?)null }
        };
        var actual = logs.Entries.Where(entry => entry.Category.EndsWith("CreatePaymentHandler"))
            .Select(entry => new
            {
                Event = entry.EventId.Name,
                Level = entry.Level,
                PaymentId = entry.Fields.GetValueOrDefault("PaymentId")?.ToString(),
                MerchantId = entry.Fields.GetValueOrDefault("MerchantId")?.ToString(),
                Status = entry.Fields.GetValueOrDefault("PaymentStatus")?.ToString(),
                FailureReason = entry.Fields.GetValueOrDefault("FailureReason")?.ToString(),
                ValidationCodes = entry.Fields.GetValueOrDefault("ValidationCodes")?.ToString()
            }).ToArray();
        actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("invalid-json-with-sensitive-bank-data")]
    [InlineData("null")]
    public async Task PostPaymentAsync_WhenBankResponseIsUnexpected_ShouldLogSafeError(string bankResponse)
    {
        // ARRANGE
        await using BankStub bank = await BankStub.StartAsync(context => context.Response.WriteAsync(bankResponse));
        using CapturingLoggerProvider logs = new();
        using GatewayFactory baseFactory = new(bank.Address);
        using WebApplicationFactory<PaymentsController> factory = baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.AddProvider(logs)));
        using HttpClient client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add("X-Api-Key", "merchant-one-key");

        // ACT
        using HttpResponseMessage response = await PaymentRequests.SendAsync(client);

        // ASSERT
        var expected = new[] { new { Level = LogLevel.Error, Category = (object)"UnexpectedFailure", HasException = false, LeaksResponse = false } };
        var actual = logs.Entries.Where(entry => entry.EventId.Name == "BankAuthorizationFailed")
            .Select(entry => new
            {
                Level = entry.Level,
                Category = entry.Fields["FailureCategory"],
                HasException = entry.Exception is not null,
                LeaksResponse = (entry.Message + string.Join(" ", entry.Fields.Values)).Contains("sensitive-bank-data")
            }).ToArray();
        actual.Should().BeEquivalentTo(expected);
    }
}
