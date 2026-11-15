using System.Collections.Immutable;
using System.Collections.ObjectModel;

using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.Services.PaymentValidation;

public sealed class PaymentDetailsValidator(TimeProvider clock) : IPaymentDetailsValidator
{
    private const int MinimumCvvLength = 3;
    private const int MaximumCvvLength = 4;
    private const int MinimumExpiryMonth = 1;
    private const int MaximumExpiryMonth = 12;
    private readonly TimeProvider _clock = clock;

    public ReadOnlyCollection<ErrorState> Validate(PaymentDetailsInput request)
    {
        ImmutableArray<ErrorState>.Builder errors = ImmutableArray.CreateBuilder<ErrorState>();
        if (request.Cvv.Length is < MinimumCvvLength or > MaximumCvvLength || request.Cvv.Any(c => c is < '0' or > '9'))
        {
            errors.Add(new("cvv", "cvv_invalid", "CVV must contain 3-4 ASCII digits."));
        }

        if (request.ExpiryMonth is < MinimumExpiryMonth or > MaximumExpiryMonth)
        {
            errors.Add(new("expiryMonth", "expiry_month_invalid", "Expiry month must be between 1 and 12."));
        }

        DateTimeOffset now = _clock.GetUtcNow();
        if (request.ExpiryMonth is >= MinimumExpiryMonth and <= MaximumExpiryMonth &&
            (request.ExpiryYear < now.Year || (request.ExpiryYear == now.Year && request.ExpiryMonth < now.Month)))
        {
            errors.Add(new("expiryDate", "expiry_date_expired", "Card expiry date must not be in the past."));
        }

        if (!Currency.Allowed.Any(currency => currency.Code == request.Currency))
        {
            errors.Add(new("currency", "currency_unsupported", "Currency must be GBP, USD, or EUR."));
        }

        if (request.Amount <= 0)
        {
            errors.Add(new("amount", "amount_invalid", "Amount must be positive."));
        }

        return errors.AsReadOnly();
    }
}