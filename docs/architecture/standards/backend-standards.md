# Backend Standards

> **Status: Draft.** Executable engineering contract — not documentation.
> Architecture and rationale live in ADR-0014, ADR-0016, ADR-0018, ADR-0019.
> This document contains only enforceable rules.

**Defaults:** Scope = `Backend` · Stability = `Stable` · Depends On = none ·
Class = `Mandatory` · Severity = `Error`.
**Language rules** are in [`csharp-coding-standards.md`](csharp-coding-standards.md).
**Severity policy** and **exception process**: ADR-0017 §7; exceptions only via
the register below.

## Layering and dependencies

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-BE-001 | `Domain` references no other project and no external framework (no EF Core, no ASP.NET) | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-002 | `Application` never references `Infrastructure`; it declares ports | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-003 | EF Core, Npgsql, and SQL execution appear **only** in `Infrastructure` | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0019](../decisions/ADR-0019-database-platform.md) |
| STD-BE-004 | `Api` never references domain entities directly; it speaks in DTOs | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-005 | Cross-module access goes only through the owning module's `Contracts` namespace | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-006 | No circular dependency between modules | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-007 | Service registration happens only in the composition root (`Api`); no service location | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |

## Domain

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Depends On | Source |
|---|---|---|---|---|---|---|---|---|
| STD-BE-010 | Business rules (`BR-*`) are enforced in the domain, never in handlers, controllers, queries, or the UI | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | — | [P2](../principles.md) |
| STD-BE-011 | Entities have no public setters; state changes go through intention-revealing methods | Mandatory | Error | Automatic | Architecture test | Architecture test | STD-CS-011 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-012 | Invariants are enforced in the constructor and in every mutating method — an entity is never constructible in an invalid state | Mandatory | Error | Semi-Automatic | Unit test + review | Review | — | [P8](../principles.md) |
| STD-BE-013 | A domain method that violates a `BR-*` throws the typed `BusinessRuleException` carrying that rule's error code | Mandatory | Error | Semi-Automatic | Unit test + review | Review | — | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-BE-014 | Domain events are past-tense facts (`ProductDeactivated`), never imperatives | Mandatory | Error | Automatic | Architecture test (naming) | Architecture test | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-015 | A domain event handler never modifies another aggregate; cross-aggregate changes are orchestrated in `Application` | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | STD-BE-014 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-016 | The domain contains no HTTP, no localization, no logging, no UI concept | Mandatory | Error | Automatic | Architecture test | Architecture test | STD-BE-001 | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |

## Application — handlers, pipeline, repositories

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Depends On | Source |
|---|---|---|---|---|---|---|---|---|
| STD-BE-020 | Every use case is an `ICommandHandler<,>` or `IQueryHandler<,>`; no mediator library | Mandatory | Error | Automatic | Architecture test | Architecture test | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-021 | Commands return `void`, an ID, or a lightweight command result — **never a read DTO** | Mandatory | Error | Automatic | Architecture test | Architecture test | STD-BE-020 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-022 | Queries never mutate state and never raise domain events | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | STD-BE-020 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-023 | Query *contracts* live in `Application`; query *implementations* live in `Infrastructure` | Mandatory | Error | Automatic | Architecture test | Architecture test | STD-BE-003 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-024 | One command = one transaction = one `SaveChanges`; the transaction decorator owns the boundary | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-025 | Generic repositories are prohibited — no `IRepository<T>`, no `GenericRepository<T>` | Mandatory | Error | Automatic | Architecture test | Architecture test | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-026 | A repository serves one aggregate (`IProductRepository`), is named for it, and lives in the module that owns it | Mandatory | Error | Automatic | Architecture test | Architecture test | STD-BE-025 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-027 | Input validation uses FluentValidation in the pipeline; domain invariants are enforced regardless | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-028 | Mapping is hand-written in explicit extension methods; no mapping library | Mandatory | Error | Automatic | Architecture test (no AutoMapper reference) | Architecture test | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-029 | Cross-cutting behavior is added as a pipeline decorator, never inside a handler | Mandatory | Warning | Manual | Engineering review | Review | STD-BE-020 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |

## Errors and the Error Catalog

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-BE-030 | Every business exception carries a stable error code and optional metadata — **never text, HTTP status, or UI copy** | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-BE-031 | `InfrastructureException` is a separate root; it never inherits from `DomainException` | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-BE-032 | Error codes are `VTF-<MODULE>-NNN`; each is unique repository-wide | Mandatory | Error | Automatic | CI script (uniqueness check) | CI | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-BE-033 | Every `BR-*` maps to exactly one error code; every error code exists in exactly one place | Mandatory | Error | Automatic | CI script | CI | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-BE-034 | Every error code has a localized message resource for every supported language | Mandatory | Error | Automatic | CI script | CI | [ADR-0007](../decisions/ADR-0007-localization-architecture.md) |
| STD-BE-035 | Infrastructure exceptions and stack traces are never exposed to clients | Mandatory | Error | Semi-Automatic | Integration test + review | CI | [ADR-0015](../decisions/ADR-0015-api-contract.md) |

## Infrastructure — EF Core, persistence, ports

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-BE-040 | Entity mapping uses `IEntityTypeConfiguration<T>` classes; no persistence attributes on domain entities | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-041 | Schema changes ship as reviewed EF Core migrations named for intent; `EnsureCreated` is prohibited | Mandatory | Error | Automatic | Architecture test + CI script | CI | [ADR-0019](../decisions/ADR-0019-database-platform.md) |
| STD-BE-042 | Migrations live only in `Infrastructure` | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0019](../decisions/ADR-0019-database-platform.md) |
| STD-BE-043 | Database identifiers are `snake_case` (PostgreSQL convention), applied by one global naming convention — never per-entity | Mandatory | Error | Automatic | Integration test | CI | [ADR-0019](../decisions/ADR-0019-database-platform.md) |
| STD-BE-044 | Arabic searchable text has a normalized search column (diacritics stripped, alef/teh-marbuta unified) with a trigram index | Mandatory | Error | Semi-Automatic | Integration test + review | CI | [ADR-0019](../decisions/ADR-0019-database-platform.md) |
| STD-BE-045 | Time comes from `TimeProvider`; `DateTime.Now`/`UtcNow` are prohibited in product code | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-046 | Every external system is reached through a port declared in `Application` and an adapter in `Infrastructure` — including a single HTTP call | Mandatory | Error | Automatic | Architecture test | Architecture test | [P7](../principles.md) |
| STD-BE-047 | Caching lives only in Infrastructure decorators; Domain and Application contain no cache concept | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0011](../decisions/ADR-0011-caching-architecture.md) |
| STD-BE-048 | Configuration uses validated typed options; invalid configuration fails startup | Mandatory | Error | Automatic | Integration test | CI | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-BE-049 | Logging goes through `Microsoft.Extensions.Logging` abstractions; the sink (Serilog) is wired only in Infrastructure | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0011](../decisions/ADR-0011-caching-architecture.md) |
| STD-BE-050 | No personal data (client names, phone numbers, addresses) in logs — log identifiers | Mandatory | Error | Semi-Automatic | Analyzer + review | Review | [P9](../principles.md) |

## Testing

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-BE-060 | Every implemented `BR-*` has at least one test whose name ends in its ID | Mandatory | Error | Automatic | CI script (traceability check) | CI | [ADR-0016](../decisions/ADR-0016-testing-and-architecture-enforcement.md) |
| STD-BE-061 | Application and API are tested by integration tests against a real PostgreSQL container | Mandatory | Error | Semi-Automatic | CI + review | CI | [ADR-0016](../decisions/ADR-0016-testing-and-architecture-enforcement.md) |
| STD-BE-062 | Domain is tested by pure unit tests — no database, no mocks of infrastructure | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0016](../decisions/ADR-0016-testing-and-architecture-enforcement.md) |
| STD-BE-063 | Every error code has an automated test proving its mapping to the right HTTP status | Mandatory | Error | Automatic | Integration test | CI | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-BE-064 | Test names are behavioral sentences; `Test1`, `Should_Work` and equivalents are prohibited | Mandatory | Warning | Manual | Engineering review | Review | [ADR-0016](../decisions/ADR-0016-testing-and-architecture-enforcement.md) |
| STD-BE-065 | Architecture tests run in under 30 s; a rule that significantly slows CI needs owner approval | Mandatory | Error | Automatic | CI (timing) | CI | [ADR-0016](../decisions/ADR-0016-testing-and-architecture-enforcement.md) |

## Approved backend libraries

Adding a foundational library requires an **ADR or explicit owner approval**,
and must pass the **Simplicity Budget** (ADR-0014 §12), **principle 13**
(stability over novelty) and **principle 14** (measurable engineering).

| Library | Lifecycle | Purpose | Allowed scope | Forbidden usage | Rejected alternatives |
|---|---|---|---|---|---|
| ASP.NET Core | Approved | HTTP host, DI, middleware | `Api`, composition root | Business logic in controllers | Node.js, FastAPI, Spring |
| Entity Framework Core | Approved | ORM, migrations | `Infrastructure` only | Any reference from `Domain`/`Application` | Dapper-only, hybrid ORM |
| Npgsql | Approved | PostgreSQL provider | `Infrastructure` only | Any reference outside Infrastructure | — |
| FluentValidation | Approved | Input validation in the pipeline | `Application` | Enforcing business rules (those belong to the domain) | DataAnnotations |
| Serilog | Approved | Log sink | `Infrastructure` wiring only | Being referenced as a logging API by product code (use `ILogger`) | NLog, log4net |
| OpenTelemetry | Approved | Tracing, TraceId/CorrelationId flow | `Infrastructure`, `Api` | Ad-hoc correlation schemes | Vendor-specific APM SDKs |
| xUnit | Approved | Test framework | Test projects | — | NUnit, MSTest |
| Testcontainers | Approved | Real PostgreSQL in tests | Integration test projects | Replacing integration tests with mocks | In-memory provider, SQLite substitute |
| NetArchTest.Rules | Approved | Architecture tests | Architecture test project | Being weakened to make a build pass | ArchUnitNET |
| Shouldly | Approved | Assertions | Test projects | — | FluentAssertions |
| **MediatR** | **Forbidden** | — | — | **All use** | Own `ICommandHandler`/`IQueryHandler` + decorators |
| **AutoMapper** | **Forbidden** | — | — | **All use** | Hand-written mapping extension methods |
| **FluentAssertions** | **Forbidden** | — | — | **All use** | Shouldly / built-in assertions |

*Forbidden entries are recorded, not deleted: MediatR and AutoMapper for
implicitness and 2025 commercial licensing; FluentAssertions for 2025 commercial
licensing (ADR-0014, ADR-0016).*

## Exception Register

| STD | Scope of exception | Reason | Approved by | Date |
|---|---|---|---|---|
| — | *(none)* | | | |

## Tombstones

| STD | Removed | Reason |
|---|---|---|
| — | *(none)* | |
