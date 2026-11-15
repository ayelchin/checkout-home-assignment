using System.Collections.Immutable;
using System.Text.Json.Serialization;

using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.Dto.CreatePayment;

public sealed class CreatePaymentResponseDto
{
    public Guid Id { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter<PaymentStatus>))]
    public PaymentStatus Status { get; init; }
    public required string CardNumberLastFour { get; init; }
    public int ExpiryMonth { get; init; }
    public int ExpiryYear { get; init; }
    public required string Currency { get; init; }
    public int Amount { get; init; }
    [JsonPropertyName("failure_reason")]
    public PaymentFailureReason FailureReason { get; init; }
    public ImmutableArray<PaymentFieldErrorDto> Errors { get; init; } = [];
}