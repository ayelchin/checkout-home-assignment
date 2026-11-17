namespace PaymentGateway.Api.Domain.Models;

public enum PaymentStatus
{
    None = 0,
    InProgress = 1,
    Authorized = 2,
    Declined = 3,
    Rejected = 4
}