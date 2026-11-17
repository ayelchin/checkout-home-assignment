using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.Repositories.Payments;

public sealed class InMemoryPaymentsRepository : IPaymentsRepository
{
    private readonly TimeProvider _clock;
    private readonly object _sync = new();
    private readonly Dictionary<Guid, PaymentRecord> _payments = [];

    public InMemoryPaymentsRepository(TimeProvider clock)
    {
        _clock = clock;
    }

    public void Add(PaymentRecord payment)
    {
        lock (_sync)
        {
            _payments.Add(payment.Id, payment);
        }
    }

    public void Update(PaymentRecord payment)
    {
        lock (_sync)
        {
            payment.LastModifiedAt = _clock.GetUtcNow();
            _payments[payment.Id] = payment;
        }
    }

    public PaymentRecord? Get(string merchantId, Guid id)
    {
        lock (_sync)
        {
            return _payments.TryGetValue(id, out PaymentRecord? payment) && payment.MerchantId == merchantId
                ? payment
                : null;
        }
    }
}