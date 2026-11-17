using System.Collections.Concurrent;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using PaymentGateway.Api.Authentication;
using PaymentGateway.Api.Dto;
using PaymentGateway.Api.Logging;

namespace PaymentGateway.Api.Filters;

public sealed class PaymentIdempotencyFilter(ILogger<PaymentIdempotencyFilter> logger) : IAsyncActionFilter
{
    private const string InvalidIdempotencyKey = "idempotency_key_invalid";

    private readonly ConcurrentDictionary<(string MerchantId, string Key), PaymentIdempotencyEntry> _entries = new();

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? key = context.ActionArguments.TryGetValue("idempotencyKey", out object? keyValue)
            ? keyValue as string
            : null;

        if (string.IsNullOrWhiteSpace(key))
        {
            await next();
            return;
        }

        if (key.Length > 255)
        {
            logger.LogInformation(LogEvents.ValidationRejected,
                "Payment request rejected: {ValidationCode}", InvalidIdempotencyKey);
            RequestInvalidResponseDto response = new([InvalidIdempotencyKey]);
            context.Result = new UnprocessableEntityObjectResult(response);
            return;
        }

        (string MerchantId, string Key) cacheKey = (context.HttpContext.User.GetMerchantId(), key);
        PaymentIdempotencyEntry candidate = new();
        PaymentIdempotencyEntry entry = _entries.GetOrAdd(cacheKey, candidate);
        if (!ReferenceEquals(candidate, entry))
        {
            logger.LogInformation(LogEvents.IdempotencyKeyMatched,
                "Matching idempotency key found for merchant {MerchantId}; replaying payment response", cacheKey.MerchantId);
            context.Result = await entry.Result.Task;
            logger.LogInformation(LogEvents.IdempotencyReplayed,
                "Payment response replayed for merchant {MerchantId}", cacheKey.MerchantId);
            return;
        }

        try
        {
            ActionExecutedContext executed = await next();
            if (executed.Exception is not null && !executed.ExceptionHandled)
            {
                RemoveEntry(cacheKey, entry);
                entry.Result.TrySetResult(new StatusCodeResult(StatusCodes.Status500InternalServerError));
                return;
            }

            entry.Result.TrySetResult(executed.Result ?? new EmptyResult());
        }
        catch
        {
            RemoveEntry(cacheKey, entry);
            entry.Result.TrySetResult(new StatusCodeResult(StatusCodes.Status500InternalServerError));
            throw;
        }
    }

    private void RemoveEntry((string MerchantId, string Key) key, PaymentIdempotencyEntry entry)
    {
        ((ICollection<KeyValuePair<(string MerchantId, string Key), PaymentIdempotencyEntry>>)_entries)
            .Remove(new KeyValuePair<(string MerchantId, string Key), PaymentIdempotencyEntry>(key, entry));
    }
}
