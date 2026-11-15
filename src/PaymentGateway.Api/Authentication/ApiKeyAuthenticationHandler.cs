using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace PaymentGateway.Api.Authentication;

public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    private readonly IReadOnlyDictionary<string, string> _merchants;

    public ApiKeyAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IOptions<MerchantOptions> merchants)
        : base(options, logger, encoder)
    {
        _merchants = new Dictionary<string, string>(merchants.Value.ApiKeys, StringComparer.Ordinal);
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiHeaderNames.ApiKey, out Microsoft.Extensions.Primitives.StringValues keys)
            || keys.Count != 1 || string.IsNullOrWhiteSpace(keys[0])
            || !_merchants.TryGetValue(keys[0]!, out string? merchantId))
        {
            return Task.FromResult(AuthenticateResult.Fail("A valid API key is required."));
        }

        ClaimsIdentity identity = new(new[] { new Claim(ClaimTypes.NameIdentifier, merchantId) }, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}