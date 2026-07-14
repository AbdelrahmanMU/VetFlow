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
| 1 | `docs/modules/catalog/ui.md` written | ✅ **written 2026-07-14 (Draft)** — Catalog doc set now 8/8 |
| 2 | System-wide Design Language exists | ✅ **`docs/ui/design-language.md` written 2026-07-14 (Draft)** — owner required it before Catalog UI could be approved |
| 3 | Catalog documentation approved by the owner | ❌ **pending — the gate** |
| 4 | Catalog docs flipped Draft → Approved | ❌ pending |
| 5 | Repository status synchronized | ✅ done (this file) |
| 6 | Architecture baseline approved | ✅ Sprint 2 complete — ADR-0001…0019 Proposed, statuses flip on the owner's word |

**Next action (owner):** review `docs/ui/design-language.md`, then re-read
`catalog/ui.md` **against the mandatory design review checklist** in
design-language §16 (it is now the gate every module UI document must pass),
then approve Catalog and flip its docs Draft → Approved. **Code begins after
that, not before.**

## Just completed (2026-07-14)

- **`docs/modules/catalog/ui.md` — written (Arabic, Draft).** UI architecture
  only: 7 screens (S1 product list · S2 detail · S3 editor · S3-م editor embedded
  in the purchase invoice · S4 unit profile · S5 prices · S6 managed lookups),
  search-first navigation, premium table language, sectioned forms with
  progressive disclosure, unified dialog patterns, adaptive desktop→mobile
  (mobile redesigned as a lookup tool, not a shrunken table), accessibility,
  Arabic microcopy. **Traceability verified mechanically:** all 45 active REQs
  covered or explicitly declared as having no Catalog UI surface; REQ-CAT-026
  correctly absent (annulled); zero dangling REQ/WF/BR/DEC references.
- **`docs/ui/design-language.md` — written (Arabic, Draft).** The product's
  **visual constitution**, ordered by the owner before Catalog UI could be
  approved (rationale: without one, every future module drifts visually even if
  each screen is individually good). 17 sections: personality · principles ·
  hierarchy & attention · layout (RTL shell — **sidebar on the right**) · one
  table language · one form language · navigation · feedback (**undo preferred
  over confirmation**; a dialog that prevents no error is deleted) · typography ·
  color (**neutral base carries 90%; primary used sparingly; no color-only
  meaning; no zebra striping**) · icons · motion · accessibility · 13 golden
  rules · **the mandatory design review checklist (§16)** · module relationship.
  **Anti-drift clause (§17): modules may extend, never redefine — and a gap in
  the language is raised as an extension request, never patched locally.**
- No new business decision was made this session — both documents derive
  entirely from already-approved Catalog docs and ADR-0007/0009/0012. Nothing
  required a new `DEC-*`, `BD-*`, or ADR entry.

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

- **Catalog module: 8 of 8 documents written** (Arabic, Draft, owner
  content-reviewed) — 51 business rules, 46 requirements, 46 acceptance
  criteria, 11 workflows, 37 test scenarios, 24 decisions, and `ui.md`
  (2026-07-14). **The set is complete and awaits owner approval.**
- **Shared docs (Draft):** `VISION.md`, `GLOSSARY.md`, `personas.md`,
  `domain-overview.md` (TODOs 2–6 unanswered), `PROJECT_CONTEXT.md`.
- **`DECISION_LOG.md`:** 31 `BD-*` decisions, all Draft.

## Open items for the owner

1. **Review `docs/ui/design-language.md`** (the visual constitution).
2. **Re-read `catalog/ui.md` against the design review checklist**
   (design-language §16) — that checklist is now the gate for every module UI doc.
3. **Approve Catalog + flip Draft → Approved — this gates ALL implementation.**
4. Approve the Sprint 1 shared docs and the `BD-*` registry.
5. Answer `domain-overview.md` TODOs 2–6 (credit sales/purchases, official
   invoicing & tax, unit-splitting, volumes).
6. Flip ADR-0003…0019 Proposed → Accepted when ready.
7. Confirm the CI performance budget numbers (ADR-0016 §5).
8. Confirm the Catalog `overview.md` question: keep or remove the negative
   boundary statements about purchase cost (DEC-CAT-024). **Still unanswered
   since Sprint 1 — worth settling before Catalog is frozen as Approved.**
9. Decide whether `docs/ui/components.md` and `docs/ui/navigation.md` (still
   placeholders) get written now, or wait until the UI/UX Architecture discovery
   the owner requested in Topic 3.

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
