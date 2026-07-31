# VetFlow — Project Context

> Status: Draft — pending owner review.
> Stable project snapshot. Changes rarely. Volatile state lives in `STATUS.md`.

## Product

- **VetFlow** — veterinary clinic management platform for a single clinic.
- **Users:** Veterinary Doctor (owner), Cashier/Assistant — see
  `docs/shared/personas.md`.
- **UI language:** Arabic (RTL). See
  `docs/architecture/cross-cutting/localization.md`.
- **Vision:** `docs/shared/VISION.md` (Draft). Domain:
  `docs/business/domain-overview.md` (Draft).

## MVP scope (see `docs/shared/roadmap/releases/mvp.md` — amended 2026-07-31)

Split by owner ruling into **Pilot MVP Scope** (delivered and validated:
Catalog incl. Pricing · Categories · Purchasing incl. receiving & returns ·
Sales incl. list & returns · Inventory incl. batches, expiry, ledger,
adjustments, write-offs) and **Post-Pilot Scope** (intentionally postponed by
recorded owner decisions: Cash Management · Expenses · Reports · Audit Log ·
Settings · in-app Local Backup · Suppliers/Customers as modules · Monitoring
alerts). The authoritative split, with each deferral's ruling, lives in
`mvp.md` — this file does not duplicate it.

MVP covers the clinic's commercial cycle only; the medical side is
deliberately post-MVP.

## Future modules (must remain additive — not in MVP)

Medical Services · Animal Records · Appointments · Vaccinations ·
Prescriptions · Laboratory · Imaging · Multi-Branch · Cloud Synchronization

## Philosophy

- Documentation-First: no implementation before approved docs.
- Business decisions belong to the owner and are final. Never assume — ask,
  and record the answer (decision routing in `.claude/rules/workflow.md`).
- Hybrid documentation language (ADR-0002): engineering docs in English,
  business/product docs in Arabic; `docs/shared/GLOSSARY.md` is the EN↔AR
  bridge.
- Developed AI-assisted with Claude Code; structure per ADR-0001; work
  procedures in `.claude/playbooks/`.

## Decisions so far

| Decision | Where | Status |
|---|---|---|
| Repository structure | ADR-0001 | Accepted |
| Documentation language (hybrid: engineering EN, business AR) | ADR-0002 | Accepted |
| Engineering foundation (stack, API, localization, UI, auth, cache, frontend) | ADR-0003 … ADR-0013 | Proposed |
| Pricing is a Catalog capability, not a module | `docs/modules/catalog/decisions.md` | Decided (owner) |

Full list: `docs/architecture/decisions/_INDEX.md`. The map of what is decided
where: `docs/architecture/overview.md` (Engineering Decision Matrix).

## Tech stack

ASP.NET Core (latest LTS) · EF Core · **PostgreSQL** · Angular (latest stable)
· VetFlow UI Kit over PrimeNG · Docker. See
`docs/architecture/overview.md` for the authoritative table and the ADR behind
each choice.

## TODO — open items

- Success criteria may get quantitative targets (owner review of
  `docs/shared/VISION.md`).
- Initial business events and glossary Arabic forms pending owner approval
  (`docs/shared/events.md`, `docs/shared/GLOSSARY.md`).
