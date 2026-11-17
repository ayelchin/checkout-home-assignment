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
The simulator starts lazily when the first simulator case requests it.
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

This filter deliberately excludes the three simulator cases; it is not full-suite
validation. Normal runs never silently skip them or replace the simulator.

Unit tests mirror production folders and cover payment rules, handler outcomes,
bank request mapping, and payment repository guarantees. Idempotency behavior is
tested through the HTTP integration path. The bank is substituted, time is fixed,
and unit tests need no network or Docker.

Integration tests follow `UseCases/CreatePayment` and `UseCases/GetPayment`, with
separate OpenAPI checks. Each payment endpoint has one missing-key 401 case beside
its use-case tests; `GetPaymentTests` also checks an authenticated unknown URL returns
404. Each test has a fresh in-process gateway host and in-memory state.
The simulator collection checks supported bank
outcomes through POST/Location/GET. A small local HTTP bank stub checks controlled
failures, redirects, concurrency and disconnects using the real bank client.
Timeout tests advance an injected clock; synchronization uses bounded signals
instead of sleeps. Shared infrastructure lives in `Infrastructure/`.

The layout follows [Microsoft's integration-testing guidance](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-8.0)
and [Testcontainers' ASP.NET Core pattern](https://dotnet.testcontainers.org/examples/aspnet/).

Create-payment tests are grouped by observable behavior in four files:

- `AuthorizedTests`: simulator authorization, Location, safe card details, and POST/GET consistency.
- `DeclinedTests`: simulator refusal/unavailability and controlled HTTP failures,
  redirects, connection loss, and late bank responses after timeout.
- `RejectedTests`: persisted business rejection (201 with a retrievable Rejected
  payment), request-validation cases (422 without a payment), and missing API key (401).
- `IdempotencyTests`: replay despite changed details, rejected replay, key boundaries
  and merchant scope, concurrent duplicates, client disconnects, and key reuse after
  validation failure.

Each scenario groups its response and essential bank-call observations in one
assertion instead of repeating the same requests in separate tests. The real bank
HTTP client stays in the path; local HTTP stubs provide controlled edge cases and
bank request counts for deduplication guarantees. Only the unexpected validator
exception case substitutes application services, using a repository spy to locate
the retained InProgress payment and verify that retry releases the failed key.

Verified on 2026-11-17: 45 unit cases and 54 integration cases passed, including
all three simulator cases, with zero failures or skips. The Docker-free filter
passed all 51 remaining integration cases.
