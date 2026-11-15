using System.Collections.Immutable;
using System.Text.Json.Serialization;

using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.Dto.GetPayment;

public sealed class GetPaymentResponseDto
{
    public Guid Id { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter<PaymentStatus>))]
    public PaymentStatus Status { get; init; }
    /// <summary>Exactly the stored last four digits, preserving leading zeros.</summary>
    public required string CardNumberLastFour { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    /// <summary>Signed 32-bit amount in minor currency units.</summary>
    public int Amount { get; init; }
    [JsonPropertyName("failure_reason")]
    public PaymentFailureReason FailureReason { get; init; }
    /// <summary>Stored field errors for rejected payments; empty for other outcomes.</summary>
    public ImmutableArray<PaymentFieldErrorDto> Errors { get; init; } = [];
}