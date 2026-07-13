# ADR-0003: Backend Platform — ASP.NET Core

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

VetFlow is entering its engineering-foundation phase. The system is a
veterinary clinic management platform: a web API consumed initially by a
single Angular client, with future clients planned (mobile, customer portal,
AI agents, external integrations — see ADR-0006). Development is done
primarily by the owner with AI assistance; maintainability, readability,
consistency, low cognitive complexity, and clear module boundaries outrank
development speed (see `docs/architecture/principles.md`).

## Decision

The backend is built on **ASP.NET Core**.

Version policy:

- Always target the **latest stable LTS release** of .NET (at the time of
  writing: .NET 10 LTS).
- Upgrade LTS → LTS; **no preview or STS versions** in the product.

## Alternatives Considered

- **Node.js (NestJS/Express):** same language as the frontend ecosystem, but
  weaker static typing guarantees at runtime, and long-term maintainability of
  large business domains favors C#'s type system and tooling.
- **Python (Django/FastAPI):** fast to start, but less suited to the strongly
  typed, modular monolith the project's principles call for.
- **Java (Spring Boot):** comparable capability, heavier ceremony and slower
  iteration for a solo-owner, AI-assisted workflow.

## Consequences

- C# becomes the backend language; English identifiers per ADR-0002, mapped
  to Arabic domain terms through `docs/shared/GLOSSARY.md`.
- Predictable ~2-year LTS upgrade cadence; no chasing previews.
- `.claude/rules/coding.md` can be populated once the remaining stack
  decisions (database, auth, caching, component library) are accepted.
