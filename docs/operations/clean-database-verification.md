# Clean Database Verification (D3 — PRS WS3)

> Status: Executed 2026-07-31 against the pilot stack (deployment-runbook.md).
> Owner ruling PRS-Q-04: **reference data only** — operational business data is
> entered by the users during UAT, never seeded.

## The clean-setup procedure (also the pilot-start procedure)

```powershell
docker compose -f docker-compose.pilot.yml down -v   # development only / at transition
docker compose -f docker-compose.pilot.yml up -d --build
```

The API applies all migrations to the empty volume automatically. **After the
Pilot begins this command pair is never run again** (it deletes the database) —
the Pilot Transition Checklist executes it one final time at the transition.

## Verified on 2026-07-31 (PRS-AC-05/06)

- **Zero rows in all 16 business tables** after bootstrap — products,
  categories, manufacturers, purchase/sales invoices and lines, returns and
  lines, batches, movements, on-hand. No development seed
  (`Database__SeedDevelopmentDataAtStartup: "false"` — explicit in the pilot
  compose).
- **All 5 document-number sequences never-called** — the first real documents
  will be `PUR-000001`, `SAL-000001`, `PRT-000001`, `SRT-000001`, and the first
  product code the first of its sequence.
- **Reference data present, from migrations alone, all Arabic intact:**
  5 product natures (دواء · غذاء · مستلزم حيوانات · مستلزم طبي · منتج عناية)
  and 13 units (جرام · زجاجة · سم · شريط · شيكارة · علبة · قرص · قطعة ·
  كرتونة · كيلوجرام · لتر · متر · مل). This is exactly PRS-Q-04's "reference
  data"; nothing else ships.
- **Arabic encoding round-trip through the full deployed stack:** a category
  named «تحقّق الترميز — أدوية وأمصال» (shadda + em-dash included) was created
  through the API and read back **byte-identical**; the stored value measures
  28 characters / 53 bytes — real multi-byte UTF-8, not the historical `????`
  fault (which would store one byte per character).
- **The `????` fault class is root-caused and closed:** the deployed stack
  round-trips Arabic perfectly. The historical corruption reproduces only when
  Arabic passes through a **Windows console command line** (curl `-d` argument,
  PowerShell here-string) — a test-client artifact, not an application defect.
  Operational consequence: none for the pilot — users enter Arabic through the
  browser, which is the path verified here.
- **BR-CAT-020 (stock unit = smallest):** the clean database holds zero
  products, so no configuration can violate it at the start; products entered
  during UAT go through the application's own validation, and smoke step S2
  exercises exactly that path. The Sprint 7 "existing product configs audit"
  leftover concerns the **development** database only and does not touch the
  pilot.

## Residue of this verification, stated plainly

The round-trip category above remains in the database that WS4's smoke pass
runs on (it is verification data by ADR-0020's definition, not pilot data).
The **final** clean reset happens once, at the Pilot Transition, per the
checklist — after which the first data entered is the owner's real data.
