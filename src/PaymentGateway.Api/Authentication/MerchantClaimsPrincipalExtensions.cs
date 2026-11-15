using System.Security.Claims;

namespace PaymentGateway.Api.Authentication;

public static class MerchantClaimsPrincipalExtensions
{
    public static string GetMerchantId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}