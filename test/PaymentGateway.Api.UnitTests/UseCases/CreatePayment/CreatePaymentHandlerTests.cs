using AwesomeAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Repositories.Payments;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.Services.PaymentValidation;
using PaymentGateway.Api.UnitTests.Infrastructure;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.UnitTests.UseCases.CreatePayment;

public sealed class CreatePaymentHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WhenValidating_ShouldAlreadyHaveStoredInProgressPayment()
    {
        // ARRANGE
        InMemoryPaymentsRepository storage = new(new FixedTimeProvider());
        Mock<IPaymentsRepository> repository = new();
        PaymentRecord? initialPayment = null;
        PaymentStatus? observedStatus = null;
        repository.Setup(store => store.Add(It.IsAny<PaymentRecord>()))
            .Callback<PaymentRecord>(payment =>
            {
                initialPayment = payment;
                storage.Add(payment);
            });
        repository.Setup(store => store.Update(It.IsAny<PaymentRecord>()))
            .Callback<PaymentRecord>(storage.Update);
        Mock<IPaymentDetailsValidator> validator = new();
        validator.Setup(service => service.Validate(It.IsAny<PaymentDetailsInput>()))
            .Callback(() => observedStatus = storage.Get(initialPayment!.MerchantId, initialPayment.Id)?.Status)
            .Returns(Array.Empty<ErrorState>().ToList().AsReadOnly());
        CreatePaymentHandler handler = new(repository.Object, new BankClientStub(), validator.Object, new FixedTimeProvider(), NullLogger<CreatePaymentHandler>.Instance);

        // ACT
        await handler.ExecuteAsync(new("merchant-one", Payments.Details()));

        // ASSERT
        PaymentStatus expected = PaymentStatus.InProgress;
        PaymentStatus? actual = observedStatus;
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValidationThrows_ShouldPropagateUnexpectedFailure()
    {
        // ARRANGE
        Mock<IPaymentDetailsValidator> validator = new();
        validator.Setup(service => service.Validate(It.IsAny<PaymentDetailsInput>()))
            .Throws(new InvalidOperationException("simulated validation failure"));
        CreatePaymentHandler handler = new(new InMemoryPaymentsRepository(new FixedTimeProvider()), new Mock<IAcquiringBankService>().Object, validator.Object, new FixedTimeProvider(), NullLogger<CreatePaymentHandler>.Instance);

        // ACT
        Exception? error = await Record.ExceptionAsync(() => handler.ExecuteAsync(new("merchant-one", Payments.Details())));

        // ASSERT
        Type expected = typeof(InvalidOperationException);
        Type? actual = error?.GetType();
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(PaymentFailureReason.None, PaymentStatus.Authorized, PaymentFailureReason.None)]
    [InlineData(PaymentFailureReason.BankDeclined, PaymentStatus.Declined, PaymentFailureReason.BankDeclined)]
    [InlineData(PaymentFailureReason.BankUnavailable, PaymentStatus.Declined, PaymentFailureReason.BankUnavailable)]
    public async Task ExecuteAsync_WhenBankReturnsOutcome_ShouldCreatePayment(PaymentFailureReason bankOutcome, PaymentStatus status, PaymentFailureReason reason)
    {
        // ARRANGE
        BankClientStub bank = new(bankOutcome);
        CreatePaymentHandler handler = CreateHandler(bank);

        // ACT
        PaymentRecord result = await handler.ExecuteAsync(new("merchant-one", Payments.Details()));

        // ASSERT
        var expected = new
        {
            Status = status,
            FailureReason = reason,
            CardNumberLastFour = "0011",
            MerchantId = "merchant-one"
        };
        var actual = new
        {
            result.Status,
            result.FailureReason,
            result.CardNumberLastFour,
            result.MerchantId
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaymentRulesFail_ShouldReturnRejectedPayment()
    {
        // ARRANGE
        BankClientStub bank = new();
        CreatePaymentHandler handler = CreateHandler(bank);

        // ACT
        PaymentRecord result = await handler.ExecuteAsync(new("merchant-one", Payments.Details(amount: 0)));

        // ASSERT
        var expected = new
        {
            Status = PaymentStatus.Rejected,
            FailureReason = PaymentFailureReason.InvalidPaymentDetails,
            Code = "amount_invalid"
        };
        var actual = new
        {
            result.Status,
            result.FailureReason,
            result.Errors.Single().Code
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaymentRulesFail_ShouldNotAuthorizeWithBank()
    {
        // ARRANGE
        Mock<IAcquiringBankService> bank = new();
        CreatePaymentHandler handler = CreateHandler(bank.Object);

        // ACT
        await handler.ExecuteAsync(new("merchant-one", Payments.Details(amount: 0)));

        // ASSERT
        bank.Verify(client => client.AuthorizeAsync(It.IsAny<PaymentDetailsInput>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBankApproves_ShouldPersistAuthorizationCode()
    {
        // ARRANGE
        InMemoryPaymentsRepository repository = new(new FixedTimeProvider());
        CreatePaymentHandler handler = new(repository, new BankClientStub(),
            new PaymentDetailsValidator(new FixedTimeProvider()), new FixedTimeProvider(), NullLogger<CreatePaymentHandler>.Instance);

        // ACT
        PaymentRecord payment = await handler.ExecuteAsync(new("merchant-one", Payments.Details()));

        // ASSERT
        string expected = "approval";
        string? actual = repository.Get("merchant-one", payment.Id)?.BankReference;
        Assert.Equal(expected, actual);
    }

    private static CreatePaymentHandler CreateHandler(IAcquiringBankService bank)
    {
        return new(new InMemoryPaymentsRepository(new FixedTimeProvider()), bank, new PaymentDetailsValidator(new FixedTimeProvider()), new FixedTimeProvider(), NullLogger<CreatePaymentHandler>.Instance);
    }
}