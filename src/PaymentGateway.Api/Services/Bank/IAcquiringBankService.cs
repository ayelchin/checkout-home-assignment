using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.UseCases.CreatePayment;

namespace PaymentGateway.Api.Services.Bank;

public interface IAcquiringBankService
{
    Task<BankAuthorizationResult> AuthorizeAsync(PaymentDetailsInput request);
}