# ADR-0011: Caching Is an Infrastructure-Only Concern

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3
  review; flips to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

The caching recommendation — no distributed cache at MVP; in-memory caching
of slow-changing lookup data behind the built-in cache abstraction; Redis
only when real measurements or multi-instance deployment demand it — was
**approved by the owner with one additional rule**, recorded here.

## Decision

1. **Caching belongs ONLY to the Infrastructure layer.**
2. The **Domain and Application layers must never know whether data is
   cached.** No business rule may depend on the existence of a cache.
3. Cache is purely an implementation optimization. Infrastructure decides —
   and may change at any time — between:
   - memory cache
   - hybrid cache
   - Redis
   - or no cache at all

   **without affecting business logic.**
4. MVP posture (approved recommendation): no distributed cache; in-memory
   caching (via the platform cache abstraction) for a small set of
   slow-changing lookup data, with explicit invalidation on write. Redis is
   introduced only when justified by real measurements or by multi-instance
   deployment — consistent with the no-premature-optimization rule of
   ADR-0004.

## Alternatives Considered

- **Cache-aware application code** (application decides what/when to cache):
  common, but leaks an optimization concern into business logic and violates
  the layer boundary the owner mandates. Rejected.
- **Redis from day one:** infrastructure readiness with zero measured need;
  an extra service to run and back up for a two-user clinic. Rejected as
  premature.

## Consequences

- Caching decisions (including removing a cache) are invisible to Domain and
  Application code; correctness never depends on a cache hit or miss.
- Cache placement (e.g., decorating repository/query implementations in
  Infrastructure) is settled during engineering documentation.
- Presupposes a layered backend (Domain/Application/Infrastructure); the
  backend layering itself is not yet recorded in any ADR — flagged as a
  pending architecture decision.
