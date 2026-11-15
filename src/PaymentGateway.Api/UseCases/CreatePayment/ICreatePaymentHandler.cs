using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.UseCases.CreatePayment;

public interface ICreatePaymentHandler
{
    Task<PaymentRecord> ExecuteAsync(CreatePaymentInput input);
}