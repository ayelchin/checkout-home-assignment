using Microsoft.OpenApi.Models;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Dto.CreatePayment;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace PaymentGateway.Api.OpenApi;

public sealed class CreatePaymentOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType != typeof(PaymentsController)
            || context.MethodInfo.Name != nameof(PaymentsController.PostPaymentAsync))
        {
            return;
        }

        ConfigureIdempotencyHeader(operation);
        ConfigureRequestBody(operation, context);
        ConfigureResponses(operation);
    }

    private static void ConfigureIdempotencyHeader(OpenApiOperation operation)
    {
        OpenApiParameter key = operation.Parameters.Single(parameter => parameter.Name == ApiHeaderNames.IdempotencyKey);
        key.Schema = new OpenApiSchema { Type = "string", MaxLength = 255 };
        key.Description = "Optional merchant-scoped, case-sensitive key, at most 255 characters. Missing or blank means an independent attempt. Reusing a key returns its original payment, even if payment details differ. Concurrent matching requests share one payment attempt. Key lookup works only while this application instance is running; restart loses payments and replay protection.";
    }

    private static void ConfigureRequestBody(OpenApiOperation operation, OperationFilterContext context)
    {
        OpenApiSchema schema = context.SchemaGenerator.GenerateSchema(typeof(CreatePaymentRequestDto), context.SchemaRepository);
        OpenApiSchema request = context.SchemaRepository.Schemas[schema.Reference.Id];
        request.Required = new HashSet<string> { "cardNumber", "cvv", "expiryMonth", "expiryYear", "currency", "amount" };
        request.Properties["cardNumber"].Description = "14-19 ASCII digits as text. No Luhn or network checks. Payments retain only last four digits; no raw PAN.";
        request.Properties["cvv"].Description = "3-4 ASCII digits as text. Transient; never retained.";
        request.Properties["expiryMonth"].Description = "1-12. Card remains valid throughout its expiry month, based on UTC.";
        request.Properties["currency"].Description = "Exactly uppercase GBP, USD, or EUR.";
        request.Properties["amount"].Description = "Signed 32-bit integer in minor units. Positive 1-2147483647; zero/negative are registered rejections. Fractional/overflow input is a basic failure.";
        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = schema }
            }
        };
    }

    private static void ConfigureResponses(OpenApiOperation operation)
    {
        operation.Responses["201"].Description = "Registered merchant-owned payment, initially InProgress while business validation and bank authorization are pending. Location points to GET /api/payments/{id}. Bank approval: Authorized/failure_reason 0; refusal: Declined/failure_reason 1. Complete domain-invalid details: Rejected/failure_reason 6 with all field errors and no bank call. HTTP, connection, timeout and unexpected bank failures are stored as Declined/failure_reason 2 (BankUnavailable). GET preserves the same attempt and outcome; other outcomes have empty errors.";
        operation.Responses["201"].Headers["Location"] = new OpenApiHeader
        {
            Description = "Retrieval route for this payment.",
            Schema = new OpenApiSchema { Type = "string" }
        };
        operation.Responses["422"].Description = "Basic validation fails before payment registration and without a bank call. Only error_type=request_invalid and a flat deduplicated error_codes array; no submitted values or parser text. request_body_invalid: body binding or JSON conversion failure, including fractional/overflow/quoted integers. payment_source_required, cvv_required, currency_required: missing/null/blank strings. payment_source_invalid: not 14-19 ASCII digits. expiry_month_required, expiry_year_required, amount_required: missing/null integers. Bound models report detected validation errors; binding failures need not report every field error. After body validation, the MVC action filter rejects nonblank keys over 255 characters with idempotency_key_invalid.";
    }
}