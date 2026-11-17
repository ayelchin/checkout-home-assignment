using AwesomeAssertions;

using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.UnitTests.Domain.Models;

public sealed class PaymentRecordTests
{
    [Fact]
    public void Create_WhenCalled_ShouldApplyInitialPaymentRules()
    {
        // ARRANGE
        DateTimeOffset createdAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);

        // ACT
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, createdAt);

        // ASSERT
        var expected = new
        {
            MerchantId = "merchant-one",
            CardNumberLastFour = "0011",
            ExpiryMonth = 12,
            ExpiryYear = 2031,
            Currency = "GBP",
            Amount = 100,
            CreatedAt = createdAt,
            LastModifiedAt = (DateTimeOffset?)null,
            Status = PaymentStatus.InProgress,
            FailureReason = PaymentFailureReason.None
        };
        var actual = new
        {
            payment.MerchantId,
            payment.CardNumberLastFour,
            payment.ExpiryMonth,
            payment.ExpiryYear,
            payment.Currency,
            payment.Amount,
            payment.CreatedAt,
            payment.LastModifiedAt,
            payment.Status,
            payment.FailureReason
        };
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Create_WhenCalled_ShouldGeneratePaymentId()
    {
        // ARRANGE
        DateTimeOffset createdAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);

        // ACT
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, createdAt);

        // ASSERT
        Guid expected = Guid.Empty;
        Guid actual = payment.Id;
        actual.Should().NotBe(expected);
    }

    [Fact]
    public void SetStatus_WhenCalled_ShouldLeaveLastModifiedAtUnchanged()
    {
        // ARRANGE
        DateTimeOffset createdAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, createdAt);

        // ACT
        payment.SetStatus(PaymentStatus.Authorized);

        // ASSERT
        DateTimeOffset? expected = null;
        DateTimeOffset? actual = payment.LastModifiedAt;
        actual.Should().Be(expected);
    }

    [Fact]
    public void SetFailureReason_WhenCalled_ShouldLeaveLastModifiedAtUnchanged()
    {
        // ARRANGE
        DateTimeOffset createdAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, createdAt);

        // ACT
        payment.SetFailureReason(PaymentFailureReason.None);

        // ASSERT
        DateTimeOffset? expected = null;
        DateTimeOffset? actual = payment.LastModifiedAt;
        actual.Should().Be(expected);
    }

    [Fact]
    public void SetBankReference_WhenCalled_ShouldLeaveLastModifiedAtUnchanged()
    {
        // ARRANGE
        DateTimeOffset createdAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, createdAt);

        // ACT
        payment.SetBankReference("approval");

        // ASSERT
        DateTimeOffset? expected = null;
        DateTimeOffset? actual = payment.LastModifiedAt;
        actual.Should().Be(expected);
    }

    [Fact]
    public void SetErrors_WhenCalled_ShouldLeaveLastModifiedAtUnchanged()
    {
        // ARRANGE
        DateTimeOffset createdAt = new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);
        PaymentRecord payment = PaymentRecord.Create("merchant-one", "0000000000000011",
            12, 2031, "GBP", 100, createdAt);

        // ACT
        payment.SetErrors([]);

        // ASSERT
        DateTimeOffset? expected = null;
        DateTimeOffset? actual = payment.LastModifiedAt;
        actual.Should().Be(expected);
    }
}