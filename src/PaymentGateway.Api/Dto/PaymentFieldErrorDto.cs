namespace PaymentGateway.Api.Dto;

public sealed record PaymentFieldErrorDto(string Field, string Code, string Message);