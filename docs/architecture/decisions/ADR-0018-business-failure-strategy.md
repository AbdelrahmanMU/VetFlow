# ADR-0018: Business Failure Strategy — Exceptions and the Error Catalog

- **Status:** Proposed
- **Date:** 2026-07-13
- **Owner ruling:** business exceptions adopted; `Result<T>` rejected.

## Context

A business rule violation — a cashier deactivating a product that still has an
open package (`BR-CAT-031`) — must travel from the domain to the client
predictably, in Arabic, with the right HTTP status, every time. Two candidate
strategies were evaluated in full: business exceptions, and `Result<T>` for
business outcomes with exceptions reserved for unexpected failures.

## Decision

### 1. Business exceptions, with a structured Error Catalog

Violations raise typed exceptions. A single middleware translates them. The
chain is fixed:

```
Business rule  (BR-CAT-031)
      ↓
BusinessRuleException
      ↓
Stable error code  (VTF-CAT-031)
      ↓
Middleware  — the single translation point
      ↓
RFC 9457 ProblemDetails  (ADR-0015)
      ↓
Localized message (Arabic / English)  +  HTTP status (409)
      ↓
Client
```

### 2. The exception hierarchy

```
DomainException                 (abstract root — Domain layer)
    BusinessRuleException       a documented BR-* was violated
    ValidationException         input failed validation
    EntityNotFoundException     the referenced entity does not exist
    AuthorizationException      the caller may not do this
    ConcurrencyException        a concurrent modification was detected

InfrastructureException         (separate root — Infrastructure layer ONLY)
```

**`InfrastructureException` does not inherit from `DomainException`** (owner
ruling). It belongs exclusively to Infrastructure: an infrastructure failure is
not a business failure, the Domain layer must not own an infrastructure concept
(the dependency rule, ADR-0014), and infrastructure failures are never exposed
to clients — they become an opaque 500 with a `traceId`.

### 3. Exceptions carry codes, never text

A business exception carries **only**:

- a **stable error code**
- optional **metadata** (structured values the message may interpolate)

**What the Domain knows:** the error code, and optional metadata. That is all.

**What the Domain never knows:** HTTP · RFC 9457 · Problem Details ·
localization · logging · the UI.

The domain does not know that HTTP exists, does not know what language the user
speaks, and does not know whether anyone is watching. Every one of those is a
concern of an outer layer, and each is added by the layer that owns it —
translation and status by the middleware, correlation by the API, presentation
by the client.

**The direction of the mapping is fixed:**

```
One Business Rule  →  One Error Code  →  Many Localized Messages
```

**Never the opposite.** One error code never serves several business rules
(then a client cannot tell them apart), and one business rule never scatters
across several codes (then the contract is ambiguous and the catalog rots).

### 4. The Error Catalog

The catalog maps, in exactly one place:

| Business rule | Error code | HTTP | Arabic message | English message |
|---|---|---|---|---|
| `BR-CAT-031` | `VTF-CAT-031` | 409 | … | … |

- Code format: `VTF-<MODULE>-<NNN>`, aligned with the module's `BR-*` numbering.
- **Every business rule maps to exactly one error code.**
- **Every error code exists in exactly one place.** Duplicate codes are a
  build failure.
- Error codes are part of the **published engineering contract** (ADR-0015):
  clients branch on the code, never on the message text.
- Messages are localization resources (ADR-0007). Adding a language adds a
  resource — it touches no rule, no exception, no handler.

### 5. Mandatory exception constraints

The system **must not**:

- throw `System.Exception` (or any non-typed base exception);
- catch and ignore, or swallow, an exception;
- use exceptions for normal control flow;
- expose infrastructure exceptions to clients;
- expose stack traces;
- duplicate an error code.

Every `BusinessRuleException` **must**: have a stable error code · be documented
in the catalog · have an automated test (ADR-0016) · map to an RFC 9457
response.

These constraints are enforced by analyzers, architecture tests, and a catalog
uniqueness check in CI (ADR-0016) — not by good intentions.

## Alternatives Considered — `Result<T>`, and why it was rejected

`Result<T>` for business outcomes (exceptions only for unexpected failures) was
seriously evaluated. It wins on one axis: failure paths are visible in method
signatures. It was rejected for four reasons:

1. **Unobservable results fail silently.** C# cannot force a `Result` to be
   inspected. An ignored exception aborts the request; an ignored `Result` lets
   a half-applied operation continue. In a medical and financial records system
   those failure modes are not symmetric — and the silent one survives code
   review, because there is nothing on the page to see (principle 9).
2. **AI implementation accuracy.** The natural C# a model writes is the correct
   code under exceptions; under `Result<T>` the correct code requires
   disciplined threading at every call site, and the mistake is invisible.
3. **Ceremony without a payoff.** C# still lacks discriminated unions and
   exhaustive matching, so `Result` means railway plumbing through every async
   chain — cost paid on every line, against principle 5.
4. **It does not deliver one paradigm.** Infrastructure failures remain
   exceptions regardless, so `Result<T>` buys two error models, not one.

The explicitness objection is answered where it matters: the **Error Catalog**
makes every failure explicit **in the contract** — the layer clients, testers,
and future AI sessions actually consume.

**Revisit triggers** (reopen this ADR only on evidence): C# gains discriminated
unions with exhaustive matching; or a measured hot path where exception cost is
real.

## Consequences

- One failure model, one translation point, one catalog.
- The chain `REQ → BR → code → test → error code → localized message` is
  auditable end to end.
- Localization of errors is free; a new language is a resource file.
- Domain and Application stay free of HTTP, of text, and of presentation.
