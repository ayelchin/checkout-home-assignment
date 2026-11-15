using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.UseCases.CreatePayment;

public sealed record CreatePaymentInput(string MerchantId, PaymentDetailsInput Details);