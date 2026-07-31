# Go / No-Go Report — Pilot Readiness (D7 — PRS WS6)

> Status: **DECIDED — GO (owner, 2026-07-31).** In the owner's words: the
> system has demonstrated operational readiness through successful deployment
> validation, backup and restore verification, clean database validation,
> smoke testing, and preparation for UAT; the remaining technical debt is
> documented, understood, and acceptable for the Pilot.
> Assembled at the completion of WS1–WS6 under the approved PRS
> (`docs/shared/roadmap/releases/pilot-readiness.md`).

## The owner's Pilot-execution rules (ruled with the GO, 2026-07-31)

- **All Pilot findings are recorded under exactly three categories: Bug ·
  Usability · Enhancement** — the log is
  [`pilot-findings.md`](pilot-findings.md).
- **No new features during the Pilot unless required to keep the system
  operational** — the `BD-PRD-008` discipline, extended by the owner through
  the Pilot itself.

## Verdict proposed for the owner's consideration (retained as submitted)

**GO — with three owner-side actions before the transition** (§Owner actions).
Every workstream completed, every acceptance criterion passed with recorded
evidence, and the five defects found on the way were all fixed and re-verified
— none was in the application itself.

## Workstream evidence

| WS | Result | Evidence |
|---|---|---|
| Sales Invoice List (pre-WS1, DEC-SAL-005) | **Done, green** | REQ-SAL-005 · BR-SAL-019 · AC-SAL-021..022 · TS-SAL-024..029; 13 new integration tests + 8 store specs; smoke S7 renders it live |
| WS1 Deployment | **PASS** | [`deployment-runbook.md`](deployment-runbook.md) — two clean bootstraps, Production config, 12/12 migrations auto-applied, same-origin bundle, SPA deep links, canonical API 404 |
| WS2 Backup & Restore | **PASS** | [`backup-restore-runbook.md`](backup-restore-runbook.md) — verified restore onto a fresh instance (identical counts, byte-identical Arabic), 3.3 s scripted restore, retain-7 exercised; 3 script defects found by execution and fixed |
| WS3 Clean Database | **PASS** | [`clean-database-verification.md`](clean-database-verification.md) — zero business rows, never-called sequences, Arabic round-trip byte-identical, `????` fault class root-caused as client-side |
| WS4 Smoke | **PASS 13/13** | [`smoke-test.md`](smoke-test.md) — full commercial cycle on the deployed stack incl. all 6 movement types, both rejection paths, BR-INV-005 = 18.000 both sides, zero console errors |
| WS5 UAT pack | **Ready** | [`uat/`](uat/) — plan + owner script + cashier script + defect log, Arabic, per the ruled participants |
| WS6 Gate & checklist | **PASS** | STD-BE-051 scan live and green (detects the one approved destructive migration); final sweep: build 0/0 · format clean · Domain 163 · Architecture 134 · Integration 236 · frontend 235 · ESLint/Stylelint clean · `ng build` exit 0 (TD-107 warning only) |

## Pilot Transition Checklist (ADR-0020 — executed at the transition)

Each item's mechanism is proven now; the checklist itself is completed **at the
transition, before real operational data is entered**:

| Item (owner's wording) | Mechanism proven | At transition |
|---|---|---|
| All migrations applied. | 12/12 auto-applied on both WS1 bootstraps | Automatic on final bootstrap |
| Database backup completed. | WS2 verified backup+restore | Run `backup-vetflow.ps1` once UAT data exists |
| Seed data finalized. | WS3: reference data ships from migrations; PRS-Q-04 rules out any other seed | Nothing to do — confirmed final |
| No destructive migrations pending. | STD-BE-051 scan **PASS** (now a live gate) | Re-run the scan |
| Current schema tagged. | Tag text ready | **Blocked on two owner actions:** the Pilot Readiness commit approval, then the git tag; pushing the tag additionally needs GCM cleared |

## Risk register at close (PRS §7)

- **Closed by execution:** RSK-01 (frontend deployment — WS1), RSK-02 (no
  backup — WS2), RSK-07 (Arabic encoding — WS3, root-caused client-side),
  RSK-08 (sales list — built and green).
- **Open, owner-held:** RSK-03 (**GCM push block** — repository still exists on
  one machine only; `! git push origin main`), RSK-06 (WS1 ran on the dev
  machine, which matches the ruled spec — **the one remaining unknown is the
  actual clinic workstation**, closed by running the runbook there once),
  RSK-04 (no auth — ruled, physical access control), RSK-05 (unbuilt MVP
  modules — UAT may surface needs; owner judges).
- **Accepted:** RSK-09 / TD-107 (bundle warning, 567.05 kB after the list).

## Owner actions before the transition — status after the GO (2026-07-31)

1. **UAT sessions** with both participants (scripts ready; schedule is yours)
   — **the one remaining action.**
2. ~~Clear the Git Credential Manager block and push~~ — **DONE:** the complete
   history is pushed (`main → 5f8e761`); PRS-RSK-03 closed.
3. ~~Commit approval + schema tag~~ — **DONE:** committed on the owner's
   approval and tagged **`pilot-2026-07-31`** (pushed). The checklist's
   «Current schema tagged» item is satisfied.
