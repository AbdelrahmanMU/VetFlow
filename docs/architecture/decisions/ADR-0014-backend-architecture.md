# ADR-0014: Backend Architecture

- **Status:** Proposed
- **Date:** 2026-07-13
- **Governs:** the shape of the backend. Detail lives in
  `standards/backend-standards.md`; this ADR holds the decisions.

## Context

VetFlow is a modular business system with a long expected life, built by the
owner with AI assistance. It needs boundaries strong enough to survive years of
incremental change, and simple enough that one person plus an AI can hold them
in their head. Clean Architecture gives the boundary; the risk is ceremony.
This ADR takes Clean Architecture and removes everything that does not pay for
itself.

## Decision

### 1. Four layers, one dependency rule

`Domain` · `Application` · `Infrastructure` · `Api`.

| Layer | Owns | May depend on |
|---|---|---|
| **Domain** | Entities, value objects, invariants, business rules, domain events | **Nothing** |
| **Application** | Use cases, command handlers, query *contracts*, ports (interfaces), input validation, pipeline | Domain |
| **Infrastructure** | EF Core + PostgreSQL, query *implementations*, identity, cache, files, time, external systems | Domain, Application (implements its ports) |
| **Api** | HTTP endpoints, error translation, composition root | All (wires them) |

**The dependency rule:** dependencies point inward. `Application` **never**
references `Infrastructure` — it declares ports; Infrastructure implements
them. Enforced by architecture tests (ADR-0016), not by convention.

### 2. Modular monolith — modules inside layers

Modules (`Catalog`, `Purchasing`, `Inventory`, `Sales`, …) mirror
`docs/modules/` and exist as **namespaces/folders inside each layer**, not as
separate projects. Project-per-module is rejected: assembly sprawl with no
payoff for a solo-owner codebase. Extraction to separate projects later
requires no namespace change.

**Cross-module rules:**

- A module's internals are private to it. Other modules see only its
  `Contracts` namespace (public DTOs, integration events, and the interfaces it
  offers). This namespace exists precisely so the boundary is machine-checkable.
- Only the owning module mutates its own aggregates. Cross-module *writes* go
  through domain events or the owning module's application services — never by
  touching another module's entities.
- Cross-module *reads* may project across tables inside query handlers. Forcing
  an internal API for a read inside a single process is ceremony, not
  architecture.

### 3. Rich domain model — pragmatic, not dogmatic

Entities enforce their own invariants: the documented `BR-*` rules live in the
domain, not in services. Aggregates and value objects are used **where
invariants demand them**, not everywhere — a lookup table does not need DDD
ceremony. Anemic entities with logic in services are a defect (principle 2).

### 4. Domain events — in-process notifications, never commands

Aggregates raise domain events; they are collected and dispatched inside the
same transaction at `SaveChanges`. **No transactional outbox at MVP** — it is
additive and unnecessary until an out-of-process consumer exists (principle 6).

**A domain event is a notification, never a command.**

- A domain event **must never directly modify another aggregate.**
- Cross-aggregate state changes belong to the **Application layer**: the handler
  observes the event and orchestrates the next command explicitly.
- This is what prevents event chains — the failure mode where a single save
  cascades through handlers nobody can trace, and hidden coupling accumulates
  behind an event bus.

Event names are past-tense facts (`ProductDeactivated`), never imperatives
(`DeactivateProduct`). An imperative name is the smell that a command is hiding
inside an event.

### 5. CQRS-lite — the boundary is strict

Commands and queries are separate handlers over **one database**. No separate
read store, no event sourcing.

**Commands:**

- modify state;
- return only `void`, an ID, or a lightweight command result;
- **never return read DTOs.** A caller that needs the updated view issues a
  query.

**Queries:**

- **never modify state;**
- **never trigger business side effects** (no domain events, no writes, no
  "while we're here" bookkeeping);
- may bypass the domain entirely (EF projections straight to response DTOs) —
  this is deliberate, not a leak.

The asymmetry is the point: a query is always safe to repeat, and a command is
always the only way state changes.

**Where a query lives.** `Application` owns the query *contract*: the query
type, its result DTO, and the `IQueryHandler<,>` interface. The **implementation
lives in Infrastructure**, because it executes SQL through EF Core — and SQL
belongs exclusively to Infrastructure (ADR-0019). Application therefore holds no
EF Core reference at all, and a provider change touches only Infrastructure. The
projection still goes straight to the response DTO: no repository, no domain
round-trip, no ceremony — only the file it lives in changed.

### 6. Application pipeline — no MediatR, no AutoMapper

Two interfaces the project owns: `ICommandHandler<TCommand, TResult>` and
`IQueryHandler<TQuery, TResult>`, resolved by DI. Cross-cutting behavior
(validation → transaction → logging) is applied as **decorators** registered in
the composition root.

- **MediatR rejected:** runtime-resolved dispatch hides the call target
  (principle 4), and the library moved to commercial licensing in 2025. A
  handler you can Ctrl-click to is a handler an AI edits correctly.
- **AutoMapper rejected:** mapping is written by hand in explicit extension
  methods — boring, greppable, and correct every time.

Both replacements are code the project owns outright, roughly fifty lines,
fully debuggable.

### 7. Transactions

**One command = one unit of work = one `SaveChanges`.** The transaction
decorator owns the boundary. No nested transactions. Queries never open one.

### 8. Repositories represent aggregates, never tables

**Generic repositories are prohibited.** The architecture must never introduce
`IRepository<T>` or `GenericRepository<T>`. `DbContext` is already a Unit of
Work and `DbSet<T>` already a repository; wrapping them re-abstracts an
abstraction and degenerates into `IQueryable` leakage.

- **Every aggregate owns its own repository contract:** `IProductRepository`,
  `IBatchRepository`, `ISaleRepository`.
- A repository contract belongs to **the module that owns the aggregate**, and
  lives with that module in the Application layer; the implementation lives in
  Infrastructure.
- A repository serves an **aggregate**, not a table. It loads and persists whole
  aggregates; it does not expose row-level or cross-aggregate access.
- Repositories exist where an aggregate has invariants to protect, or where a
  caching decorator (ADR-0011) needs a seam. **Query handler implementations use
  EF Core directly** (in Infrastructure, §5) and need no repository.

Enforced by architecture test: no type named `*Repository<*>`; no repository
interface outside its owning module.

### 9. Composition root

The `Api` project is the **only** place anything is wired. Each layer exposes
one registration extension (`AddDomain()`, `AddApplication()`,
`AddInfrastructure()`, …). No service location anywhere.

### 10. Configuration

Strongly-typed options, bound and **validated at startup**. Invalid
configuration **refuses to boot** (principle 8) — a clinic system must not limp.
Secrets never live in the repository.

### 11. Performance budget

Engineering targets, not an optimization licence. Performance work remains
evidence-driven (ADR-0004, principle 6): **measure first, then optimize the
thing the measurement named.**

| Target | Budget |
|---|---|
| API p95 latency (typical CRUD endpoint) | < 300 ms |
| API p95 latency (worst case, any endpoint) | < 500 ms |
| Product search / autocomplete | < 200 ms |
| Checkout (sale completion, end to end) | < 1 s |
| Page load — first meaningful paint on desktop | < 2 s |
| Application startup (container ready) | < 10 s |
| Background jobs | never block a request; failures retried and logged, never silent |

Budgets are **tripwires**: a breach opens an investigation, it does not
authorize speculative optimization elsewhere. Budgets are revisited with real
production data, and any change is recorded here.

### 12. The Simplicity Budget

Before introducing **any** new technology, framework, infrastructure component,
or architectural pattern, both questions must be answered:

1. **Does it solve a current, verified problem?**
2. **Does it reduce the overall complexity of the system?**

**If either answer is No, it is not introduced.** No exceptions, no "we'll need
it eventually."

This budget exists to prevent the premature adoption of exactly these:
Kafka · MassTransit · Redis · Elasticsearch · microservices · an event bus ·
distributed messaging. Each is a good technology and each is a permanent tax;
none is justified by a measured VetFlow need today.

The Simplicity Budget is the operational form of principle 6 (*no speculation*)
and principle 5 (*simplicity over cleverness*): complexity must be **bought with
evidence**, never with anticipation. Passing the budget is what an ADR proposing
a new component must demonstrate.

## Alternatives Considered

- **Layered/N-tier with a data layer at the bottom:** familiar, but inverts the
  dependency rule and lets persistence dictate the domain.
- **Project-per-module modular monolith:** stronger compile-time boundaries;
  rejected as assembly sprawl for one maintainer — architecture tests give the
  same guarantee at zero build cost.
- **Vertical slice architecture (no layers):** excellent for small services,
  but weakens the domain-owns-rules principle as modules grow.
- **Full CQRS with a separate read model / event sourcing:** solves problems
  VetFlow does not have. Rejected (principle 6).

## Consequences

- Business rules have exactly one home; the domain is testable without a
  database.
- The database, cache, identity provider, and file store are all replaceable
  (principle 7).
- The pipeline is explicit and greppable — the single biggest lever on AI
  implementation accuracy in this codebase.
- Every rule in this ADR that can be executed **is** executed as an architecture
  test (ADR-0016). A rule without a test is a wish.
