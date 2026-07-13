# ADR-0013: Angular Feature-Based Architecture

- **Status:** Proposed (decision approved by owner 2026-07-13, Topic 3
  review; flips to Accepted once the owner reviews this write-up)
- **Date:** 2026-07-13

## Context

Angular applications are commonly organized by technical type
(`components/`, `services/`, `models/`), which scatters each business
capability across the tree and erodes module boundaries as the app grows.
VetFlow's business documentation is already organized by module
(`docs/modules/<name>/`); the frontend structure should mirror that boundary.

## Decision

Adopt a **feature-based architecture**. The feature is the **primary
architectural boundary**. Preferred structure:

```
/core
/shared
/features/catalog
/features/purchasing
/features/inventory
/features/sales
...
```

The application must **not** be organized primarily by technical type
(`components/`, `services/`, `models/`).

## Alternatives Considered

- **Type-based structure** (`components/`, `services/`, `models/`): familiar
  from tutorials, but each business change touches every top-level folder
  and inter-feature coupling becomes invisible. Rejected by the owner.
- **Nx-style monorepo with enforced library boundaries:** the same idea with
  stronger tooling, but heavier scaffolding than a single-app project needs
  today; the folder convention keeps that door open.

## Consequences

- Feature folders map one-to-one to documented business modules
  (`docs/modules/_INDEX.md`), so documentation boundary = code boundary.
- `/shared` hosts the VetFlow UI Kit and cross-feature presentation code
  (ADR-0012); `/core` hosts app-wide singletons (auth session, API access,
  layout shell). Precise contents are defined during engineering
  documentation and the UI/UX Architecture discovery.
- Related mandatory standards recorded in
  `docs/architecture/principles.md`: strict TypeScript (no `any` outside
  documented exceptions) and the Smart/Presentation component split.
