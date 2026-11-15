using System.Collections.Immutable;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Dto.CreatePayment;
using PaymentGateway.Api.Dto.GetPayment;

namespace PaymentGateway.Api.Dto;

internal static class PaymentResponseMapper
{
    public static CreatePaymentResponseDto ToPostResponse(PaymentRecord payment)
    {
        return new()
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount,
            FailureReason = payment.FailureReason,
            Errors = ToErrorDtos(payment.Errors)
        };
    }

    public static GetPaymentResponseDto ToGetResponse(PaymentRecord payment)
    {
        return new()
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount,
            FailureReason = payment.FailureReason,
            Errors = ToErrorDtos(payment.Errors)
        };
    }

    private static ImmutableArray<PaymentFieldErrorDto> ToErrorDtos(ImmutableArray<ErrorState> errors)
    {
        return [.. errors.Select(error => new PaymentFieldErrorDto(error.Field, error.Code, error.Message))];
    }
}