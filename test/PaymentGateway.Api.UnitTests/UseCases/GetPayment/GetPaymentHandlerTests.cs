using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Repositories.Payments;
using PaymentGateway.Api.UnitTests.Infrastructure;
using PaymentGateway.Api.UseCases.GetPayment;

namespace PaymentGateway.Api.UnitTests.UseCases.GetPayment;

public sealed class GetPaymentHandlerTests
{
    [Fact]
    public void Execute_WhenPaymentIsOwned_ShouldReturnPayment()
    {
        // ARRANGE
        InMemoryPaymentsRepository repository = new(new FixedTimeProvider());
        PaymentRecord payment = Payments.Record();
        repository.Add(payment);
        GetPaymentHandler handler = new(repository, NullLogger<GetPaymentHandler>.Instance);

        // ACT
        PaymentRecord? result = handler.Execute("merchant-one", payment.Id);

        // ASSERT
        PaymentRecord expected = payment;
        PaymentRecord? actual = result;
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("merchant-two", "11111111-1111-1111-1111-111111111111")]
    [InlineData("merchant-one", "22222222-2222-2222-2222-222222222222")]
    public void Execute_WhenPaymentIsMissingOrForeign_ShouldReturnNothing(string merchantId, string id)
    {
        // ARRANGE
        InMemoryPaymentsRepository repository = new(new FixedTimeProvider());
        repository.Add(Payments.Record());
        GetPaymentHandler handler = new(repository, NullLogger<GetPaymentHandler>.Instance);

        // ACT
        PaymentRecord? result = handler.Execute(merchantId, Guid.Parse(id));

        // ASSERT
        PaymentRecord? expected = null;
        PaymentRecord? actual = result;
        Assert.Equal(expected, actual);
    }
}