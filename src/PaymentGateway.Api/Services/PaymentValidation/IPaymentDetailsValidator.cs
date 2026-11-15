using System.Collections.ObjectModel;

using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.Services.PaymentValidation;

public interface IPaymentDetailsValidator
{
    ReadOnlyCollection<ErrorState> Validate(PaymentDetailsInput request);
}