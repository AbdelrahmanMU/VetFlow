# ADR-0008: Docker From the Beginning

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

Development happens on Windows; testing and deployment targets may differ.
A solo-owner project cannot afford "works on my machine" divergence between
environments, and future developers must be able to reproduce the full stack
quickly.

## Decision

**Docker is used from the beginning.** Development, testing, and deployment
remain consistent across environments: the application and its backing
services (database, and later cache, etc.) are defined as containers from the
first runnable milestone.

## Alternatives Considered

- **Native local installs, containerize at deployment time:** lighter day-one
  setup, but environment drift accumulates exactly during the period with the
  least testing coverage.
- **Full orchestration (Kubernetes) from day one:** operational overkill for
  a single-clinic deployment; Compose-level tooling is sufficient until
  measured needs say otherwise.

## Consequences

- The chosen database and any future cache must run well in containers
  (a criterion for the open database/caching decisions).
- CI and deployment build on the same images developers run locally.
- Onboarding a future developer is "clone + compose up", not a setup guide.
