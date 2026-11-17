# Payment Gateway

A payment gateway lets a merchant request card payment authorization from an acquiring bank.

## Language

**Merchant**:
The seller requesting a payment for goods or services.

**Shopper**:
The individual buying goods or services from a merchant.

**Acquiring bank**:
The bank through which the gateway requests card payment authorization.

**Authorized**:
A payment outcome confirming that the acquiring bank approved the payment request.

**Rejected**:
A payment request refused by the gateway because its supplied information is invalid, before contacting the acquiring bank.

**Declined**:
A payment attempt for which the gateway did not confirm authorization, including bank refusal, unavailability, or a response timeout. The bank may have authorized an attempt whose response was lost; the accompanying failure reason distinguishes the gateway's observation.

**In progress**:
A payment attempt that has been stored before business validation or bank authorization and has not yet reached a final outcome. It can be retrieved while authorization is pending.

**Minor currency unit**:
The denomination used to express a payment amount as an integer, such as cents for USD or pence for GBP.

**Supported currency**:
A currency the gateway accepts for payment. The gateway currently supports USD, GBP, and EUR.

**Idempotency key**:
A merchant-scoped client key identifying one payment attempt. Reusing it returns the same payment attempt and stored outcome, regardless of changes to the submitted payment details. Coordination is process-local and lasts only for the running application instance.

**Failure reason**:
The explanation for a payment rejection or failure to confirm authorization. Bank refusal, technical bank failure, and invalid payment details are distinct; None means no failure has been recorded.
_Avoid_: Reason

**Bank reference**:
The authorization code supplied by the acquiring bank for a payment.
