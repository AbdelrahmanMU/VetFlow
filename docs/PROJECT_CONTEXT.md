# VetFlow — Project Context

> Stable project snapshot. Changes rarely. Volatile state lives in `STATUS.md`.

## Product

- **VetFlow** — veterinary clinic management platform.
- **Users:** Veterinary Doctor (owner), Cashier/Assistant.
- **UI language:** Arabic (RTL). See `docs/architecture/cross-cutting/localization.md`.

## MVP scope (fixed — see `docs/shared/roadmap/releases/mvp.md`)

Catalog (incl. Pricing) · Categories · Suppliers · Customers (minimal) ·
Purchasing · Sales · Inventory · Batch · Monitoring · Cash Management ·
Expenses · Reports · Audit Log · Local Backup · Settings

## Future modules (must remain additive — not in MVP)

Medical Services · Animal Records · Appointments · Vaccinations ·
Prescriptions · Laboratory · Imaging · Multi-Branch · Cloud Synchronization

## Philosophy

- Documentation-First: no implementation before approved docs.
- Business decisions belong to the owner and are final. Never assume — ask.
- Developed AI-assisted with Claude Code; structure per ADR-0001.

## Decisions so far

| Decision | Where | Status |
|---|---|---|
| Repository structure | ADR-0001 | Accepted |
| Documentation language (hybrid: engineering EN, business AR) | ADR-0002 | Accepted |

## Tech stack

Not chosen yet. Will be decided by a future ADR after MVP documentation.
