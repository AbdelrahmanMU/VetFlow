# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-07-13

## Current sprint

Sprint 2 — Final Architecture Governance (engineering constitution before
implementation). Sprint 1 (shared docs + Catalog) items remain open below.

**2026-07-13 — Governance structure APPROVED by the owner (in principle).
The governance documents themselves do NOT exist yet: writing them is the
remaining work of Sprint 2 (4 gated waves below). Nothing may be frozen and
no implementation may start until they exist.**

### Approved governance structure (authoritative until the docs exist)

- **ADRs (4 + 1):** ADR-0014 backend architecture (Clean Architecture modular
  monolith; layers + dependency rule; module boundaries via a `Contracts`
  namespace per module; rich domain; domain events; CQRS-lite; **application
  pipeline WITHOUT MediatR** — own `ICommandHandler`/`IQueryHandler` +
  decorators; **no AutoMapper** — manual mapping; no generic repositories;
  composition root; options validated at startup). ADR-0015 API contract
  (URL versioning `/api/v1`, RFC 9457 Problem Details, offset-pagination
  envelope, resource naming — no RPC paths). ADR-0016 testing & architecture
  enforcement (integration-first; BR-ID traceability instead of coverage
  gates; **architecture tests mandatory in CI**; Architecture Rule template
  Reason → ADR → Test → CI → Exception; rule registry IS the test code; tests
  may never be weakened without an ADR + owner approval). ADR-0017 AI
  execution model (Definition of Ready, expanded Definition of Done, context
  loading Mandatory/Optional/Forbidden-by-default, context budgets, playbook
  system). **ADR-0018 business failure strategy — owner ruling 2026-07-13:
  Option A, business exceptions + a structured error catalog (stable code per
  BR-* → HTTP status → Arabic copy); `Result<T>` evaluated and REJECTED
  (unobservable-result silent-failure risk, railway plumbing, C# lacks
  exhaustive matching).**
- **Standards (4):** `docs/architecture/standards/` —
  `csharp-coding-standards.md` (only what Microsoft's guidelines don't
  define), `backend-standards.md` (EF/infra + aggregate conventions),
  `frontend-standards.md` (incl. mandatory `@for`-with-track, `@defer`,
  skeleton/empty/error/loading states, virtualization, optimistic UI
  default-prohibited), `api-standards.md` (worked examples of ADR-0015).
- **Rewrites (2):** `principles.md` → the constitution: **12 principles**
  (business first; domain owns business rules; module boundaries are sacred;
  explicit over implicit; simplicity over cleverness; no speculation; the
  periphery is replaceable; fail fast; integrity over convenience;
  consistency over preference; ADR before irreversible change; **repository
  is the source of truth**) + a **Repository Evolution** section.
  `overview.md` → system map + **Engineering Decision Matrix** section.
- **Rules:** new always-loaded `.claude/rules/ai-governance.md` (≤60 lines);
  `.claude/rules/coding.md` → pointer table to the standards docs.
- **Playbooks:** new `add-module`, `add-lookup`, `amend-module-docs`; rework
  `new-feature.md` into the slice playbook (full / API-only / page-only
  modes); align `refactor.md` + `bug-fix.md` headers. Every playbook carries:
  Inputs → **Context Budget** (Mandatory / Optional / Forbidden-by-default +
  escalation rule: if mandatory context exceeds the budget, STOP and split
  the task — never widen the net) → Steps → Validation → Stop conditions →
  Review gate.
- **Deliberately NOT created** (do not re-propose without new justification):
  separate testing / infrastructure / performance / evolution-policy /
  decision-matrix / rule-registry documents; CONTRIBUTING; a second
  refactoring playbook; `add-api` / `add-page` / `add-aggregate` playbooks
  (merged); style guides duplicating Microsoft/Angular official ones (adopted
  by reference). MediatR / AutoMapper / FluentAssertions rejected (2025
  commercial licensing + implicitness).

### Owner rulings 2026-07-13 (final — apply, do not relitigate)

- **Database: PostgreSQL. FINAL.** Domain and Application must never depend on
  PostgreSQL; DB-specific knowledge lives only in Infrastructure; replacement
  must stay possible. → **ADR-0019** (Wave 2), referencing the study.
- **Business failure: business exceptions + structured Error Catalog. FINAL.**
  `Result<T>` REJECTED (record with reasons). One `DomainException` hierarchy;
  exceptions carry a **stable error code + optional metadata only** — never
  user-facing text, never Arabic copy, never HTTP status. **Middleware is the
  single translation point:** BR-CAT-031 → VTF-CAT-031 → HTTP 409 → Arabic/
  English message → RFC 9457. Every business rule maps to exactly one error
  code; every error code exists in exactly one place. → **ADR-0018** (Wave 2).
- **Exception constraints:** never throw `System.Exception`; never catch-and-
  ignore or swallow; no exceptions as control flow; never expose infrastructure
  exceptions or stack traces to clients; no duplicate error codes. Every
  `BusinessRuleException`: stable code + documented + automated test + RFC 9457
  mapping.
- **Performance Budget** = a section inside the backend architecture ADR-0014
  (API p95, search, checkout, page load, background jobs, startup) — engineering
  targets, evidence-driven, never a licence to pre-optimize.
- **`InfrastructureException` must NOT inherit from `DomainException`** (owner
  ruling, Wave 1 review). It belongs exclusively to Infrastructure, as a
  separate root. Everything else in the hierarchy stands. → ADR-0018.
- **Authority hierarchy (constitutional):** Principles → ADRs → Standards →
  Playbooks. Implementation conforms to the highest applicable authority; a
  lower document may add detail, never contradict. → `principles.md`.
- **Contradiction policy:** contradiction affecting the current wave/task →
  STOP; affecting a future wave → record it and continue. Never invent an
  alternative; never overwrite a newer decision. → `.claude/rules/ai-governance.md`.
- **Quality gates:** `.claude/rules/ai-governance.md` holds **policy + pointers
  only** (owner ruling: ≤ ~70 lines, always-loaded). The **full enumerations**
  (Definition of Ready, commit gate, push gate, repository integrity gate) are
  authoritative in **ADR-0017** — one rule, one source.

### Sprint 2 waves — Wave 1 DONE (Draft, awaiting owner review)

1. **Wave 1 — Constitution: DONE, owner-approved with 4 amendments, committed
   2026-07-13.** `docs/architecture/principles.md` (12 principles incl.
   *repository is the source of truth*; authority hierarchy; Repository
   Evolution), `docs/architecture/overview.md` (system map + Engineering
   Decision Matrix), `.claude/rules/ai-governance.md` (always-loaded policy +
   pointers, 70 lines, incl. contradiction policy). Supporting edits:
   `CLAUDE.md`, `docs/PROJECT_CONTEXT.md` (stale tech-stack drift fixed).
   All Draft — the owner flips statuses when the foundation freezes.
2. Wave 2 — ADRs 0014 (backend architecture + performance budget), 0015 (API
   contract), 0016 (testing & architecture enforcement), 0017 (AI execution
   model), 0018 (business failure strategy), 0019 (PostgreSQL).
3. Wave 3 — the 4 standards docs (`csharp-coding-standards.md` incl. the owner's
   mandatory rule list, `backend-standards.md`, `frontend-standards.md`,
   `api-standards.md`). Unblocked now that the database is decided.
4. Wave 4 — playbooks (+ `coding.md` → pointer table).

*(Wave 1's open item — the `InfrastructureException` hierarchy — was ruled by
the owner and is recorded above; no open items block Wave 2.)*

## Just completed

- **Sprint 1 shared docs (all Draft, uncommitted):** `docs/shared/VISION.md`
  (Arabic, owner-reviewed content), `GLOSSARY.md` (seeded, Arabic forms
  pending approval), `personas.md`, `docs/business/domain-overview.md`
  (TODO questions 2–6 still unanswered), `docs/PROJECT_CONTEXT.md` refreshed.
- **Business Decision Sprint:** `docs/business/DECISION_LOG.md` rebuilt as an
  extraction registry — 31 decisions (`BD-*`), all Draft, each citing its
  source document.
- **Catalog Discovery Workshop:** completed and approved by the owner
  (7 question groups + clarifications; all conflicts resolved).
- **Catalog documentation — 7 of 8 files written (Arabic, Draft), each
  content-reviewed by the owner during generation:** `overview.md`,
  `business-rules.md` (51 rules), `requirements.md` (46), `acceptance.md`
  (46), `workflow.md` (11 flows), `test-scenarios.md` (37), `decisions.md`
  (24 decisions).
- **Module quality review** passed mechanically (full traceability, zero
  broken references); four owner rulings applied (DEC-CAT-020…023: default
  purchase unit added, deactivated-stock clarified, lookup lifecycle rule).
- **Final owner ruling DEC-CAT-024:** purchase-cost data removed from Catalog
  entirely (belongs to Purchasing). BR-CAT-027, REQ-CAT-026, AC-CAT-026,
  TS-CAT-019, DEC-CAT-013, DEC-CAT-021 annulled with IDs reserved.
- **Topic 3 (engineering foundation) — owner decisions received and
  recorded** as ADR-0003…ADR-0009 (all **Proposed** — decisions approved
  2026-07-13, write-ups awaiting owner review) plus
  `docs/architecture/principles.md` (Draft): ASP.NET Core latest LTS, EF Core
  only, Angular latest stable, product API from day one, localization-ready
  Arabic-first MVP (EGP / Gregorian / Western numerals), Docker from the
  beginning, own design system + adaptive UI.
- **Topic 3 owner review applied (2026-07-13):** ADR-0010…ADR-0013 created
  (auth abstraction, Infrastructure-only caching, VetFlow UI Kit / UI-library
  independence, Angular feature-based architecture — all Proposed);
  ADR-0009 amended (design-system scope expanded to 24 areas, first-class
  architectural asset); `principles.md` gained mandatory standards (strict
  TypeScript / no `any`, Smart vs Presentation components, UI independence);
  `docs/architecture/database-platform-study.md` drafted (PostgreSQL vs SQL
  Server, 19 criteria, recommendation only — **decision NOT made**).

## In flight / next

- **Database platform — still open (do NOT assume):** owner will rule after
  reading `docs/architecture/database-platform-study.md`; record the ruling
  as an ADR referencing the study.
- **Auth details still open:** API token mechanism (JWT + refresh rotation
  was recommended, not ruled) and permission-based authorization model — to
  be specified in engineering docs for owner review (see ADR-0010).
- **Backend layering gap — closes with ADR-0014** (wave 2). Until that ADR
  exists, ADR-0010/0011 presuppose
  Domain/Application/Infrastructure layers that no ADR defines yet — propose
  a backend architecture topic (mirror of ADR-0013 for the frontend).
- **UI/UX Architecture discovery topic** — owner-requested, must precede
  engineering documentation: design-system philosophy, layout/navigation
  architecture, adaptive UI strategy, component standards, tables, forms,
  keyboard shortcuts, accessibility, RTL architecture, theme architecture,
  design tokens.
- `docs/modules/catalog/ui.md` — the only missing file of the standard set;
  awaiting owner go-ahead.
- After ui.md: owner flips Catalog docs Draft → Approved, then the module can
  be declared "Approved for Implementation".
- **Recorded cross-module debt (do not lose):** seed `docs/shared/GLOSSARY.md`
  with workshop terms («منتج» canonical, وحدة المخزون، عبوة مفتوحة، …);
  confirm/extend Catalog events in `docs/shared/events.md`; amend
  `VISION.md` principle 5 + `personas.md` + BD-SEC-002 per DEC-CAT-015
  (identical MVP permissions); update Catalog row in
  `docs/modules/_INDEX.md`.
- **Named agenda items for future discoveries:** low-stock threshold
  ownership → Monitoring/Inventory; purchase-cost model (latest/per-unit/
  history) → Purchasing (reopened by DEC-CAT-024); duplicate-match
  strictness → Catalog UI review.
- Nothing committed since the initial commit — the entire Sprint 1 tree is
  uncommitted working state.

## Open questions for the owner

1. Generate `catalog/ui.md` now?
2. Approve the Sprint 1 shared docs (VISION, GLOSSARY, personas,
   domain-overview, PROJECT_CONTEXT) and the BD-* decision registry?
3. Answer domain-overview TODOs 2–6 (credit sales/purchases, official
   invoicing & tax, unit-splitting realities beyond Catalog, volumes)?
4. Commit the Sprint 1 work (suggested: one commit after ui.md review)?
5. Keep the negative boundary statements about purchase cost in
   `catalog/overview.md`, or remove even those?
6. Review the ADR-0003…ADR-0013 write-ups (+ `principles.md`) and flip
   Proposed → Accepted?
7. Rule on the database platform after reading
   `docs/architecture/database-platform-study.md`?
8. Approve a backend-layering architecture topic (Domain/Application/
   Infrastructure) that ADR-0010/0011 presuppose?
