namespace PaymentGateway.Api.Domain.Models;

// Transient input only: never store or log full card details.
public sealed class PaymentDetailsInput(string cardNumber, string cvv, int expiryMonth, int expiryYear, string currency, int amount)
{
    public string CardNumber { get; } = cardNumber;
    public string Cvv { get; } = cvv;
    public int ExpiryMonth { get; } = expiryMonth;
    public int ExpiryYear { get; } = expiryYear;
    public string Currency { get; } = currency;
    public int Amount { get; } = amount;
}