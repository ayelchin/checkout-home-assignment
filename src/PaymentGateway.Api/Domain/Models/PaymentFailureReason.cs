namespace PaymentGateway.Api.Domain.Models;

public enum PaymentFailureReason
{
    None = 0,
    BankDeclined = 1,
    BankUnavailable = 2,
    InvalidPaymentDetails = 6
}