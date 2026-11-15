# Superseded: store payment idempotency keys with payment records

Status: superseded by [ADR-0002](0002-mvc-owned-payment-idempotency.md) on 2026-11-16.

This decision placed key reservation and replay in the payment repository. It was reversed after reviewing the later payment lifecycle decisions: the MVC filter now owns key reservation and HTTP result sharing, while the use case and payment repository store and complete the payment attempt without key coordination.
