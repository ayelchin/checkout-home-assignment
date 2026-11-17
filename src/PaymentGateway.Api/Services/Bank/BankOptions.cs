namespace PaymentGateway.Api.Services.Bank;

public sealed class BankOptions
{
    public const string Section = "Bank";
    public double ResponseTimeoutSeconds { get; set; } = 5;
}