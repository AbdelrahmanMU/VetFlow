# ADR-0001: Repository Structure

- **Status:** Accepted
- **Date:** 2026-07-12 (proposed and accepted, with owner revisions)

## Context

VetFlow is Documentation-First, developed almost entirely AI-assisted with
Claude Code, and will be maintained for years. Future modules (medical
services, appointments, multi-branch, cloud sync, …) must be additive — they
must not force restructuring.

## Decision

- Single repository holding documentation and (later) code.
- Module-folder documentation: `docs/modules/<name>/` with a fixed standard
  file set defined by `docs/modules/_TEMPLATE/` (`overview`, `requirements`,
  `business-rules`, `workflow`, `ui`, `acceptance`, `decisions`,
  `test-scenarios`).
- Cross-module business documentation lives in `docs/shared/` (vision,
  glossary, personas, business events, roadmap, reference). Reference
  material is split into `external/`, `regulations/`, `competitors/`,
  `assets/`. A shared folder holds only what genuinely spans modules.
- Three-tier context loading: `CLAUDE.md` router (always loaded, ≤ 80 lines) →
  `STATUS.md` / `PROJECT_CONTEXT.md` / `_INDEX.md` files → deep docs on demand.
- Native `.claude/` for AI machinery: rules, slash commands, and
  **playbooks** (`.claude/playbooks/` — repeatable procedures for a kind of
  work: discovery, new-feature, review, bug-fix, refactor, release);
  `docs/` for knowledge.
- Two-tier decision records: ADRs for expensive-to-reverse decisions; decision
  logs for everything else. Decision logs are split by scope:
  - **Global** — spans modules → `docs/business/DECISION_LOG.md`.
  - **Module** — confined to one module → that module's `decisions.md`.
  A decision lives in exactly one place; link instead of duplicating.

### Owner revisions accepted with this ADR (2026-07-12)

- Inventory split into three modules: Inventory, Batch, Monitoring.
- Products module renamed to Catalog; Pricing is a Catalog capability, not an
  independent module (a standalone Pricing module was briefly added, then
  folded into Catalog at the foundation review).
- Settings module added.
- `docs/shared/` created for cross-module business docs; reference material
  expanded into external/regulations/competitors/assets.
- `docs/shared/events.md` added with the initial business event list.
- `docs/modules/_TEMPLATE/` defines the standard eight-file module doc set.
- `.claude/playbooks/` added; decision logs split into Global and Module.

## Alternatives Considered

Docs organized by type instead of by module; separate documentation repo;
one large all-knowing CLAUDE.md. All rejected — full rationale in the
Sprint 0 proposal (summarized: higher token cost, restructuring risk when
modules are added, and context scattered across distant folders).

## Consequences

Adding a future module = one new folder + one index row. The always-loaded
context stays fixed-size regardless of project growth. Consistency of the
standard module file set is load-bearing and is enforced by
`docs/modules/_TEMPLATE/` + `/new-module`.
