# Record technical bank failures as declined with BankUnavailable

Status: revised on 2026-11-16 by the payment lifecycle design session.

All technical bank failures, including HTTP errors, connection failures, response
timeouts and unexpected or unreadable responses, produce Declined with the single
BankUnavailable failure reason. Explicit bank refusal remains BankDeclined.
This replaces the earlier distinct BankResponseTimeout and AuthorizationUnknown
reasons to keep the public model small; logs retain the technical failure category.
Declined means authorization was not confirmed: the bank may have processed the
attempt. No automatic retry, reconciliation or later outcome rewrite is performed.
The acquiring bank service owns this mapping and exception handling.
