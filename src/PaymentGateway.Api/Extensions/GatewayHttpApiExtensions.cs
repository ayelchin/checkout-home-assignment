using System.Text.Json.Serialization;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.OpenApi;

namespace PaymentGateway.Api.Extensions;

public static class GatewayHttpApiExtensions
{
    public static IServiceCollection AddGatewayHttpApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict)
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = RequestValidationResponseFactory.Create);
        services.AddPaymentGatewaySwagger();

        return services;
    }
}