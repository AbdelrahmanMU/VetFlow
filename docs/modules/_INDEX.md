# Modules Index

> One row per business module. Update when a module is added or its status changes.
> Statuses: `Not documented → Draft → Review → Approved → Implemented`.
> `_TEMPLATE/` is the scaffold for the standard module doc set — not a module.

| Module | Folder | Status |
|---|---|---|
| Catalog (incl. Pricing) | `catalog/` | Approved (docs 8/8, 2026-07-14) — Product List + Create/View Details + Edit (non-audited unified editor, DEC-CAT-031) + Manufacturer Managed Data (REQ-CAT-047/048, DEC-CAT-032) slices implemented & committed (`9e5c99c`, 2026-07-16) |
| Categories | `categories/` | Approved (docs 8/8, 2026-07-16 — Managed Data slice, REQ/BR/AC-CTG, owner-ruled) — implemented & committed (`9e5c99c`, 2026-07-16) |
| Suppliers | `suppliers/` | Not documented |
| Customers (minimal) | `customers/` | Not documented |
| Purchasing | `purchasing/` | Approved (Sprint 4; docs 8/8) — Slice 1 Purchase List (2026-07-16, REQ-PUR-001/BR-PUR-001..004/AC-PUR-001..003/DEC-PUR-001) · Slice 2 Purchase Details (2026-07-17, REQ-PUR-002/AC-PUR-004..005/TS-PUR-008..011) · Slice 3 Create Purchase (2026-07-17, REQ-PUR-003/AC-PUR-006..007/TS-PUR-012..015/DEC-PUR-002) · Slice 4 Purchase Line Items (REQ-PUR-004/BR-PUR-005..008/AC-PUR-008..013/TS-PUR-016..024/DEC-PUR-003..006) — **Slices 1–4 implemented & committed (see STATUS.md)**; Slice 5 Purchase Receiving (REQ-PUR-005/BR-PUR-009..013/AC-PUR-014..018/TS-PUR-025..033/DEC-PUR-007..009) — **APPROVED (DoR complete, 2026-07-22), owner-ruled; not yet implemented. Depends on the Inventory public contract only (DEC-PUR-008 — no new ADR)** |
| Sales | `sales/` | Not documented |
| Inventory | `inventory/` | Not documented (module pending) — **Write Kernel (Receiving support) APPROVED + implemented with Purchasing Slice 5 (2026-07-22): `write-kernel.md` — REQ-INV-001 / BR-INV-001..005 / AC-INV-001..003 / TS-INV-001..003 / DEC-INV-001..002. Not the Inventory module; reference-only links to Catalog/Purchasing, no cascade.** |
| Batch | `batch/` | Not documented |
| Monitoring | `monitoring/` | Not documented |
| Cash Management | `cash/` | Not documented |
| Expenses | `expenses/` | Not documented |
| Reports | `reporting/` | Not documented |
| Audit Log | `audit-log/` | Not documented |
| Local Backup | `backup/` | Not documented |
| Settings | `settings/` | Not documented |
