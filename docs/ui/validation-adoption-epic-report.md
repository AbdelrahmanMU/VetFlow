# Validation UX Adoption Epic — Consolidated Owner Report

> **Status: Submitted for owner review — 2026-08-01.**
> Commissioned by the owner ruling of 2026-07-31 (recorded in
> `validation-and-guidance.md` §0, third addendum): adoption of the frozen
> Validation Foundation v1 across **every remaining production form, dialog,
> and workflow**, as one Epic under Continuous Capability Mode, closing with
> this one consolidated report. The nine sections below are the nine the
> ruling enumerated. **Nothing is committed — the Epic stops here and waits
> for Epic Commit Approval.**

## 0 · What the Epic did, in one paragraph

Thirteen capabilities (C0–C12). C0 built `vf-banner` — the one §13 piece the
Phase-1 scope ruling had deferred — and retrofitted the three already-migrated
screens onto it. C1–C8 migrated every remaining form and mutation surface:
sale create, the manufacturer dialog, write-off (rebuilt as the adjustment
mirror), the purchase add-line dialog, the receive dialog, the sale add-line
and commit dialogs, and both return pages — un-silencing every silent failure
path found by the audit on the way. C9 migrated the product editor — the
system's qualifying long form — including the `vf-checkbox` repair (the
freeze's accessibility exception) and the duplicate-check advisory through the
shared async-check utility. C10 swept the polite live region across all
remaining screens (19/19 routed screens now carry it). C11 synchronized the
documentation (module `ui.md` copy homes, `components.md` registration). C12
ran the full gates, a 16-screen live-browser sweep at both ruled viewports —
which found one real wording defect, fixed and re-verified — and produced this
report.

## 1 · Coverage metrics

| Surface | Count | On the Foundation |
|---|---|---|
| Routed screens | 19 | **19/19** carry the polite live region (STD-UX-092); all form screens fully migrated |
| Forms and mutation dialogs (typed reactive + `vfSubmitGuide`) | 12 | **12/12** — adjustment · write-off · category dialog · manufacturer dialog · purchase create · sale create · purchase add-line · receive · sale add-line · sale commit† · purchase return · sales return · product editor |
| `vf-form-field` instances | 41 | every field on a migrated form |
| `vf-banner` instances | 29 across 16 files | **0** locally-declared banner CSS blocks remain (was 10 — AP-10 closed) |
| Per-store `classify()` maps | **0** (was 5) | all failures pass through `ApiErrorMapper` (STD-UX-123; gap F-4 closed) |
| Silent failure paths from the audit | **0 remain** | list toggles, line removals, picker loads, duplicate-check failure — all surfaced (AP-01 closed) |
| `vf-validation-summary` | 1 | the one qualifying long form (product editor) — short single-view forms correctly carry none (STD-UX-023) |

† the commit dialog triggers the shared guidance via `trigger()`; its ruled
metadata-conditional wordings are preserved unchanged.

**Foundation freeze compliance:** Foundation files modified during the Epic:
**one** — `vf-checkbox` (CVA + explicit `id`/`for` label + `aria-invalid`/
`aria-describedby` error channel), under the freeze's **accessibility
exception**, exactly as §13 item 8 specified; legacy `[checked]`/`(toggled)`
contract preserved for the filter drawers. `vf-banner` was **built**, not
modified — §13 item 2 of the approved architecture, flagged at C0. No other
Foundation file changed.

## 2 · Before / after compliance by module

Audit verdicts from `validation-gap-analysis.md` §3 (before) against the
C12 state (after):

| Module | Surface | Before | After |
|---|---|---|---|
| Catalog | Product editor | **Violates** | **Complies** — per-rule messages, summary, projection, focus, classification, unit-row validation, checkbox a11y, surfaced lookup failures, debounced/cached advisory |
| Catalog | Product details / list | Partial (announcer/list gaps) | **Complies** (announcer added; list already had one) |
| Categories | Dialog + list toggles | Partial | **Complies** — maxLength wording, focus, toggle failures surfaced (C2) |
| Manufacturers | Dialog + list toggles | Partial | **Complies** — mirror of categories (C2) |
| Purchasing | Purchase create | Partial | **Complies** (Adoption Test screen) |
| Purchasing | Add-line dialog | Partial | **Complies** — three moments, classification, units-load retry, removal un-silenced (C4) |
| Purchasing | Receive dialog | **Violates** (audit's largest UX risk) | **Complies** — full per-code classification, per-line expiry `FormRecord`, first-offending-line focus, retry relabel (C5) |
| Purchasing | Purchase return | **Violates** | **Complies** — typed reactive, per-line rules, STD-UX-042 sequence statement (C7) |
| Sales | Sale create | Partial | **Complies** (C1) |
| Sales | Add-line + commit dialogs | Partial | **Complies** — incl. profile-armed `wholeNumber`, mapper-based commit with banner focus (C6) |
| Sales | Sales return | **Violates** | **Complies** (C8) |
| Inventory | Adjustment | Complies (POC) | **Complies** — plus `vf-banner` retrofit, picker retry, announcer |
| Inventory | Write-off | Partial | **Complies** — rebuilt as the adjustment mirror; owns its six keys (AP-16 closed) (C3) |
| Inventory | Batch viewer / expiry / history | Partial (no announcer) | **Complies** (C10) |

## 3 · Remaining exceptions

None entered the standard's Exception Register. Open items that **look** like
exceptions but are ruled or pending elsewhere:

1. **AMD-1..6 unruled** — the mapper's status-based 404 branch remains, as the
   standard itself documents (STD-UX-030's stated exception until AMD-1).
2. **DEC-INV-022** (insufficient-stock wording) — still the owner's; the
   registry maps the code to its current approved sentence.
3. **STD-UX-065 (section ⚠ + count)** — today no screen has tabs/accordions
   (the standard's §9 note); the product editor's plain always-visible
   sections satisfy `ui.md`'s «تُبرَز الأقسام الناقصة» through the highlighted
   fields inside them plus the summary map, and the units section joins the
   summary as a linked entry. A dedicated section-rail indicator was **not**
   built; if the owner reads STD-UX-065 as requiring one for plain sections,
   it is a small follow-up.
4. **TD-007** — the return pages keep their raw `<table>` per the recorded
   ruling (no `<vf-table>` before or during the Pilot).

## 4 · Remaining technical debt

| Item | Where | Proposed home |
|---|---|---|
| `nonNegative` validator duplicated ×3 (add-purchase-line, both return pages) | screen-local | `vfValidators` at the next Foundation window (freeze forbids it now) |
| `validation.*` shared strings + `errors.<code>` defaults still awaiting the owner's copy review (flagged since Phase 1) | `ar.ts` | owner review at this report |
| Write-off borrows `adjustment.field.batchLoading/batchPlaceholder/actor*` label keys (labels only — its six error keys are its own) | `ar.ts` | key-ownership cleanup with the next copy pass |
| TD-107: initial bundle **583.98 kB** (+4.16 kB over the Epic: `vf-banner`, checkbox CVA, summary wiring) | build budget | accepted-debt horizon unchanged (through the Pilot) |
| GLOSSARY sync debt (~26 terms + the standard's §6 hint/success/summary vocabulary) | `docs/shared/GLOSSARY.md` | unchanged — the Epic's new sentences minted **no new domain terms** (verified against the glossary; «طبيعة المنتج», «الشركة المصنعة», «تصنيف» all pre-exist) |

## 5 · Reuse metrics

| Metric | Value |
|---|---|
| Foundation pieces consumed unmodified | all 10 — `vf-form-field` · `vf-validation-message` · `vf-validation-summary` · `SubmitGuidanceDirective` · `ValidationFocusService` · `VTF_ERROR_REGISTRY` · `ApiErrorMapper` · `projectServerFieldErrors` · `vfValidators` · `debouncedCheck` |
| New validation components written during adoption | **1** — `vf-banner` (approved §13 architecture, deferred from Phase 1; not screen-local code) |
| Screens/dialogs per shared piece | `vfSubmitGuide` 12 · `ApiErrorMapper` 19 consumers · `projectServerFieldErrors` 5 · `vfValidators` 12 files · `debouncedCheck` 1 (its first consumer — the duplicate check it was built for) |
| Copy-pasted infrastructure remaining | **0** `classify()` maps · **0** local banner CSS |
| Epic change set (whole uncommitted set: Phase 1 + Adoption Test + Epic) | 67 modified + 8 new files · **+4 282 / −1 811** lines |
| New Arabic strings (whole Epic, all flagged in `ar.ts` and landed in their `ui.md` homes) | **15** — six `writeOff.error.*` (one new wording) · 3 `pickers.*` (C3/C4) + 4 editor pickers · both `*.draftState` · `editor.error.save` · `editor.duplicateCheck.failed` · 3 `editor.units.error.*` · `validation.nonNegative` — counted by distinct wording groups; dead keys removed: `purchaseCreate.required` · `saleCreate.required` · `editor.required` · `editor.error` |

## 6 · Validation-UX compliance percentage

Measured against the gap analysis's own audit table (23 audited surfaces,
AC-UX-01..19 dimensions):

- **Before the initiative:** 5 comply · 12 partial · 6 violate → **22 %**.
- **After the Epic:** **23 / 23 audited surfaces comply** with every
  applicable AC-UX criterion → **100 %**, with the §3 items above as the
  recorded qualifications (AMD-dependent behaviors and STD-UX-065's
  interpretation).

## 7 · Accessibility compliance summary

- **Labels:** every field on a migrated form has exactly one programmatic
  label via `vf-form-field`'s `for`/`id`; `vf-checkbox` now associates
  explicitly (`id`/`for`) — no unlabeled checkbox remains (STD-UX-093).
- **State:** `aria-invalid` + `aria-describedby` stamped by the wrapper on
  text, number, textarea, date, select (combobox), and now checkboxes
  (STD-UX-090/091) — verified live in the browser sweep.
- **Announcements:** errors interrupt via `role="alert"` (banner + field
  messages); success and warnings are `role="status"`; **19/19 routed screens**
  carry the standing polite region announcing load outcomes, saved facts whose
  banners are status insertions, and in-flight saves (STD-UX-092). Rejections
  are not double-announced: the alert surface owns them.
- **Focus:** first-invalid focus with scroll headroom on every guided form;
  operation banners and the advisory notice receive focus themselves; summary
  links navigate; dialogs keep focus inside and per-line errors focus the
  first offending line (STD-UX-070/071/074/076/084) — all exercised live.
- **Keyboard:** the whole error journey (submit → summary → field → retry) is
  operable without a pointer; the focus ring is never removed.

## 8 · Browser verification results

Method: headless Chrome over CDP against the **real stack** (web :4200 → API
:5080 → PostgreSQL :5434), matching the Phase-1/Adoption-Test method. **16
screens × 2 viewports (1440×900 and 390×844), 201 recorded checks — all
passing at close.**

- Base checks per screen and viewport: `dir=rtl`/`lang=ar` · zero horizontal
  overflow · polite region present · zero console errors — **all pass on all
  16 screens at both widths**.
- Interaction flows (at 1440): moment-3 guidance with canonical or ruled
  contextual wordings, first-invalid focus, and aria chains on purchase
  create, sale create, adjustment, write-off, and the product editor; dialog
  rejected-submit stays open with inline per-field errors on the category,
  manufacturer, purchase add-line, and sale add-line dialogs; the receive
  dialog flags a missing expiry per line and focuses the offending line; the
  return pages error inline on a negative quantity at blur and banner the
  no-lines rejection focused with `role="alert"`; the product editor clears an
  error on the fixing keystroke, shows the success mark, renders the linked
  summary (units section included), and its summary links move focus; list
  regions announce «1–25 من 256»-style ranges after load.
- **One real defect found, fixed, re-verified:** both return pages rendered
  the strictly-positive sentence («أكبر من صفر») for the **non-negative** rule
  where zero legitimately means "not returning this line" — replaced with the
  new shared `validation.nonNegative` sentence («القيمة يجب أن تكون صفرًا أو
  أكثر.», flagged for review); the corrected sentence verified live.
- Seven first-pass check failures were **checker artifacts** (the script
  asserted the generic canonical sentence where the ruled *contextual*
  wordings correctly render); corrected and re-run green — the application
  behavior was right each time.
- **Dev-data side effect (pre-pilot):** one draft purchase invoice
  **PUR-000017 «مورد تحقق الإيبك»** with one line, created via the API to give
  the receive dialog a real per-line expiry case. No stock moved; every
  browser flow stopped before a committing action.

**Gates at close:** frontend **291/291** (was 283 at C8; 266 pre-Epic) ·
ESLint clean · Stylelint clean · `ng build` exit 0 (TD-107 warning only) ·
backend untouched and green — build 0/0 · `dotnet format` clean · Domain
**163** · Architecture **134** · Integration **236**.

## 9 · Final UX audit against the approved standard

Section-by-section against `validation-and-guidance.md`:

| § | Verdict | Note |
|---|---|---|
| 1 Philosophy / 2 Categories | **Conforms** | every failure classified into exactly one category by the registry |
| 3 Field validation (moments, per-rule messages, projection, summary) | **Conforms** | moments owned by `vf-form-field` + guidance; summary on the one qualifying long form |
| 4 Business errors | **Conforms** | operation-adjacent banners, metadata naming, retry on B1, registry-complete codes (pinned by test) |
| 5 System errors | **Conforms** | generic copy with in-place retry; failed loads never degrade to empty; 404s distinct; STD-UX-042 sequence statements on the returns |
| 6 Style guide | **Conforms** | all new copy follows the templates; every new wording flagged and landed in its `ui.md` home; DEC-INV-022 remains the one unfinishable template |
| 7 Visual | **Conforms** | three signals per invalid field; one banner component; tokens only |
| 8 Focus | **Conforms** | verified live at C12 |
| 9 Dialogs/panes | **Conforms** | dialogs stay open with inside errors; per-line editors follow §3; no tabbed screen exists yet (forward-binding rules untested by necessity) |
| 10 Accessibility | **Conforms** | §7 summary above; owner-ruled Mandatory/Error respected |
| 11 Performance | **Conforms** | whole-form work only at moment 3; the one server-backed advisory is debounced/cancelling/cached; submit is the only mutation call |
| 12 Catalog strategy | **Conforms** | one registry; `errors.<code>` + ruled contextual overrides; no `detail` rendered; missing-key fallback |
| 13 Architecture | **Conforms** | all pieces in use; STD-UX-120..129 hold on every migrated surface |
| 17 Anti-patterns | **None observed** on migrated surfaces — AP-01/02/03/10/16 each explicitly closed during the Epic |

**Residuals for the owner, consolidated:** the new Arabic wording groups of §5
(review) · AMD-1..6 (§3.1) · DEC-INV-022 (§3.2) · the STD-UX-065 reading
(§3.3) · the §4 debt table.

---

*Prepared at the close of C12. The Epic's seven stop conditions are met:
implementation complete · all tests green · architecture tests green ·
browser verification passed · performance unaffected (no new queries, no
per-keystroke work — §11 verified) · self review complete · this report.
**Awaiting Epic Commit Approval — nothing committed, nothing pushed.***
