using PaymentGateway.Api.Services.Bank;

namespace PaymentGateway.Api.Extensions;

public static class AcquiringBankExtensions
{
    public static IServiceCollection AddAcquiringBank(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BankOptions>().Bind(configuration.GetSection(BankOptions.Section))
            .Validate(options => options.ResponseTimeoutSeconds > 0 && double.IsFinite(options.ResponseTimeoutSeconds)
                && options.ResponseTimeoutSeconds <= uint.MaxValue / 1000d, "Bank response timeout must be positive and supported by the timer.")
            .ValidateOnStart();
        services.AddHttpClient<IAcquiringBankService, AcquiringBankService>(client =>
        {
            string baseUrl = configuration["Bank:BaseUrl"]
                ?? throw new InvalidOperationException("Bank:BaseUrl must be configured.");
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = Timeout.InfiniteTimeSpan;
        })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });

        return services;
    }
}