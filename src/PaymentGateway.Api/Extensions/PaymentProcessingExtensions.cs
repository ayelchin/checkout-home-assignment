using PaymentGateway.Api.Filters;
using PaymentGateway.Api.Repositories.Payments;
using PaymentGateway.Api.Services.PaymentValidation;
using PaymentGateway.Api.UseCases.CreatePayment;
using PaymentGateway.Api.UseCases.GetPayment;

namespace PaymentGateway.Api.Extensions;

public static class PaymentProcessingExtensions
{
    public static IServiceCollection AddPaymentProcessing(this IServiceCollection services)
    {
        services.AddSingleton<IPaymentsRepository, InMemoryPaymentsRepository>();
        services.AddSingleton<PaymentIdempotencyFilter>();
        services.AddSingleton<IGetPaymentHandler, GetPaymentHandler>();
        services.AddTransient<ICreatePaymentHandler, CreatePaymentHandler>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPaymentDetailsValidator, PaymentDetailsValidator>();

        return services;
    }
}