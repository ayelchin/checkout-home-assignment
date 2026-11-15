using Microsoft.AspNetCore.Mvc.Filters;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

internal sealed class RequestArrivalFilter(TaskCompletionSource secondRequestReachedFilters) : IAsyncActionFilter, IOrderedFilter
{
    private int _requests;

    public int Order => -1;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Path == "/api/payments" && Interlocked.Increment(ref _requests) == 2)
        {
            secondRequestReachedFilters.TrySetResult();
        }

        await next();
    }
}