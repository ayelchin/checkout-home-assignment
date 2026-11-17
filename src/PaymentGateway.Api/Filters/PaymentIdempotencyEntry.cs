using Microsoft.AspNetCore.Mvc;

namespace PaymentGateway.Api.Filters;

public sealed class PaymentIdempotencyEntry
{
    public TaskCompletionSource<IActionResult> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}