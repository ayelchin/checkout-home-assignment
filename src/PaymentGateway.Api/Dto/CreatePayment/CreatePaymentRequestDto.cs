using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Dto.CreatePayment;

public sealed class CreatePaymentRequestDto
{
    [Required(ErrorMessage = "payment_source_required")]
    [RegularExpression(@"\A[0-9]{14,19}\z", ErrorMessage = "payment_source_invalid")]
    public string? CardNumber { get; init; }

    [Required(ErrorMessage = "cvv_required")]
    public string? Cvv { get; init; }

    [Required(ErrorMessage = "expiry_month_required")]
    public int? ExpiryMonth { get; init; }

    [Required(ErrorMessage = "expiry_year_required")]
    public int? ExpiryYear { get; init; }

    [Required(ErrorMessage = "currency_required")]
    public string? Currency { get; init; }

    [Required(ErrorMessage = "amount_required")]
    public int? Amount { get; init; }
}