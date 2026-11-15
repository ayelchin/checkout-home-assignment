using System.Collections.Immutable;

using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Services.PaymentValidation;
using PaymentGateway.Api.UnitTests.Infrastructure;

namespace PaymentGateway.Api.UnitTests.Services.PaymentValidation;

public sealed class PaymentDetailsValidatorTests
{
    [Fact]
    public void Validate_WhenExpiryIsCurrentMonth_ShouldAcceptPayment()
    {
        // ARRANGE
        PaymentDetailsValidator validator = new(new FixedTimeProvider());
        PaymentDetailsInput input = new("0000000000000011", "123", 11, 2030, "GBP", 100);

        // ACT
        ImmutableArray<ErrorState> errors = [.. validator.Validate(input)];

        // ASSERT
        IEnumerable<ErrorState> expected = [];
        IEnumerable<ErrorState> actual = errors;
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("123", 12, 2031, "GBP", 1)]
    [InlineData("1234", 12, 2031, "USD", int.MaxValue)]
    [InlineData("012", 12, 2031, "EUR", 100)]
    public void Validate_WhenDetailsAreSupported_ShouldAcceptPayment(string cvv, int month, int year, string currency, int amount)
    {
        // ARRANGE
        PaymentDetailsValidator validator = new(new FixedTimeProvider());
        PaymentDetailsInput input = new("0000000000000011", cvv, month, year, currency, amount);

        // ACT
        ImmutableArray<ErrorState> errors = [.. validator.Validate(input)];

        // ASSERT
        IEnumerable<ErrorState> expected = [];
        IEnumerable<ErrorState> actual = errors;
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("12", 12, 2031, "GBP", 100, "cvv_invalid")]
    [InlineData("12345", 12, 2031, "GBP", 100, "cvv_invalid")]
    [InlineData("1a3", 12, 2031, "GBP", 100, "cvv_invalid")]
    [InlineData("１２３", 12, 2031, "GBP", 100, "cvv_invalid")]
    [InlineData("123", 0, 2031, "GBP", 100, "expiry_month_invalid")]
    [InlineData("123", 13, 2031, "GBP", 100, "expiry_month_invalid")]
    [InlineData("123", 10, 2030, "GBP", 100, "expiry_date_expired")]
    [InlineData("123", 12, 2029, "GBP", 100, "expiry_date_expired")]
    [InlineData("123", 12, 2031, "gbp", 100, "currency_unsupported")]
    [InlineData("123", 12, 2031, "JPY", 100, "currency_unsupported")]
    [InlineData("123", 12, 2031, "GBP", 0, "amount_invalid")]
    [InlineData("123", 12, 2031, "GBP", -1, "amount_invalid")]
    public void Validate_WhenPaymentRuleFails_ShouldReturnFieldError(string cvv, int month, int year, string currency, int amount, string code)
    {
        // ARRANGE
        PaymentDetailsValidator validator = new(new FixedTimeProvider());
        PaymentDetailsInput input = new("0000000000000011", cvv, month, year, currency, amount);

        // ACT
        ImmutableArray<ErrorState> errors = [.. validator.Validate(input)];

        // ASSERT
        string[] expected = [code];
        IEnumerable<string> actual = errors.Select(error => error.Code);
        Assert.Equal(expected, actual);
    }
}