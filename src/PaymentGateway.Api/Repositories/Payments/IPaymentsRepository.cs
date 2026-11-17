using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.Repositories.Payments;

public interface IPaymentsRepository
{
    void Add(PaymentRecord payment);

    void Update(PaymentRecord payment);

    PaymentRecord? Get(string merchantId, Guid id);
}