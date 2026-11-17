using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Services.Bank;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.UnitTests.Infrastructure;

internal sealed class BankClientStub(PaymentFailureReason failureReason = PaymentFailureReason.None) : IAcquiringBankService
{
    public int Calls { get; private set; }

    public Task<BankAuthorizationResult> AuthorizeAsync(PaymentDetailsInput request)
    {
        Calls++;
        bool authorized = failureReason == PaymentFailureReason.None;
        return Task.FromResult(new BankAuthorizationResult(
            authorized ? PaymentStatus.Authorized : PaymentStatus.Declined, failureReason,
            new AuthorizationResponse(authorized, authorized ? "approval" : string.Empty)));
    }
}