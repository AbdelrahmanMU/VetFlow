# ADR-0004: ORM Strategy — Entity Framework Core Only

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

With ASP.NET Core approved (ADR-0003), the data-access approach must be
fixed before engineering documentation begins. A common pattern is a hybrid
stack (EF Core for writes, Dapper/raw SQL for reads), which buys performance
at the cost of two query dialects, two sets of conventions, and higher
cognitive load — directly against the project's maintainability-first
principles.

## Decision

**Entity Framework Core is the primary and, initially, the only ORM.**

- No hybrid ORM complexity from the beginning (no parallel Dapper/raw-SQL
  read stack).
- Raw SQL or specialized optimizations may be introduced later **only when
  supported by real measurements** on real data.
- Premature optimization is explicitly discouraged.

## Alternatives Considered

- **Hybrid EF Core + Dapper from day one:** faster reads in theory, but
  doubles the data-access surface before any measured need exists.
- **Dapper/raw SQL only:** maximum control, but loses migrations,
  change-tracking, and LINQ productivity that a solo-maintained codebase
  benefits from.

## Consequences

- One data-access idiom across the codebase; EF Core migrations are the
  single schema-evolution mechanism.
- Performance work requires a measurement first (profile/trace), then a
  targeted, documented exception.
- The database platform decision (open) must have a first-class EF Core
  provider.
