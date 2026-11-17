using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Logging;
using PaymentGateway.Api.Repositories.Payments;

namespace PaymentGateway.Api.UseCases.GetPayment;

public sealed class GetPaymentHandler(IPaymentsRepository payments, ILogger<GetPaymentHandler> logger) : IGetPaymentHandler
{
    private readonly IPaymentsRepository _payments = payments;

    public PaymentRecord? Execute(string merchantId, Guid id)
    {
        PaymentRecord? payment = _payments.Get(merchantId, id);
        logger.LogInformation(LogEvents.PaymentLookupCompleted,
            "Payment {PaymentId} lookup for merchant {MerchantId}: found {Found}", id, merchantId, payment is not null);
        return payment;
    }
}