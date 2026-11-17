using System.Collections.ObjectModel;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Logging;
using PaymentGateway.Api.Repositories.Payments;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.Services.PaymentValidation;

namespace PaymentGateway.Api.UseCases.CreatePayment;

public sealed class CreatePaymentHandler(
    IPaymentsRepository payments,
    IAcquiringBankService bank,
    IPaymentDetailsValidator validator,
    TimeProvider clock,
    ILogger<CreatePaymentHandler> logger) : ICreatePaymentHandler
{
    public async Task<PaymentRecord> ExecuteAsync(CreatePaymentInput input)
    {
        PaymentRecord payment = PaymentRecord.Create(input.MerchantId,
            input.Details.CardNumber, input.Details.ExpiryMonth,
            input.Details.ExpiryYear, input.Details.Currency, input.Details.Amount,
            clock.GetUtcNow());

        logger.LogDebug(LogEvents.PaymentAttemptStarted,
            "Payment {PaymentId} started for merchant {MerchantId}", payment.Id, input.MerchantId);

        payments.Add(payment);

        ReadOnlyCollection<ErrorState> errors = validator.Validate(input.Details);
        if (errors.Count > 0)
        {
            payment.SetErrors(errors);
            payment.SetFailureReason(PaymentFailureReason.InvalidPaymentDetails);
            payment.SetStatus(PaymentStatus.Rejected);
            logger.LogWarning(LogEvents.PaymentDetailsRejected,
                "Payment {PaymentId} for merchant {MerchantId} rejected: {ValidationCodes}",
                payment.Id, input.MerchantId, string.Join(",", errors.Select(error => error.Code)));
        }
        else
        {
            BankAuthorizationResult authorization = await bank.AuthorizeAsync(input.Details);
            payment.SetBankReference(authorization.Response?.AuthorizationCode ?? string.Empty);
            payment.SetFailureReason(authorization.FailureReason);
            payment.SetStatus(authorization.Status);
        }

        payments.Update(payment);

        logger.LogInformation(LogEvents.PaymentAttemptCompleted,
            "Payment {PaymentId} for merchant {MerchantId} completed with {PaymentStatus}, failure reason {FailureReason}",
            payment.Id, input.MerchantId, payment.Status, payment.FailureReason);

        return payment;
    }
}
