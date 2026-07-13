# ADR-0005: Frontend Platform — Angular

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3; flips
  to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

VetFlow's UI is an Arabic-first, RTL, data-entry-heavy application used for
long working sessions by two on-site users (doctor/owner and cashier). The
frontend must support a dedicated design system, adaptive per-device layouts
(ADR-0009), and keyboard-first desktop workflows. It is the first — but not
the only planned — client of the product API (ADR-0006).

## Decision

The frontend is built with **Angular**.

Version policy:

- Always use the **latest stable release** of Angular.
- **No preview/next versions** in the product.

## Alternatives Considered

- **React:** larger ecosystem, but framework-level conventions (DI, forms,
  router, RTL/i18n discipline) must be assembled from libraries; Angular's
  batteries-included consistency suits a solo-owner, maintainability-first
  codebase.
- **Vue:** similar trade-off to React with a smaller enterprise-component
  ecosystem.
- **Blazor:** single-language stack with the backend, but weaker RTL/component
  ecosystem for premium data-dense UIs and a smaller hiring pool for future
  developers.

## Consequences

- TypeScript on the frontend; Angular's opinionated structure becomes the
  frontend consistency baseline.
- The UI component library decision (open) is scoped to the Angular
  ecosystem.
- Angular's stable-major cadence sets the frontend upgrade rhythm.
