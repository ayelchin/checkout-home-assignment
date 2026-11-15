using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.UnitTests.Infrastructure;

internal static class Payments
{
    public static PaymentDetailsInput Details(string cardNumber = "0000000000000011", string cvv = "123",
        int month = 12, int year = 2031, string currency = "GBP", int amount = 100)
    {
        return new(cardNumber, cvv, month, year, currency, amount);
    }

    public static PaymentRecord Record(string merchantId = "merchant-one")
    {
        return PaymentRecord.Create(merchantId, "0000000000000011", 12, 2031,
            "GBP", 100, new FixedTimeProvider().GetUtcNow());
    }
}