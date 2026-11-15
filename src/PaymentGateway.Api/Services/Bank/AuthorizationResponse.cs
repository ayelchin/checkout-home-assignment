using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Services.Bank;

public sealed record AuthorizationResponse(
    bool Authorized,
    [property: JsonPropertyName("authorization_code")] string AuthorizationCode);