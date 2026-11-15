namespace PaymentGateway.Api.UnitTests.Infrastructure;

internal sealed class FixedTimeProvider : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2030, 11, 15, 0, 0, 0, TimeSpan.Zero);
}