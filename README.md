# Payment Gateway

A .NET 8 API for submitting card payments to the supplied acquiring-bank simulator
and retrieving their recorded outcomes. Built for the
[Checkout.com take-home challenge](https://github.com/cko-recruitment/).

## Architecture

One production project keeps the application small. Controllers handle HTTP;
create/get use-case handlers coordinate payments. Supporting services validate
payment details and call the bank. An in-memory repository stores merchant-owned
payments, and an MVC action filter handles idempotent replay.

```text
HTTP request -> Controller -> Use-case handler -> Validation / Bank service
                                    |
                             Payment repository
```

Unit and HTTP integration tests live in separate projects under `test/`.

## Start the solution

Install Docker Desktop with Linux containers enabled and Docker Compose. From the
repository root:

```powershell
docker compose up --build -d
docker compose logs -f paymentgateway.api
```

Wait for the gateway's listening message, then press Ctrl+C to leave the log view;
the containers keep running. The gateway is at **http://localhost:5055** and
Swagger is at **http://localhost:5055/swagger**. The simulator uses host port 8080
(administration: 2525); the gateway reaches it through the Compose network.

Compose automatically loads the development override and two sample merchant API
keys. This demo uses HTTP and synthetic card details, without developer certificates.
Real hosting would require HTTPS and separately configured secrets. Keep
`imposters/` and `.editorconfig` unchanged.

## Try the API

Run these examples in PowerShell 7 after startup. Amounts use minor currency units:
`100` means £1.00 for GBP.

### Create a payment

```powershell
$gateway = 'http://localhost:5055'
$headers = @{
    'X-Api-Key' = 'local-merchant-one-key'
    'Idempotency-Key' = [guid]::NewGuid().ToString()
}
$body = @{
    cardNumber = '0000000000000011'
    cvv = '123'
    expiryMonth = 12
    expiryYear = [DateTime]::UtcNow.Year + 1
    currency = 'GBP'
    amount = 100
} | ConvertTo-Json

$response = Invoke-WebRequest "$gateway/api/payments" -Method Post `
    -Headers $headers -ContentType 'application/json' -Body $body
$payment = $response.Content | ConvertFrom-Json
$response.StatusCode  # 201
$payment             # Authorized, failure_reason: 0
```

### Retrieve the payment

The creation response includes a `Location` header pointing to the stored payment.

```powershell
$location = [string]$response.Headers.Location[0]
Invoke-RestMethod "$gateway$location" -Headers $headers  # 200, same payment
```

### Replay the attempt

```powershell
$replayed = Invoke-RestMethod "$gateway/api/payments" -Method Post `
    -Headers $headers -ContentType 'application/json' -Body $body
$replayed.id -eq $payment.id  # True; no second bank call
```

Use a new idempotency key for each new attempt. Change the synthetic card's final
digit to try the simulator outcomes:

| Final digit | Payment status | `failure_reason` |
| --- | --- | --- |
| Odd, e.g. `1` | Authorized | 0: None |
| Even and nonzero, e.g. `2` | Declined | 1: BankDeclined |
| `0` (simulator returns 503) | Declined | 2: BankUnavailable |

Registered attempts return 201 and can be retrieved with GET. Complete details
that fail payment rules produce `Rejected` with reason 6 (`InvalidPaymentDetails`).
Malformed or incomplete requests return 422 without creating a payment. Missing or
invalid API keys return 401; an unknown or another merchant's payment returns 404.
The second development key is `local-merchant-two-key`.

Swagger documents the full contract, validation errors, and headers.

## Run the tests

Install the .NET 8 SDK and keep Docker Desktop running:

```powershell
dotnet build PaymentGateway.sln
dotnet test PaymentGateway.sln
```

The integration suite starts and removes its own simulator container; no manual
Compose startup is needed. See [test/README.md](test/README.md) for individual
projects and controlled integration tests without Docker.

## Design considerations and assumptions

1. **Keep the architecture small.** One API project separates HTTP handling,
   payment operations, bank communication, and storage without extra application layers.
2. **Use in-memory storage for the exercise.** Payments and idempotency keys are
   lost on restart and are not shared between instances. Entries have no expiry.
3. **Separate request errors from payment rejection.** Unusable requests return
   422; complete requests that fail payment rules become retrievable Rejected payments.
4. **Use whole minor units for money.** Accepted amounts are positive 32-bit
   integers. Supported currencies are GBP, USD, and EUR. Cards remain valid through
   their printed expiry month, using UTC; Luhn and card-brand checks are outside scope.
5. **Keep merchants' payments separate.** API keys identify merchants, and a
   merchant cannot retrieve another merchant's payment. Keys are simple local
   configuration rather than a merchant onboarding system.
6. **Retain only necessary card details.** Full card numbers and CVVs are transient
   inputs, excluded from stored payment records, responses, and application logs.
   Responses expose only the last four digits. This exercise makes no PCI compliance
   claim; use synthetic data with the simulator.
7. **Use one idempotency key per attempt.** Within one running instance, concurrent
   and later requests with the same merchant/key share the original result, even if
   details change. Body validation still runs before replay. No original request or
   request fingerprint is retained for comparison.
8. **Make bank uncertainty explicit.** Technical failures become Declined with
   BankUnavailable: authorization was not confirmed, but the bank may have processed
   the attempt. The configurable timeout defaults to five seconds. There are no
   automatic retries, redirects, or later reconciliation. Successful bank responses
   are assumed to follow the supplied contract.
9. **Finish accepted attempts after a disconnect.** Processing continues within
   the bank timeout while the process runs. A payment is stored as InProgress before
   payment-rule validation and bank authorization, making pending attempts retrievable.
10. **Test behavior and make failures traceable.** Unit tests cover focused rules;
    HTTP integration tests cover payment flows, failures, and concurrent requests.
    Structured logs connect requests and payment outcomes without sensitive inputs.

Capture, settlement, refunds, durable storage, and distributed duplicate-payment
protection are outside this exercise. The [ADRs](docs/adr/) record the main trade-offs.

## Stop the solution

```powershell
docker compose down
```
