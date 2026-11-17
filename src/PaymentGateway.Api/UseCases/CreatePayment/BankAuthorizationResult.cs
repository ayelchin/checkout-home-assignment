using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Services.Bank;

namespace PaymentGateway.Api.UseCases.CreatePayment;

public sealed record BankAuthorizationResult(
    PaymentStatus Status,
    PaymentFailureReason FailureReason,
    AuthorizationResponse? Response = null);