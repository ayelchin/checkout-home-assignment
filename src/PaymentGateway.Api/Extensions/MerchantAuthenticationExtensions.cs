using Microsoft.AspNetCore.Authentication;

using PaymentGateway.Api.Authentication;

namespace PaymentGateway.Api.Extensions;

public static class MerchantAuthenticationExtensions
{
    public static IServiceCollection AddMerchantAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MerchantOptions>(configuration.GetSection("Merchants"));
        services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });
        services.AddAuthorization();

        return services;
    }
}