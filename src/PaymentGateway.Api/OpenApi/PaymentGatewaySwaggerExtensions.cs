using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Domain.Models;

namespace PaymentGateway.Api.OpenApi;

public static class PaymentGatewaySwaggerExtensions
{
    public static IServiceCollection AddPaymentGatewaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // Keep public schema names stable when implementation types gain the Dto suffix.
            options.CustomSchemaIds(type => type.Name.EndsWith("Dto", StringComparison.Ordinal) ? type.Name[..^3] : type.Name);
            options.OperationFilter<CreatePaymentOperationFilter>();
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "PaymentGateway.Api.xml"));
            options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = ApiHeaderNames.ApiKey,
                Description = "Configured merchant API key. Missing or invalid credentials return 401. Ownership is derived exclusively from this key."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }] = Array.Empty<string>()
            });
            options.MapType<PaymentFailureReason>(() => new OpenApiSchema
            {
                Type = "integer",
                Format = "int32",
                Enum = new List<IOpenApiAny> { new OpenApiInteger(0), new OpenApiInteger(1), new OpenApiInteger(2), new OpenApiInteger(6) },
                Description = "Numeric failure_reason: 0=None (no recorded failure); 1=BankDeclined (explicit refusal); 2=BankUnavailable (HTTP, connection, timeout or unexpected bank failure); 6=InvalidPaymentDetails. Values 3, 4, 5 and 7 are unused. Declined means authorization was not confirmed; the bank may have processed the attempt. Retrieval preserves the recorded outcome."
            });
        });
        return services;
    }
}