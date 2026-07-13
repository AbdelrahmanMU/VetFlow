# API Standards

> **Status: Draft.** Executable engineering contract — not documentation.
> The decisions and their rationale are in
> [ADR-0015](../decisions/ADR-0015-api-contract.md). This document contains the
> enforceable rules and the canonical shapes.

**Defaults:** Scope = `API` · Stability = `Stable` · Depends On = none ·
Class = `Mandatory` · Severity = `Error`.
**Severity policy** and **exception process**: ADR-0017 §7; exceptions only via
the register below.

## Resources and naming

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-API-001 | Resources are plural, kebab-case nouns (`/products`, `/purchase-orders`) | Mandatory | Error | Automatic | Contract test (OpenAPI lint) | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-002 | **No verbs in paths.** `/createProduct`, `/updateProduct`, `/deleteProduct`, `/getProductList` are prohibited | Mandatory | Error | Automatic | Contract test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-003 | State transitions are sub-resources: `POST /products/{id}/activate`, `POST /sales/{id}/complete` | Mandatory | Error | Semi-Automatic | Contract test + review | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-004 | Resource names come from the English side of `GLOSSARY.md`; a term not in the glossary is not a resource | Mandatory | Error | Semi-Automatic | CI script + review | CI | [ADR-0002](../decisions/ADR-0002-documentation-language.md) |
| STD-API-005 | Every route is versioned: `/api/v1/…`. No unversioned endpoint ships | Mandatory | Error | Automatic | Contract test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-006 | A published contract is never broken within a version; breaking changes require a new version and owner approval | Mandatory | Error | Semi-Automatic | Contract test (breaking-change diff) + review | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |

**Canonical verb table:**

| Method | Path | Meaning |
|---|---|---|
| `GET` | `/api/v1/products` | list (paginated) |
| `GET` | `/api/v1/products/{id}` | read one |
| `POST` | `/api/v1/products` | create |
| `PUT` | `/api/v1/products/{id}` | full update |
| `PATCH` | `/api/v1/products/{id}` | partial update |
| `DELETE` | `/api/v1/products/{id}` | delete/deactivate per business rules |
| `POST` | `/api/v1/products/{id}/activate` | state transition |

## Errors

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Depends On | Source |
|---|---|---|---|---|---|---|---|---|
| STD-API-010 | **Every non-2xx response is RFC 9457 `application/problem+json`.** No bespoke error shapes | Mandatory | Error | Automatic | Integration test | CI | — | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-011 | Every business failure carries `errorCode` from the Error Catalog; clients branch on the code, never on text | Mandatory | Error | Automatic | Integration test | CI | STD-BE-032 | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-API-012 | Translation happens only in middleware; handlers and the domain never produce user-facing text | Mandatory | Error | Automatic | Architecture test | Architecture test | STD-BE-030 | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-API-013 | Stack traces and infrastructure details are never returned; unexpected failures are an opaque 500 with a `traceId` | Mandatory | Error | Automatic | Integration test | CI | — | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-014 | Validation failures use the fixed `errors` extension shape, on every endpoint | Mandatory | Error | Automatic | Integration test | CI | STD-API-010 | [ADR-0015](../decisions/ADR-0015-api-contract.md) |

**Canonical error shape:**

```json
{
  "type": "https://vetflow.app/errors/VTF-CAT-031",
  "title": "Product cannot be deactivated while an open package exists",
  "status": 409,
  "detail": "<localized — Arabic by default>",
  "instance": "/api/v1/products/42/deactivate",
  "errorCode": "VTF-CAT-031",
  "traceId": "00-4bf92f…-01"
}
```

**Canonical validation shape:**

```json
{
  "type": "https://vetflow.app/errors/VTF-VAL-001",
  "status": 400,
  "errorCode": "VTF-VAL-001",
  "errors": { "name": ["<localized>"], "price": ["<localized>"] },
  "traceId": "00-4bf92f…-01"
}
```

## Collections

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-API-020 | Collection endpoints are paginated by default; an unpaginated collection is a registered exception | Mandatory | Error | Automatic | Contract test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-021 | Pagination is offset-based: `?page=&pageSize=`, with a server-side cap on `pageSize` | Mandatory | Error | Automatic | Integration test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-022 | The collection envelope is fixed: `{ items, page, pageSize, totalCount }` | Mandatory | Error | Automatic | Contract test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-023 | Filtering and sorting use explicit whitelisted parameters per endpoint; no generic query language | Mandatory | Error | Semi-Automatic | Contract test + review | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |

## Consistency, time, correlation, idempotency

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-API-030 | All timestamps are UTC ISO 8601; the API never returns localized or pre-formatted dates | Mandatory | Error | Automatic | Contract test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-031 | Money is decimal + currency code; floating-point money is prohibited | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0007](../decisions/ADR-0007-localization-architecture.md) |
| STD-API-032 | Bodies are camelCase JSON | Mandatory | Error | Automatic | Contract test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-033 | Every request carries a `TraceId` and a `CorrelationId`, propagated to logs, audit, Problem Details, and traces | Mandatory | Error | Automatic | Integration test | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-034 | Critical commands (checkout, purchase receiving, cash-session close) must remain able to accept an `Idempotency-Key`: no design may assume exactly-once delivery. *Implementation is not required in MVP* | Mandatory | Warning | Manual | Engineering review | Review | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-API-035 | Endpoints expose no domain entity; request and response types are DTOs | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-API-036 | Every endpoint is covered by an integration test asserting status, shape, and error contract | Mandatory | Error | Automatic | CI (coverage of routes) | CI | [ADR-0016](../decisions/ADR-0016-testing-and-architecture-enforcement.md) |

## Exception Register

| STD | Scope of exception | Reason | Approved by | Date |
|---|---|---|---|---|
| — | *(none)* | | | |

## Tombstones

| STD | Removed | Reason |
|---|---|---|
| — | *(none)* | |
