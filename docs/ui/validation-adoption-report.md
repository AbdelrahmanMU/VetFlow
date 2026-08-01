# Validation Foundation — Adoption Test Report

> **Status: Submitted for owner review — 2026-07-31.**
> Commissioned by the owner ruling of 2026-07-31 (recorded in
> `validation-and-guidance.md` §0, second addendum): one Adoption Test on one
> medium-complexity production screen before Phase 2, with the Foundation
> **frozen** — no modification unless a real defect is discovered, only
> approved reusable infrastructure, no new validation components. The
> objective: prove the Foundation **minimizes future implementation effort**,
> not merely that it functions correctly.

## 1 · Screen chosen and why

**Purchase create** (`web/src/app/features/purchasing/purchase-create/`,
`/purchases/new`). Audit verdict **Partial** (gap analysis §3): a real typed
reactive form of medium complexity — two required fields, two optional, a
write path, navigation on success. Chosen deliberately **not** to be a mirror
of either POC screen (write-off mirrors the adjustment POC; the manufacturer
dialog mirrors the category dialog — either would bias the effort estimate),
and because it exercises Foundation surface the POCs never touched: the
**`vf-date-input` ControlValueAccessor repair in a real blur-validated form**
(the audit's named gap: «invoiceDate cannot error on blur»), and the
`ApiErrorMapper` + `projectServerFieldErrors` path driven by a **live**
`VTF-VAL-001` response rather than a simulated one.

## 2 · The numbers

| Metric | Value |
|---|---|
| Lines removed / added — production code (component + forms + i18n) | **−67 / +141** (`git diff --numstat`) |
| Lines removed / added — spec | −21 / +91 (5 tests → 7) |
| Screen-local validation/guidance code after adoption | **≈ 55 lines** (failure signal + banner focus/clear wiring + server-field map + classification call) |
| Foundation code exercised, unmodified | **≈ 640 lines** (8 of the 10 foundation files; `vf-validation-summary` and `debouncedCheck` correctly not needed on a short form) |
| **Reuse percentage** (shared ÷ (shared + screen-local) validation code) | **≈ 92 %** |
| Reused pieces | **9** — `vf-form-field` · `vf-validation-message` · `SubmitGuidanceDirective` · `ValidationFocusService` · `ApiErrorMapper` · `VTF_ERROR_REGISTRY` · `vfValidators` · `projectServerFieldErrors` · the repaired input CVAs (`vf-date-input` first real-form use, `vf-text-input`, `vf-textarea`, `vf-button type="submit"`) |
| New validation components introduced | **0** |
| Foundation files modified | **0** |
| New Arabic strings | **0** (one dead key removed: `purchaseCreate.required`, byte-identical to the shared `validation.required`) |
| Bundle | 579.82 → **579.71 kB** (−0.11 kB) |

The production line count **rises** (+74 net) while duplicated plumbing was
deleted, because the screen *gained six behaviors it never had*: moment-2
blur validation (dates included), moment-1 clearing + success confirmation,
first-invalid focus, the full `aria-invalid`/`aria-describedby` chain, VTF
classification with a focusable self-clearing banner, and server field-error
projection. Implemented without the Foundation, those six cost ≈ 700 lines
per screen (the Phase-1 actuals); with it they cost ≈ 55.

## 3 · Verification (all green, 2026-07-31)

- **Gates:** ESLint clean · Stylelint clean · frontend tests **268/268**
  (+2 net; the migrated suite covers all three moments, projection, and
  banner-clear-on-edit) · `ng build` exit 0.
- **Live browser (headless Chrome/CDP, real stack, 1440×900 + 390×844):**
  **16/16 checks** — rtl/lang, zero overflow at both widths, no error before
  any moment, label `for`-wiring, **date blur error (the closed CVA gap)**,
  exactly two required errors on empty submit, focus into the first invalid
  field, `aria-describedby` resolving to the visible error line, clear-on-
  keystroke + success state, **live `VTF-VAL-001` projected inline with no
  banner** (whitespace-only supplier name — the one real client-side escape),
  projected error clearing on edit, end-to-end save navigating to the created
  invoice's details, zero console errors.
- Dev-data side effect (pre-pilot): one draft purchase invoice
  («مورد تحقق الاعتماد», 2026-07-31) created by the end-to-end save.

## 4 · Foundation verdict

- **Defect discovered: none.** The Foundation ran unmodified.
- **Insufficient API: none.** Every behavior the standard requires of this
  screen was expressible with the approved surface. Two pre-existing,
  owner-ruled deferrals were felt but are not insufficiencies: `vf-banner`
  (the screen keeps ~10 lines of local banner CSS, as ruled for Phase 1) and
  the system-banner path is spec-covered but not browser-drivable without
  killing the API (accepted; unit test pins it).
- One **pattern note** for Phase 2, no API change needed: screens without a
  store call `ApiErrorMapper` directly in the component (this screen), while
  store-backed screens classify in the store (adjustment POC). Both are
  standard-conformant; Phase 2 should keep each screen's existing shape.

## 5 · Projected effort for the remaining screens (from this real adoption)

This migration cost **≈ 0.5 focused dev-day** including spec rewrite and live
browser verification — against the audit's 1–1.5 d implied share. Applying
the measured ~0.5× factor to form-migration work (long-form extras kept at
full weight):

| Package | Screens | Estimate |
|---|---|---|
| Sale create | mirror of this screen | 0.25–0.5 d |
| Add-line dialogs (purchase + sale) | guidance + classification onto existing per-field copy | 0.5–0.75 d each |
| Receive dialog | classification + per-line guidance (largest UX risk) | 0.75–1 d |
| Return pages (purchase + sales) | field rules + guidance + existing classification | 0.5–1 d each |
| Product editor | long form: summary, hints, unit rows, checkbox channel, debounced duplicate check | 2–2.5 d |
| Manufacturer dialog · write-off | POC mirrors + silent-toggle fix | 0.5 d combined |
| **Phase 2 total (ruled list)** | | **≈ 5–6.5 d** |
| Phase 3 completion (remaining mirrors, announcers, key cleanup) | | ≈ 1.5–2 d |

Pre-adoption estimate for the same remaining work was ≈ 7–11 d (gap analysis
§5). One data point, one medium screen — the product-editor number is the
least certain.

## 6 · Conclusion

The Adoption Test **succeeded without requiring Foundation redesign**: zero
Foundation modifications, zero new components, ≈ 92 % reuse, effort roughly
halved against the audit estimate. Per the owner ruling, this satisfies the
condition for **freezing the Foundation as v1** and proceeding to **Phase 2
on the existing infrastructure** — both awaiting the owner's confirmation of
this report. Nothing is committed.
