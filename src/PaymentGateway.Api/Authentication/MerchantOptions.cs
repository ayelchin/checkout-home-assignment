namespace PaymentGateway.Api.Authentication;

public sealed class MerchantOptions
{
    public Dictionary<string, string> ApiKeys { get; set; } = new(StringComparer.Ordinal);
}