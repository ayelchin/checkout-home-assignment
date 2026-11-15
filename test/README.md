# Running the tests

Both projects target .NET 8 and use xUnit. Run from the repository root:

```powershell
dotnet build PaymentGateway.sln
dotnet test PaymentGateway.sln
```

Docker Desktop must be running with Linux containers enabled for the full suite.
Testcontainers starts one shared `bbyars/mountebank:2.8.1` simulator, copies the
supplied `imposters/` configuration unchanged, waits for its HTTP endpoint, assigns
a free host port, and removes the container after its test collection finishes.
No manual Compose startup is needed; an existing development simulator can stay
running. Docker/image startup failures fail the tests with a prerequisite message.

Run either project independently:

```powershell
dotnet test test/PaymentGateway.Api.UnitTests/PaymentGateway.Api.UnitTests.csproj
dotnet test test/PaymentGateway.Api.IntegrationTests/PaymentGateway.Api.IntegrationTests.csproj
```

For an explicit run of only the controlled integration tests without Docker:

```powershell
dotnet test test/PaymentGateway.Api.IntegrationTests/PaymentGateway.Api.IntegrationTests.csproj --filter "Dependency!=Docker"
```

This filter deliberately excludes the five simulator cases; it is not full-suite
validation. Normal runs never silently skip them or replace the simulator.

Unit tests mirror production folders and cover payment rules, handler outcomes,
bank request mapping, and payment repository guarantees. Idempotency behavior is
tested through the HTTP integration path. The bank is substituted, time is fixed,
and unit tests need no network or Docker.

Integration tests follow `UseCases/CreatePayment` and `UseCases/GetPayment`, with
separate authentication and OpenAPI checks. Each test has a fresh in-process
gateway host and in-memory state. The simulator collection checks supported bank
outcomes through POST/Location/GET. A small local HTTP bank stub checks controlled
failures, redirects, concurrency and disconnects using the real bank client.
Timeout tests advance an injected clock; synchronization uses bounded signals
instead of sleeps. Shared infrastructure lives in `Infrastructure/`.

The layout follows [Microsoft's integration-testing guidance](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0)
and [Testcontainers' ASP.NET Core pattern](https://dotnet.testcontainers.org/examples/aspnet/).

Logging integration cases exercise application EventIds and outcomes, HTTP summaries,
correlation, validation/replay bypasses, safe error logs and generic 500 responses,
secret exclusion, and completion after client disconnects. A captured ILogger
provider observes structured fields and scopes through real HTTP requests.

Verified on 2026-11-16: 45 unit cases and 87 integration cases passed, including
all five simulator cases, with zero failures or skips.
