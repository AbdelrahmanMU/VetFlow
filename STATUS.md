# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-07-14

## Current sprint

**Sprint 3 — Implementation.** The first product code of VetFlow.

Implementation outranks governance. If implementation exposes a weakness in the
foundation: **record it under "Foundation friction" below, keep working if it is
safe, and evaluate the change only after the feature is complete.** Governance
changes require evidence (Governance Change Policy — `docs/architecture/principles.md`).

**Every implementation session starts at `.claude/playbooks/implementation.md`.**

### Sprint 3 gate — Definition of Ready (nothing else blocks implementation)

| # | Condition | State |
|---|---|---|
| 1 | `docs/modules/catalog/ui.md` written | ❌ **missing — the only file of the standard set not written** |
| 2 | Catalog documentation approved by the owner | ❌ pending |
| 3 | Catalog docs flipped Draft → Approved | ❌ pending |
| 4 | Repository status synchronized | ✅ done (this file) |
| 5 | Architecture baseline approved | ✅ **Sprint 2 complete** — ADR-0001…0019 Proposed, statuses flip on the owner's word |

**Next action:** write `catalog/ui.md` (owner go-ahead needed), then the owner
approves Catalog and flips its statuses. Code begins after that, not before.

## Sprint 2 — Engineering Foundation (COMPLETE, 2026-07-14)

The foundation is complete and stable. **Stability is not prohibition** — the
repository remains evolvable; what changed is the burden of proof.

| Layer | Where | Contents |
|---|---|---|
| Constitution | `docs/architecture/principles.md` | 14 principles · authority hierarchy (Principles → ADRs → Standards → Playbooks) · repository evolution · Governance Change Policy |
| Map | `docs/architecture/overview.md` | System shape · Engineering Decision Matrix |
| Decisions | `docs/architecture/decisions/` | **ADR-0001 … ADR-0019** (all Proposed/Accepted per `_INDEX.md`) |
| Standards | `docs/architecture/standards/` | **137 executable standards** — `STD-CS` (33) · `STD-BE` (47) · `STD-FE` (35) · `STD-API` (22) |
| AI rules | `.claude/rules/ai-governance.md` | Always loaded: session protocol · context loading · contradiction policy · gate pointers |
| Execution | `.claude/playbooks/implementation.md` | The **only** implementation playbook: 10 modes · 5-stage context loader · token budgets · 9-question self review |

**The stack:** ASP.NET Core (LTS) · EF Core · **PostgreSQL** · Angular (stable) ·
VetFlow UI Kit over PrimeNG · Docker.
**Recorded as rejected — do not re-propose without evidence:** MediatR ·
AutoMapper · FluentAssertions · generic repositories · `Result<T>` · NgRx.

**Retired 2026-07-14:** `new-feature`, `bug-fix`, `refactor`, `review`,
`release` playbooks → `.claude/playbooks/superseded/` (history only, never
load). `discovery.md` remains **active** for business discovery workshops.

## Sprint 1 — Documentation (carried forward)

- **Catalog module:** 7 of 8 documents written (Arabic, Draft, owner
  content-reviewed) — 51 business rules, 46 requirements, 46 acceptance
  criteria, 11 workflows, 37 test scenarios, 24 decisions. `ui.md` outstanding.
- **Shared docs (Draft):** `VISION.md`, `GLOSSARY.md`, `personas.md`,
  `domain-overview.md` (TODOs 2–6 unanswered), `PROJECT_CONTEXT.md`.
- **`DECISION_LOG.md`:** 31 `BD-*` decisions, all Draft.

## Open items for the owner

1. Go-ahead to write `catalog/ui.md`.
2. Approve Catalog + flip Draft → Approved (**gates all implementation**).
3. Approve the Sprint 1 shared docs and the `BD-*` registry.
4. Answer `domain-overview.md` TODOs 2–6 (credit sales/purchases, official
   invoicing & tax, unit-splitting, volumes).
5. Flip ADR-0003…0019 Proposed → Accepted when ready.
6. Confirm the CI performance budget numbers (ADR-0016 §5).
7. Confirm the Catalog `overview.md` question: keep or remove the negative
   boundary statements about purchase cost (DEC-CAT-024).

## Cross-module debt (do not lose)

- Seed `GLOSSARY.md` with the Catalog workshop terms («منتج» canonical, وحدة
  المخزون، عبوة مفتوحة…).
- Confirm/extend Catalog events in `docs/shared/events.md`.
- Amend `VISION.md` principle 5 + `personas.md` + BD-SEC-002 per DEC-CAT-015
  (identical MVP permissions).
- Update the Catalog row in `docs/modules/_INDEX.md`.
- Future discovery agenda: low-stock threshold ownership → Monitoring/Inventory ·
  purchase-cost model → Purchasing (reopened by DEC-CAT-024) · duplicate-match
  strictness → Catalog UI review.

## Foundation friction (evidence for future governance change)

*Record here when implementation hits a governance weakness. Do not act on it
mid-feature. Empty = the foundation is holding.*

| Date | Friction | Occurrences | Proposed change |
|---|---|---|---|
| — | *(none yet)* | | |
