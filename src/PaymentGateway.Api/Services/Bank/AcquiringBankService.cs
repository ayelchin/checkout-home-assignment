using System.Globalization;
using System.Net;

using Microsoft.Extensions.Options;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Logging;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.Services.Bank;

public sealed class AcquiringBankService(
    HttpClient client,
    IOptions<BankOptions> options,
    TimeProvider clock,
    ILogger<AcquiringBankService> logger) : IAcquiringBankService
{
    private readonly HttpClient _client = client;
    private readonly IOptions<BankOptions> _options = options;
    private readonly TimeProvider _clock = clock;
    private readonly ILogger<AcquiringBankService> _logger = logger;

    public async Task<BankAuthorizationResult> AuthorizeAsync(PaymentDetailsInput request)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(_options.Value.ResponseTimeoutSeconds), _clock);
        try
        {
            _logger.LogDebug(LogEvents.BankAuthorizationStarted, "Bank authorization started");
            using HttpResponseMessage response = await _client.PostAsJsonAsync("/payments", new
            {
                card_number = request.CardNumber,
                expiry_date = string.Create(CultureInfo.InvariantCulture, $"{request.ExpiryMonth:00}/{request.ExpiryYear:0000}"),
                currency = request.Currency,
                amount = request.Amount,
                cvv = request.Cvv
            }, timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(LogEvents.BankAuthorizationFailed, "Bank authorization failed: {FailureCategory}, HTTP {StatusCode}",
                    ClassifyHttpFailure(response.StatusCode), (int)response.StatusCode);
                return new(PaymentStatus.Declined, PaymentFailureReason.BankUnavailable);
            }

            AuthorizationResponse result = (await response.Content.ReadFromJsonAsync<AuthorizationResponse>(cancellationToken: timeout.Token))!;

            BankAuthorizationResult authorization = new(result.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
                result.Authorized ? PaymentFailureReason.None : PaymentFailureReason.BankDeclined, result);
            _logger.LogDebug(LogEvents.BankAuthorizationCompleted,
                "Bank authorization completed: authorized {Authorized}", result.Authorized);
            return authorization;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(LogEvents.BankAuthorizationFailed, "Bank authorization failed: {FailureCategory}, timeout {TimeoutSeconds} seconds",
                "ResponseTimeout", _options.Value.ResponseTimeoutSeconds);

            return new(PaymentStatus.Declined, PaymentFailureReason.BankUnavailable);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(LogEvents.BankAuthorizationFailed, "Bank authorization failed: {FailureCategory}, transport {HttpRequestError}",
                "ConnectionFailure", exception.HttpRequestError);

            return new(PaymentStatus.Declined, PaymentFailureReason.BankUnavailable);
        }
        catch (Exception)
        {
            _logger.LogError(LogEvents.BankAuthorizationFailed, "Bank authorization failed: {FailureCategory}", "UnexpectedFailure");
            return new(PaymentStatus.Declined, PaymentFailureReason.BankUnavailable);
        }
    }

    private static string ClassifyHttpFailure(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.ServiceUnavailable => "ServiceUnavailable",
            >= (HttpStatusCode)300 and < (HttpStatusCode)400 => "RedirectRejected",
            _ => "UnexpectedHttpStatus"
        };
    }
}
