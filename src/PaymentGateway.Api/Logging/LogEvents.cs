namespace PaymentGateway.Api.Logging;

public static class LogEvents
{
    public static readonly EventId PaymentAttemptStarted = new(1000, nameof(PaymentAttemptStarted));
    public static readonly EventId PaymentAttemptCompleted = new(1001, nameof(PaymentAttemptCompleted));
    public static readonly EventId PaymentLookupCompleted = new(1002, nameof(PaymentLookupCompleted));
    public static readonly EventId IdempotencyReplayed = new(1003, nameof(IdempotencyReplayed));
    public static readonly EventId ValidationRejected = new(1004, nameof(ValidationRejected));
    public static readonly EventId BankAuthorizationFailed = new(1005, nameof(BankAuthorizationFailed));
    public static readonly EventId UnhandledException = new(1006, nameof(UnhandledException));
    public static readonly EventId ClientDisconnected = new(1007, nameof(ClientDisconnected));
}