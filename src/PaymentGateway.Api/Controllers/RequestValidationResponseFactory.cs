using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Dto;
using PaymentGateway.Api.Logging;

namespace PaymentGateway.Api.Controllers;

public static class RequestValidationResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        string[] codes = context.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage switch
            {
                "payment_source_required" or "payment_source_invalid" or "cvv_required"
                    or "expiry_month_required" or "expiry_year_required" or "currency_required"
                    or "amount_required" => error.ErrorMessage,
                _ => "request_body_invalid"
            })
            .Distinct()
            .ToArray();

        ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RequestValidationResponseFactory).FullName!);
        logger.LogInformation(LogEvents.ValidationRejected,
            "Payment request rejected: {ValidationCodes}", string.Join(",", codes));

        return new UnprocessableEntityObjectResult(new RequestInvalidResponseDto(codes));
    }
}