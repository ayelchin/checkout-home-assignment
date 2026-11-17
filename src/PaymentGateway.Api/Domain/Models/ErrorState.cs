namespace PaymentGateway.Api.Domain.Models;

public sealed record ErrorState(string Field, string Code, string Message);