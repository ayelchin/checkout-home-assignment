using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.UseCases.GetPayment;

public interface IGetPaymentHandler
{
    PaymentRecord? Execute(string merchantId, Guid id);
}