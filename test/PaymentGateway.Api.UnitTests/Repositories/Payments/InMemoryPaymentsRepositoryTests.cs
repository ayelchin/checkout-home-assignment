using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Repositories.Payments;
using PaymentGateway.Api.UnitTests.Infrastructure;

namespace PaymentGateway.Api.UnitTests.Repositories.Payments;

public sealed class InMemoryPaymentsRepositoryTests
{
    [Fact]
    public void Add_WhenPaymentIsNew_ShouldPersistPayment()
    {
        // ARRANGE
        InMemoryPaymentsRepository repository = new(new FixedTimeProvider());
        PaymentRecord expected = global::PaymentGateway.Api.UnitTests.Infrastructure.Payments.Record();

        // ACT
        repository.Add(expected);

        // ASSERT
        PaymentRecord? actual = repository.Get(expected.MerchantId, expected.Id);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void Update_WhenPaymentIsProvided_ShouldPersistPayment()
    {
        // ARRANGE
        InMemoryPaymentsRepository repository = new(new FixedTimeProvider());
        PaymentRecord expected = global::PaymentGateway.Api.UnitTests.Infrastructure.Payments.Record();
        expected.SetStatus(PaymentStatus.Authorized);

        // ACT
        repository.Update(expected);

        // ASSERT
        PaymentRecord? actual = repository.Get(expected.MerchantId, expected.Id);
        Assert.Same(expected, actual);
    }

    [Fact]
    public void Update_WhenPaymentExists_ShouldStampLastModifiedAt()
    {
        // ARRANGE
        DateTimeOffset updatedAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);
        InMemoryPaymentsRepository repository = new(new FixedTimeProvider());
        PaymentRecord payment = global::PaymentGateway.Api.UnitTests.Infrastructure.Payments.Record();
        repository.Add(payment);

        // ACT
        repository.Update(payment);

        // ASSERT
        DateTimeOffset? expected = updatedAt;
        DateTimeOffset? actual = payment.LastModifiedAt;
        Assert.Equal(expected, actual);
    }
}