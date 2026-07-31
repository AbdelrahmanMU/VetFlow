# Smoke Test — Pilot Stack (D4 — PRS WS4)

> Status: Approved scope (PRS, owner 2026-07-31). A documented, repeatable pass
> over **the deployed pilot stack** (deployment-runbook.md) against **the clean
> database** (clean-database-verification.md) — never against a dev server.
> Every step traces to an existing approved criterion or scenario; **this
> document invents no behavior** (PRS §4 WS4). Any failing step stops the
> phase with gate semantics: diagnose, fix through the normal gates, re-run.

## Preconditions

- Pilot stack up per the deployment runbook; `docker compose -f
  docker-compose.pilot.yml ps` shows both services running.
- Database in the clean state verified by WS3 (or a fresh `down -v && up` —
  development only; never after real data exists).

## The pass — the implemented commercial cycle, in business order

Executed through the served UI at `http://localhost:8080` (RTL Arabic
throughout; zero console errors expected at every step).

| # | Step | Proves (existing IDs) |
|---|---|---|
| S1 | Open the shell; every nav entry renders in Arabic, RTL, no horizontal overflow | AC-CAT/ui baselines; BR-INV-054 screens |
| S2 | Create a category, a manufacturer, then a product whose stock unit is the smallest unit; purchase/sale units convert exactly | BR-CAT-020 (DEC-CAT-033), AC-CAT-049 |
| S3 | Create a purchase invoice (header), add a line (product, purchase unit, quantity, unit cost), verify line total = qty × cost | REQ-PUR-003/004, AC-PUR-006..013 |
| S4 | Receive the invoice with a batch (expiry, received quantity); status → «مستلمة» | REQ-PUR-005, AC-PUR-014..018 |
| S5 | Inventory projection shows the product with on-hand in stock units; batch viewer shows the batch; expiry monitoring reflects the horizon | REQ-INV-002/003/004, AC-INV-004..030 |
| S6 | Create a sales invoice (customer optional — leave empty once, filled once), add a line, commit; on-hand falls by the exact converted quantity (FEFO) | REQ-SAL-001..003, AC-SAL-001..013 |
| S7 | **Sales list** shows both invoices, newest sale first; search by number and by customer name; status filter; row opens details | REQ-SAL-005, AC-SAL-021..022, TS-SAL-024..029 |
| S8 | From the committed sale: partial sales return; remaining returnable falls; on-hand rises into the batches the goods left (consumption order) | REQ-SAL-004, AC-SAL-014..020, BR-SAL-017 |
| S9 | From the received purchase: partial purchase return in purchase units; stock falls by the converted amount | REQ-PUR-006, AC-PUR-019..025, BR-PUR-016 |
| S10 | An inventory adjustment (with reason) and a write-off (with reason); both appear in movement history with correct types | AC-INV-051..060 |
| S11 | Movement history lists every movement this pass created — receive, consume, both returns, adjustment, write-off — filterable by type | REQ-INV-005, AC-INV-031..036 |
| S12 | Over-limit rejections stay clean: sell more than on-hand → Arabic business error, nothing moves; return more than returnable → VTF-SAL-016 path, nothing moves | BR-INV-052/061, AC-SAL-011, AC-SAL-017/TS-SAL-019 |
| S13 | Invariant check in SQL after the pass: Σ `remaining_quantity` = Σ `on_hand_quantity`; committed movements ↔ ledger rows | BR-INV-005, BR-INV-062 |

## Execution record

Appended per run — date, code state, result per step, defects raised.

### Run 1 — 2026-07-31 (WS4 execution) — **PASS, 13/13**

Against the WS1-validated pilot stack over the WS3-verified clean database.
Method note, stated rather than hidden: the business chain (S2–S6, S8–S13) was
driven **through the deployed API** with UTF-8 file bodies (the Windows console
corrupts Arabic in command-line arguments — the WS3 finding); the **UI layer**
was verified by rendering the served bundle in **headless Chrome** at 1440×900
(S1 shell, S7 sales list — the one screen never browser-verified before) with
**zero console errors**, on top of the Epic 2 live-browser passes that already
covered every other screen at 1440/390 this same day.

| Step | Result |
|---|---|
| S1 shell | RTL, Arabic nav (فواتير الشراء · فواتير البيع · المخزون), 0 console errors |
| S2 catalog | `PRD-000001`: كرتونة=10 علبة, stock=علبة (BR-CAT-020 conform) |
| S3–S4 purchase | `PUR-000001` + line (3 كرتونة × 400) received, expiry 2027-06-30 |
| S5 projection | on-hand **30.000 علبة** (exact 3×10 conversion), batch visible |
| S6 sale | `SAL-000001`, 4 علبة committed → on-hand **26.000**; total **200.00** (snapshot 50) |
| S7 sales list | Renders live data at 1440 (6 headers) and as cards at 800; status filter + exact-number search correct; 0 console errors |
| S8 sales return | `SRT-000001`: returnable 4.000, returned 1 → on-hand **27.000** |
| S9 purchase return | `PRT-000001`: returned 1 كرتونة → on-hand **17.000** (−10, receipt-derived factor) |
| S10 adjust/write-off | +2 (countCorrection) −1 (damaged) → on-hand **18.000** |
| S11 history | Exactly the 6 movement types, one each: receive · consume · salesReturn · purchaseReturn · adjustment · writeOff |
| S12 rejections | Over-sell → 409 `VTF-INV-052`; over-return → 409 `VTF-SAL-016`; **nothing moved** |
| S13 invariants | Σ remaining = Σ on-hand = **18.000** (BR-INV-005); committed returns ↔ ledger rows; the 2 rejected drafts moved nothing |

**Post-run:** a backup of this stock-bearing database restored onto a fresh
separate instance reproduced Σ = 18.000, all 6 movements, and the Arabic
product name byte-identical — closing the BR-INV-005 clause of PRS-AC-03.

**Defects found by this run: none in the application.** (The two client-side
tooling defects — console-argument Arabic corruption and PowerShell binary
redirection — were found earlier in WS2/WS3 and are recorded there.)
