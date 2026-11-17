using System.Collections.Immutable;

namespace PaymentGateway.Api.Domain.Models;

public sealed class PaymentRecord
{
    private PaymentRecord(Guid id, string merchantId, string cardNumberLastFour,
        int expiryMonth, int expiryYear, string currency, int amount, PaymentStatus status,
        PaymentFailureReason failureReason, string bankReference, ImmutableArray<ErrorState> errors,
        DateTimeOffset createdAt, DateTimeOffset? lastModifiedAt)
    {
        Id = id;
        MerchantId = merchantId;
        CardNumberLastFour = cardNumberLastFour;
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        Currency = currency;
        Amount = amount;
        Status = status;
        FailureReason = failureReason;
        BankReference = bankReference;
        Errors = errors;
        CreatedAt = createdAt;
        LastModifiedAt = lastModifiedAt;
    }

    public static PaymentRecord Create(string merchantId, string cardNumber,
        int expiryMonth, int expiryYear, string currency, int amount, DateTimeOffset createdAt)
    {
        return new(Guid.NewGuid(), merchantId, cardNumber[^4..], expiryMonth,
            expiryYear, currency, amount, PaymentStatus.InProgress, PaymentFailureReason.None,
            string.Empty, [], createdAt, null);
    }

    public Guid Id { get; }
    public string MerchantId { get; }
    public string CardNumberLastFour { get; }
    public int ExpiryMonth { get; }
    public int ExpiryYear { get; }
    public string Currency { get; }
    public int Amount { get; }
    public PaymentStatus Status { get; private set; }
    public PaymentFailureReason FailureReason { get; private set; }
    public string BankReference { get; private set; }
    public ImmutableArray<ErrorState> Errors { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? LastModifiedAt { get; internal set; }

    public void SetStatus(PaymentStatus status)
    {
        Status = status;
    }

    public void SetFailureReason(PaymentFailureReason reason)
    {
        FailureReason = reason;
    }

    public void SetBankReference(string bankReference)
    {
        BankReference = bankReference;
    }

    public void SetErrors(IEnumerable<ErrorState> errors)
    {
        Errors = [.. errors];
    }
}