using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Domain.Models;
using PaymentGateway.Api.Dto;
using PaymentGateway.Api.Dto.CreatePayment;
using PaymentGateway.Api.Dto.GetPayment;
using PaymentGateway.Api.Filters;
using PaymentGateway.Api.UseCases.CreatePayment;
using PaymentGateway.Api.UseCases.GetPayment;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class PaymentsController(IGetPaymentHandler getPayment, ICreatePaymentHandler createPayment) : ControllerBase
{
    private readonly IGetPaymentHandler _getPayment = getPayment;

    private readonly ICreatePaymentHandler _createPayment = createPayment;

    /// <summary>Create a merchant-owned payment authorization attempt.</summary>
    /// <remarks>Supply X-Api-Key. Basic invalid input returns 422 before registration. Complete invalid payment details return a retrievable Rejected payment with failure_reason 6 and safe field/code/message errors. Full PAN and CVV are transient. Merchant identity is derived only from the API key. Payments are initially stored as InProgress before business validation or bank authorization. Bank HTTP failures (including 503 and refused redirects), connection failures, timeouts and unexpected bank failures create Declined with failure_reason 2=BankUnavailable. Outcomes return 201 with Location and empty errors. Bank:ResponseTimeoutSeconds configures the response timeout (default five seconds). No automatic retries or redirects are performed. Declined means authorization was not confirmed: the bank may have processed the attempt. GET preserves the recorded outcome even after a late bank response.</remarks>
    [HttpPost]
    [ServiceFilter(typeof(PaymentIdempotencyFilter))]
    [ProducesResponseType(typeof(RequestInvalidResponseDto), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(CreatePaymentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CreatePaymentResponseDto>> PostPaymentAsync(
        [FromBody] CreatePaymentRequestDto request,
        [FromHeader(Name = ApiHeaderNames.IdempotencyKey)] string? idempotencyKey)
    {
        PaymentDetailsInput details = MapPaymentDetails(request);
        CreatePaymentInput command = new(User.GetMerchantId(), details);
        PaymentRecord payment = await _createPayment.ExecuteAsync(command);
        CreatePaymentResponseDto response = PaymentResponseMapper.ToPostResponse(payment);

        return Created($"/api/payments/{response.Id}", response);
    }

    private static PaymentDetailsInput MapPaymentDetails(CreatePaymentRequestDto request)
    {
        return new PaymentDetailsInput(
            request.CardNumber!,
            request.Cvv!,
            request.ExpiryMonth!.Value,
            request.ExpiryYear!.Value,
            request.Currency!,
            request.Amount!.Value);
    }

    /// <summary>Retrieve a payment owned by the authenticated merchant.</summary>
    /// <remarks>Supply X-Api-Key. Other merchants' payments and missing IDs both return 404.</remarks>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<GetPaymentResponseDto> GetPayment(Guid id)
    {
        PaymentRecord? payment = _getPayment.Execute(User.GetMerchantId(), id);

        return payment is null ? NotFound() : Ok(PaymentResponseMapper.ToGetResponse(payment));
    }
}