using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Dto;

public sealed class RequestInvalidResponseDto(string[] errorCodes)
{
    [JsonPropertyName("error_type")]
    public string ErrorType => "request_invalid";
    [JsonPropertyName("error_codes")]
    public string[] ErrorCodes { get; } = errorCodes;
}