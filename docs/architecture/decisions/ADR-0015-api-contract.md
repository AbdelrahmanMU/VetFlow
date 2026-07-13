# ADR-0015: API Contract Standards

- **Status:** Proposed
- **Date:** 2026-07-13
- **Governs:** the public shape of the API. Worked examples live in
  `standards/api-standards.md`; this ADR holds the decisions.

## Context

The API is a **product API** (ADR-0006): Angular is its first client, not its
only one. Contract decisions become expensive to reverse the moment a second
client exists, so they are decided once, here, and applied uniformly.

## Decision

### 1. REST conventions and naming

- Resources are **plural nouns**, kebab-case: `/api/v1/products`,
  `/api/v1/purchase-orders`.
- Standard verbs carry standard meaning:

  | Preferred | Meaning |
  |---|---|
  | `GET /products` | list (paginated) |
  | `GET /products/{id}` | read one |
  | `POST /products` | create |
  | `PUT /products/{id}` | full update |
  | `PATCH /products/{id}` | partial update |
  | `DELETE /products/{id}` | delete/deactivate per business rules |

- **State transitions are sub-resources, not verbs in the path:**
  `POST /products/{id}/activate`, `POST /sales/{id}/complete`.
- **Rejected — RPC-style paths:** `/createProduct`, `/updateProduct`,
  `/deleteProduct`, `/getProductList`. The verb belongs to HTTP, never to the
  URL.
- Resource names come from the English side of `docs/shared/GLOSSARY.md`. If a
  term is not in the glossary, it is not an API resource yet.

### 2. Versioning

**URL segment: `/api/v1/…`.** Visible in logs, trivially cacheable, obvious to
every client and every AI reading a request. Header and media-type versioning
are rejected as invisible cleverness with real client cost.

A new major version is created only for a breaking change; additive changes ship
inside the current version. Breaking a published contract requires owner
approval.

### 3. Errors — RFC 9457 Problem Details, always

**Every non-2xx response is an RFC 9457 `application/problem+json` document.**
No exceptions, no bespoke error shapes anywhere in the API.

```json
{
  "type": "https://vetflow.app/errors/VTF-CAT-031",
  "title": "Product cannot be deactivated while an open package exists",
  "status": 409,
  "detail": "<localized message — Arabic by default>",
  "instance": "/api/v1/products/42/deactivate",
  "errorCode": "VTF-CAT-031",
  "traceId": "…"
}
```

- `errorCode` is the stable code from the Error Catalog (ADR-0018) — the
  machine-readable contract. Clients branch on `errorCode`, never on `detail`.
- `detail` is localized user-facing text; the API is the **single translation
  point** (ADR-0018). Domain exceptions never carry text.
- **Stack traces and infrastructure exception details are never exposed.**
  Unexpected failures return an opaque 500 with a `traceId` and are logged.

### 4. Validation errors

Field-level validation failures use Problem Details with a fixed extension:

```json
{
  "type": "https://vetflow.app/errors/VTF-VAL-001",
  "status": 400,
  "errorCode": "VTF-VAL-001",
  "errors": { "name": ["<localized>"], "price": ["<localized>"] }
}
```

One shape for all validation failures, across every endpoint.

### 5. Collections — pagination, filtering, sorting

- **Collections are paginated by default.** An unpaginated collection endpoint
  is an explicit, documented exception.
- Offset pagination: `?page=1&pageSize=25`, `pageSize` capped server-side.
  Cursor pagination is rejected — wrong complexity for this domain's data.
- Fixed envelope:
  `{ "items": [...], "page": 1, "pageSize": 25, "totalCount": 137 }`
- **Filtering and sorting use explicit, whitelisted parameters per endpoint**
  (`?search=…&categoryId=…&sort=name&dir=asc`). Generic query languages
  (OData, GraphQL) are rejected: enormous surface, no client needs them.

### 6. Idempotency — the architecture must not prevent it

Critical operations must be **able** to accept an idempotency key, so that a
retried request cannot double-charge, double-receive, or double-close:

- Checkout (sale completion)
- Purchase receiving
- Cash session closing

The MVP is **not required to implement** idempotency keys. The requirement is
architectural: the command pipeline, the API contract, and the transaction
boundary must leave room for an `Idempotency-Key` header and a stored
request-result record to be added later **without redesign**. Nothing in the
design may assume that a command is only ever delivered once.

### 7. Correlation — every request is traceable

Every request carries a **`TraceId`** and a **`CorrelationId`**, and they flow
consistently through:

- logs (structured, on every entry),
- the audit trail,
- Problem Details responses (the `traceId` member),
- distributed tracing.

`TraceId` identifies one request; `CorrelationId` ties together the chain of
work a single user action caused. A failure a user reports must be findable from
either.

### 8. Time

**All API timestamps are UTC**, ISO 8601. The API never returns a local time,
never a client-specific calendar, never a formatted date. **Localization belongs
exclusively to the presentation layer** (ADR-0007) — the client formats; the API
states facts.

### 9. Consistency rules

- Money: decimal + currency code (`EGP` by default, ADR-0007). Never a float.
- IDs are opaque to clients.
- All user-facing text in responses is localizable — including error copy.
- Request/response bodies are camelCase JSON.

## Alternatives Considered

- **Backend-for-frontend shaped to Angular screens:** fastest now, forces a
  redesign or a second API when the next client arrives (contradicts ADR-0006).
- **GraphQL:** one flexible endpoint; rejected — large surface, weak caching
  story, no MVP client that benefits.
- **Bespoke error format:** marginally prettier, throws away an ecosystem
  standard that every future client and AI agent already understands.

## Consequences

- One error shape and one collection shape across the entire API — the single
  rule that prevents the most common form of API drift.
- The Error Catalog (ADR-0018) becomes part of the published contract:
  business rule → error code → HTTP status → localized message.
- Adding a client (mobile, portal, agent) requires no API redesign.
