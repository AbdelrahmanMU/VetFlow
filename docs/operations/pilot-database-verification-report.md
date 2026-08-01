# Pilot Database Verification Report

> **Status: Submitted for owner review — 2026-08-01.**
> Produced at the owner's explicit request: create a clean Pilot-ready
> database, then verify it. This executes the **final clean reset** of the
> Pilot Transition Checklist (ADR-0020). **Nothing is committed.**

## 1 · What was reset, and what was deliberately not

| Target | Action |
|---|---|
| **Pilot stack** — compose project `vetflow-pilot`, volume `vetflow-pilot_vetflow-pilot-db-data` | **Reset.** Volume destroyed and rebuilt from migrations |
| **Development stack** — project `vetflow`, volume `vetflow_vetflow-db-data` (host port 5434) | **Untouched, by design.** Verified after the reset: still 3 products · 49 sales invoices · 256 movements |

Isolation was proven **before** the destructive step, not assumed: the pilot
compose declares `name: vetflow-pilot`, and `docker compose config --volumes`
resolved to exactly one volume (`vetflow-pilot-db-data`). The development
volume was never in scope of the command.

## 2 · Proof that no real clinic data was destroyed

The pilot database was inspected **before** the reset. Every record matched
the documented WS4 smoke pass and encoding round-trip — verification data by
ADR-0020's own definition, never operational clinic data:

| Evidence | Finding |
|---|---|
| Document numbers | `PUR-000001` · `SAL-000001` · `SAL-000002` · `PRT-000001` · `SRT-000001` · `SRT-000002` — the smoke sequence, first-of-series |
| Creation timestamps | all `2026-07-31 13:07:39 → 13:09:21 UTC` — a 102-second window, the WS4 run |
| The 28-character / 53-byte category | exactly the encoding round-trip record documented in `clean-database-verification.md` §Verified |
| Volume held | 1 product · 2 categories · 1 manufacturer · 6 movements · 1 batch |

The Pilot has not begun — no first real operational entry exists (ADR-0020's
trigger), so the reset carries no data-loss risk to the clinic.

## 3 · Procedure used — the ruled one

Per `docs/operations/clean-database-verification.md` (owner ruling PRS-Q-04:
**reference data only**; business data is entered by users, never seeded):

```
docker compose -f docker-compose.pilot.yml down -v
docker compose -f docker-compose.pilot.yml up -d
```

The API applied all migrations to the empty volume at startup and became
healthy in 2 seconds. **No hand-written `DELETE`/`TRUNCATE` was used** — the
database is not a cleaned database, it is a newly created one, which is what
"identical to a brand-new clinic installation" requires.

**One deviation from the documented command, stated plainly:** the runbook
writes `up -d --build`; this run used `up -d`, reusing the existing
`vetflow-pilot-api:latest` image from the Pilot Readiness build. Reason: the
source tree carries **12 migration files and the database applied 12** — the
uncommitted Validation UX work changed no backend or migration code, so the
database outcome is byte-for-byte identical, while `--build` would have baked
uncommitted, unapproved frontend code into the pilot image. If you prefer the
pilot image rebuilt from the current tree, that is a separate decision and one
command; it does not change any result below.

## 4 · Verification results — all seven goals

### 4.1 Every business table is empty

All 16 business tables at **0 rows**, counted with exact `count(*)`, not
statistics estimates:

| Table | Rows | Table | Rows |
|---|---|---|---|
| categories | **0** | purchase_returns | **0** |
| manufacturers | **0** | purchase_return_lines | **0** |
| products | **0** | sales_invoices | **0** |
| product_units | **0** | sales_line_items | **0** |
| purchase_invoices | **0** | sales_returns | **0** |
| purchase_line_items | **0** | sales_return_lines | **0** |
| inventory_batches | **0** | product_on_hands | **0** |
| inventory_movements | **0** | *(all 16 verified)* | |

### 4.2 Sequences start from the first business number

All five document sequences are **never-called** with `start_value = 1`, so
the first documents the clinic creates will be `PUR-000001`, `SAL-000001`,
`PRT-000001`, `SRT-000001`, and product code `PRD-000001`:

| Sequence | start_value | last_value | State |
|---|---|---|---|
| `product_internal_code_seq` | 1 | — | **NEVER CALLED** |
| `purchase_invoice_number_seq` | 1 | — | **NEVER CALLED** |
| `purchase_return_number_seq` | 1 | — | **NEVER CALLED** |
| `sales_invoice_number_seq` | 1 | — | **NEVER CALLED** |
| `sales_return_number_seq` | 1 | — | **NEVER CALLED** |

### 4.3 Inventory totals are zero

| Measure | Rows | Sum |
|---|---|---|
| `product_on_hands.on_hand_quantity` | 0 | **0** |
| `inventory_batches.remaining_quantity` | 0 | **0** |
| `inventory_batches.quantity` (received) | 0 | **0** |
| `inventory_movements.quantity` | 0 | **0** |

### 4.4 Movement history is empty

`inventory_movements` = **0 rows**. The ledger is untouched, so the first
movement the clinic sees will be its own first receipt.

### 4.5 No orphan records

A **dynamic scan across every foreign key** was executed (not a spot check):
all 12 FK relationships were enumerated from `pg_constraint` and each was
queried for children without a parent.

> `FK constraints scanned: 12 | total orphan rows: 0`

### 4.6 Integrity checks

| Check | Result |
|---|---|
| Foreign-key constraints | 12 present, **0 unvalidated** (no `NOT VALID`) |
| Primary-key constraints | 18 present, **0 unvalidated** |
| Indexes | 65 present, **0 invalid**, **0 not-ready** |
| Migration history | **12 applied**, latest `20260731123845_SalesInvoiceSearchText` |
| Schema vs EF model | `dotnet ef migrations has-pending-model-changes` → **"No changes have been made to the model since the last migration."** |
| Destructive-migration scan (STD-BE-051 / ADR-0020) | **PASS** — no unapproved destructive operations; the one `DropTable` is the owner-approved `20260731003637_InventoryMovementLedger` on record |
| Application-level confirmation (pilot API `:8080`) | products · categories · manufacturers · purchase-invoices · sales-invoices all return `totalCount = 0` |

### 4.7 Required data for a fresh installation is present and intact

Exactly the reference data PRS-Q-04 permits, installed by migrations alone —
**5 product natures** and **13 measurement units**, matching the documented
clean-install baseline:

- **Natures (5):** دواء · غذاء · مستلزم حيوانات · مستلزم طبي · منتج عناية
- **Units (13):** جرام · زجاجة · سم · شريط · شيكارة · علبة · قرص · قطعة · كرتونة · كيلوجرام · لتر · متر · مل

**Arabic encoding proven, not assumed:** every one of the 18 reference rows
has `octet_length > char_length` — real multi-byte UTF-8. The historical
`????` fault would store one byte per character and fail this test. The pilot
API returns `product-natures` = 5 and `units` = 13.

## 5 · Verdict

**The pilot database is clean and Pilot-ready.** It is indistinguishable from
a first-time installation on a new clinic machine: zero business rows, zero
inventory, zero history, unissued document sequences, full referential
integrity, and only migration-installed reference data.

## 6 · State of the Pilot Transition Checklist (ADR-0020)

| Item | State |
|---|---|
| All migrations applied | **Done** — 12/12, verified against the EF model |
| Seed data finalized | **Done** — reference data only, per PRS-Q-04 |
| No destructive migrations pending | **Done** — scan PASS |
| Current schema tagged | **Done** — `pilot-2026-07-31`; the Epic changed no migration, so the tagged schema is the running schema |
| **Database backup completed** | **Not done** — the remaining item. `scripts/backup-vetflow.ps1` exists and was validated in WS2; taking the first backup of this clean baseline is your call, and I have not run it |

## 7 · Standing facts

- **Nothing was committed.** The working tree still holds the uncommitted
  Validation UX Adoption Epic awaiting Epic Commit Approval.
- **The pilot stack is running** — API on `127.0.0.1:8080` (loopback only),
  database on loopback `5435`.
- **The development stack is also running and is not clean** — dev API `5080`,
  web `4200`, dev database `5434`, still holding the test/perf data. Anything
  tested at `localhost:4200` is *not* the clean pilot database.
- The next data entered into the pilot database is, by ADR-0020's definition,
  **real clinic data** — and from that moment `down -v` must never run again.
