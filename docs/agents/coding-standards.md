# C# Coding Standards

Keep this coding-challenge solution simple and easy to follow. Prefer the smallest design that meets the requirement.

## Design principles

- **SOLID:** Keep responsibilities cohesive, depend on focused abstractions at application boundaries, and make implementations substitutable without changing caller behavior. Split or generalize code only when doing so makes a real change easier to understand, test, or extend; do not add layers just to satisfy a principle by name.
- **KISS:** Choose the simplest design that meets the stated requirement and is easy to understand. Prefer direct code over indirection when both communicate the behavior clearly.
- **YAGNI:** Balance reliable behavior for the requirements in scope with a design that stays small. Handle relevant failure cases and preserve required guarantees, but do not build speculative features, cover irrelevant gaps, or add complexity for scenarios the solution does not need to support.

## Formatting

- Treat the existing `.editorconfig` as the formatting source of truth. Never edit it.
- When formatting needs correction, suggest running `dotnet format PaymentGateway.sln` to apply the configured rules.

## Application structure

Keep responsibilities in their corresponding areas:

- **Controllers** handle HTTP input and output and delegate application work.
- **UseCases** implement application operations and coordinate the work needed to complete them.
- **Services** hold supporting behavior used by application operations.
- **Repositories** handle data access. Repository classes belong in a `Repositories` area, never in `Services`.

Use interfaces at boundaries between these areas so implementations can be substituted in tests. Keep interfaces focused on the operation the caller needs. Do not add interfaces to simple models or implementation-only helpers.

Avoid extra layers, projects, abstractions, or patterns unless a concrete requirement benefits from them. Keep names and structure consistent with the existing solution where they do not conflict with these standards.

### Naming and discoverability

- Name use-case classes with the `Handler` suffix, with matching interfaces such as `CreatePaymentHandler` and `ICreatePaymentHandler`.
- Name supporting components by their responsibility, using suffixes such as `Client`, `Validator`, `Reader`, `Coordinator`, `Generator`, and `Repository`. In-memory repository implementations use the `InMemory` prefix.
- Name HTTP contracts with the `Dto` suffix and place them in the dedicated `Dto` area. Keep HTTP contracts out of application handlers and payment-rule validation; map them at the HTTP boundary.
- Put exactly one declared type in each file, including interfaces, records, and enums. The filename must match the type name.
- Keep interfaces beside implementations. Group files by component inside the existing areas, keep operation-specific input/result types beside their handler, and match namespaces to folders.
- Give synchronous methods synchronous names; reserve `Async` for asynchronous operations.

## Testing

- Use xUnit. Name tests after the exact operation under test: `Operation_WhenCondition_ShouldExpectedOutcome`, for example `GetPaymentAsync_WhenPaymentExists_ShouldReturnPayment`.
- Test observable behavior rather than private implementation details. Cover the normal path, meaningful boundary values, and relevant failure paths; skip speculative scenarios outside the requirements.
- For value assertions, assign the expected value to a clearly named `expected` variable and the observed value to a clearly named `actual` variable before asserting. This keeps the comparison easy to scan and its failure meaning clear.
- Keep each test focused on one behavior and one logical assertion. Use one assertion call per test. When one outcome includes several fields, group those field checks in one assertion (for example, with AwesomeAssertions); split distinct outcomes into separate tests.
- Structure every test with `// ARRANGE`, `// ACT`, and `// ASSERT` comments. Keep each section clear and local. Prefer small, readable duplication over helpers that obscure the behavior; use shared helpers only when they reduce noise without hiding setup or intent.
- Keep tests independent, fast, and deterministic. Avoid shared mutable state, ordering dependencies, and sleeps. Unit tests use in-memory substitutes instead of network calls or real external services. Integration tests may use local HTTP and the supplied bank simulator managed by Testcontainers, with bounded synchronization and isolated application state. Prefer fixed, readable input values; use randomness only when it adds useful coverage and make it reproducible.
- Write unit tests against the smallest useful unit, substituting in-memory fakes or stubs at boundaries as needed. Use local integration tests when framework or component wiring is part of the behavior; exercise the real application path while keeping dependencies local and self-contained.
- Assert application guarantees, not incidental framework behavior. Keep failure output informative through specific names and grouped assertions.
