# Frontend Standards

> **Status: Draft.** Executable engineering contract — not documentation.
> Rationale lives in ADR-0005, ADR-0009, ADR-0012, ADR-0013. This document
> contains only enforceable rules.

**Defaults:** Scope = `Frontend` · Stability = `Stable` · Depends On = none ·
Class = `Mandatory` · Severity = `Error`.
**Severity policy** and **exception process**: ADR-0017 §7; exceptions only via
the register below.

**Boundary:** this document governs *engineering* — the presence, wiring, and
structure of things. Their *appearance* (tokens, colors, motion, copy) is the
design system's, and lands in `docs/ui/` after the UI/UX discovery.

## Architecture and boundaries

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-FE-001 | The app is organized by feature: `/core`, `/shared`, `/features/<module>` — never by technical type (`components/`, `services/`, `models/`) | Mandatory | Error | Automatic | ESLint (boundaries) | Compilation | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |
| STD-FE-002 | Feature folders map one-to-one to documented business modules | Mandatory | Error | Semi-Automatic | CI script + review | CI | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |
| STD-FE-003 | **`primeng/*` is not importable outside the UI Kit** | Mandatory | Error | Automatic | ESLint `no-restricted-imports` | Compilation | [ADR-0012](../decisions/ADR-0012-ui-kit-library-independence.md) |
| STD-FE-004 | A feature never imports from another feature; shared code moves to `/shared` | Mandatory | Error | Automatic | ESLint (boundaries) | Compilation | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |
| STD-FE-005 | Every reusable component is consumed as a `Vf*` UI Kit wrapper (`VfButton`, `VfInput`, `VfTable`, `VfDialog`, `VfSelect`, …) | Mandatory | Error | Automatic | ESLint | Compilation | [ADR-0012](../decisions/ADR-0012-ui-kit-library-independence.md) |
| STD-FE-006 | Standalone components only; no `NgModule` | Mandatory | Error | Automatic | ESLint | Compilation | [ADR-0005](../decisions/ADR-0005-frontend-platform.md) |
| STD-FE-007 | Feature routes are lazy-loaded | Mandatory | Error | Automatic | ESLint + bundle check | CI | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |

## Components and state

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Depends On | Source |
|---|---|---|---|---|---|---|---|---|
| STD-FE-010 | Smart components hold orchestration and data access; presentation components take inputs and emit outputs and inject no services | Mandatory | Error | Semi-Automatic | ESLint + review | Compilation | — | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |
| STD-FE-011 | The UI Kit contains presentation components only | Mandatory | Error | Automatic | ESLint | Compilation | STD-FE-005 | [ADR-0012](../decisions/ADR-0012-ui-kit-library-independence.md) |
| STD-FE-012 | Signals are the default for component and feature state; no global state library | Mandatory | Error | Semi-Automatic | ESLint + review | Compilation | — | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |
| STD-FE-013 | RxJS is used only at stream-shaped boundaries (HTTP, debounced input, route params) and converted to signals at the edge | Mandatory | Warning | Manual | Engineering review | Review | STD-FE-012 | [ADR-0013](../decisions/ADR-0013-angular-feature-architecture.md) |
| STD-FE-014 | Every subscription is cleaned up (`takeUntilDestroyed` or the async pipe); no manual unmanaged `subscribe` | Mandatory | Error | Automatic | ESLint | Compilation | STD-FE-013 | — |
| STD-FE-015 | Change detection is OnPush (or zoneless) everywhere | Mandatory | Error | Automatic | ESLint | Compilation | — | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-016 | Typed reactive forms only; template-driven forms are prohibited | Mandatory | Error | Automatic | ESLint | Compilation | — | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-017 | Validation display and error copy come from the UI Kit form field; features never hand-roll validation UI | Mandatory | Error | Semi-Automatic | ESLint + review | Compilation | STD-FE-005 | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |

## TypeScript

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-FE-020 | TypeScript `strict` mode is enabled | Mandatory | Error | Automatic | Compiler | Compilation | [P](../principles.md) |
| STD-FE-021 | `any` is prohibited; an exception requires a register entry | Mandatory | Error | Automatic | ESLint | Compilation | [P](../principles.md) |
| STD-FE-022 | Non-null assertion `!` and unchecked casts are prohibited | Mandatory | Error | Automatic | ESLint | Compilation | [P](../principles.md) |
| STD-FE-023 | API response types are explicit; no implicit `any` from HTTP calls | Mandatory | Error | Automatic | ESLint | Compilation | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-FE-024 | No `console.*` in product code | Mandatory | Error | Automatic | ESLint | Compilation | — |
| STD-FE-025 | No `TODO`, `FIXME`, `HACK`, commented-out code, or dead code | Mandatory | Error | Automatic | CI script | CI | [ADR-0017](../decisions/ADR-0017-ai-execution-model.md) |
| STD-FE-026 | Formatting is enforced by the repository config; CI verifies it | Mandatory | Error | Automatic | CI script | CI | — |

## Rendering and data-view engineering

Every data view has all four states. Their **presence** is engineering (here);
their **look** is the design system.

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Depends On | Source |
|---|---|---|---|---|---|---|---|---|
| STD-FE-030 | Every data view implements loading, empty, error, and success states — none may be omitted | Mandatory | Error | Semi-Automatic | ESLint + review | Review | STD-FE-005 | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-031 | Initial data loads render the UI Kit skeleton (`VfSkeleton`); ad-hoc spinners are prohibited | Mandatory | Error | Semi-Automatic | ESLint + review | Review | STD-FE-030 | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-032 | List rendering uses `@for` with `track`; legacy `*ngFor` is prohibited | Mandatory | Error | Automatic | ESLint | Compilation | — | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-033 | Unbounded lists are virtualized (CDK virtual scroll or the UI Kit table's virtual mode) | Mandatory | Error | Semi-Automatic | ESLint + review | Review | STD-FE-005 | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-FE-034 | Below-the-fold and heavy widgets are deferred with `@defer` | Recommended | Warning | Manual | Engineering review | Review | — | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-035 | Images use `NgOptimizedImage` and lazy loading | Mandatory | Error | Automatic | ESLint | Compilation | — | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |
| STD-FE-036 | **Optimistic UI is prohibited for business mutations**; it is permitted only for trivially reversible local UI state | Mandatory | Error | Semi-Automatic | Engineering review + integration test | Review | — | [P9](../principles.md) |
| STD-FE-037 | Server errors are surfaced from the API `errorCode`, never by parsing message text | Mandatory | Error | Semi-Automatic | ESLint + review | Review | — | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-FE-038 | Bundle budgets are configured in `angular.json` and enforced in CI | Mandatory | Error | Automatic | CI (bundle budget) | CI | — | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |

## Localization, RTL, accessibility

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-FE-040 | No hardcoded user-facing strings; all copy comes from localization resources | Mandatory | Error | Automatic | ESLint | Compilation | [ADR-0007](../decisions/ADR-0007-localization-architecture.md) |
| STD-FE-041 | Layout uses logical CSS properties (`inline-start`/`inline-end`), never physical `left`/`right` | Mandatory | Error | Automatic | Stylelint | Compilation | [ADR-0007](../decisions/ADR-0007-localization-architecture.md) |
| STD-FE-042 | Dates, numbers, and money are formatted through the localization service; no manual formatting | Mandatory | Error | Semi-Automatic | ESLint + review | Review | [ADR-0007](../decisions/ADR-0007-localization-architecture.md) |
| STD-FE-043 | Interactive elements are keyboard-reachable and labelled; automated a11y checks pass | Mandatory | Error | Automatic | ESLint a11y + component test | CI | [ADR-0009](../decisions/ADR-0009-design-system-adaptive-ui.md) |

## Approved frontend libraries

Adding a foundational library requires an **ADR or explicit owner approval**,
and must pass the **Simplicity Budget** (ADR-0014 §12), **principle 13** and
**principle 14**.

| Library | Lifecycle | Purpose | Allowed scope | Forbidden usage | Rejected alternatives |
|---|---|---|---|---|---|
| Angular | Approved | Application framework | Everywhere | Preview/`next` releases | React, Vue, Blazor |
| PrimeNG | Approved | Component foundation | **Inside the VetFlow UI Kit only** | **Any import from a feature module** | Angular Material, Kendo UI |
| Angular CDK | Approved | Low-level primitives (overlay, a11y, virtual scroll, bidi) | UI Kit; features via UI Kit wrappers | Bypassing the UI Kit for styled components | Hand-rolled primitives |
| RxJS | Approved | Stream-shaped boundaries | HTTP, debounced input, route params | State management (signals are the default) | NgRx, Akita, MobX |

*Rejected and recorded: NgRx and other global state libraries — a two-user,
server-truth CRUD app does not need one (Simplicity Budget).*

## Exception Register

| STD | Scope of exception | Reason | Approved by | Date |
|---|---|---|---|---|
| — | *(none)* | | | |

## Tombstones

| STD | Removed | Reason |
|---|---|---|
| — | *(none)* | |
