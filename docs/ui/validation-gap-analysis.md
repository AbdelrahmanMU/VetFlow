# Validation & User Guidance — Gap Analysis

> **Status: Approved with the standard — owner, 2026-07-31; revised the same
> day to synchronize with the twelve owner rulings incorporated into
> `validation-and-guidance.md`.** Findings and estimates are evidence for the
> owner's scheduling; the implementation *order* is owner-ruled (Foundation →
> Shared infrastructure → Module adoption), the calendar is not.
> Audit date: 2026-07-31, against the working tree at `4768c6f`
> (= `pilot-2026-07-31`).
> Method: full read-only code audit of `web/src` (every routed screen, every
> dialog, the UI Kit, i18n) and `src/` (validation pipeline, middleware, Error
> Catalog, resources), plus the documentation surface (`docs/ui/`, standards,
> module `ui.md` files). Evidence cites `path:line` in the audited tree.

**Verdict scale** — per screen, against the standard's AC-UX-01..19:
**Complies** (no material gap) · **Partial** (core behavior present, named
gaps) · **Violates** (a mandatory section of the standard is absent).

**Effect of the 2026-07-31 rulings on this audit:** no verdict changed. The
rulings added requirements that exist **nowhere** yet (hints, success states,
the Validation Summary, debounced async checks — recorded as gap F-10), which
deepens the form-screen gaps without flipping any compliant screen: the five
compliant screens are read-only and unaffected.

---

## 1 · What already complies (keep, and build on)

The standard was written *from* these — they are its reference
implementations, not accidents:

1. **The backend contract is strong and uniform.** One RFC 9457 shape for
   every non-2xx, produced in exactly one place
   (`src/VetFlow.Api/Middleware/ProblemDetailsWriter.cs`); 33 `VTF` codes in
   perfect 1:1 across constants ↔ `ErrorCatalog` ↔ ar/en resx; zero hardcoded
   validator messages; no exception text can leak (pinned by
   `ExceptionTranslationTests`). §14's affirmations hold today.
2. **`errorCode`-only branching where classification exists** (STD-FE-037):
   no frontend code parses message text.
3. **The four data-view states with retry** on all ten read-only screens —
   uniform `vf-empty-state` error surface with the `X.error.title/body/retry`
   triplet (STD-UX-041 substantially met for page loads).
4. **Distinct not-found states** on details screens («فاتورة الشراء غير
   موجودة» pattern) — STD-UX-043 met where it matters most.
5. **The commit-sale dialog** (`commit-sale-dialog.component.ts`) — the
   reference implementation of §4: code classification, metadata
   interpolation naming products, a metadata-less fallback, and the primary
   button relabeling to «إعادة المحاولة» on concurrency (STD-UX-033/034).
6. **The adjustment screen's per-field distinct messages**
   (`adjustment.error.productRequired/batchRequired/quantityPositive/reasonRequired`)
   — the only screen already meeting STD-UX-017.
7. **The category/manufacturer dialogs' server-error projection** — the only
   two instances of STD-UX-019 (inline `errors.name` rendering, cleared on
   edit).
8. **Pessimistic saves everywhere** (STD-FE-036), confirmation dialogs on
   the two irreversible operations, and the state-guarantee sentence («لم
   يُحفظ أيّ تغيير») already present in the ruled concurrency/rejection copy.
9. **`role="alert"` on field-error spans and banners; `role="status"` on
   success surfaces** — the announcement primitive exists (its wiring is the
   gap, §2 F-8).
10. **Digit normalization on every number input** (`digits.ts`) — STD-UX-002's
    prevention arm, in place.
11. **No toast infrastructure exists at all** — owner ruling 8 (STD-UX-044)
    is trivially satisfied today and must simply stay that way.

## 2 · Cross-cutting gaps (the Foundation / Shared-infrastructure work)

These violate the standard on **every** form screen at once; no per-screen
fix is meaningful before them. Rulings 1, 2, 4 and 11 are all blocked on this
layer.

| # | Gap | Standard | Evidence |
|---|---|---|---|
| F-1 | **No focus or scroll management anywhere.** Zero `.focus()`/`scrollIntoView` calls in all of `web/src/app/features`. §8 is entirely unimplemented. | STD-UX-070/071 · AC-UX-09 | grep: 0 hits |
| F-2 | **No `aria-describedby` anywhere**; error spans have no `id`. Field↔error association does not exist app-wide. | STD-UX-091 · AC-UX-11 | grep: 0 hits |
| F-3 | **No `vf-form-field`** — five input components duplicate label/error markup; `vf-select` lacks `aria-invalid`; `vf-date-input` is not a ControlValueAccessor (never fires touched → blur validation impossible); `vf-checkbox` has no label association and no error channel. | STD-UX-090/093 · §13 items 1, 8 | `shared/ui-kit/*` |
| F-4 | **No central code→message registry** — five byte-identical `classify()` copies (`adjustment.store.ts:13` · `write-off.store.ts:9` · `purchase-return.store.ts:8` · `sales-return.store.ts:8` · `sale-lines.store.ts:17`) plus two inline heuristics; two stores classify nothing at all. | STD-UX-110/036 | frontend audit §2 |
| F-5 | **Ten copy-pasted local `.banner` CSS blocks**, two class-name conventions (`.banner` vs `.banner banner--error`), and a fallback color literal (`#fbeae8`) that disagrees with the global token value (`#fef2f2`). No shared banner component. | STD-UX-062 · AC-UX-12 | frontend audit §4 |
| F-6 | **Three forms regimes and four error-timing regimes** across 12 mutation surfaces — blur-or-submit vs submit-only vs server-error-priority vs none. None of the four implements the ruled three-moment model (no screen has a typing moment with clearing, none has success states). | STD-UX-007 · §3 | frontend audit §4 |
| F-7 | **No screen binds submit behavior to a guided flow** — all use `[disabled]="saving()"` variants and block inside the handler with no focus/summary guidance. (The enabled-submit half of STD-UX-016 is coincidentally met.) | STD-UX-012/070 | frontend audit §4 |
| F-8 | **The `aria-live` announcer exists on only 6 of 20 screens** (the main lists), and on none of the form screens where outcomes most need announcing. | STD-UX-092 | frontend audit §4 |
| F-9 | **`VTF-INV-061` has four different Arabic wordings; `VTF-INV-068`'s identical sentence is duplicated across three keys**; write-off borrows `adjustment.*` keys cross-context. | STD-UX-054/111 · AP-16 | `ar.ts:538-580,641` |
| F-10 | **The Progressive-Guidance surface does not exist anywhere** (new with the owner rulings): no hint slot or hint copy on any field · no success-after-correction state · no Validation Summary component · no debounce/cancel/cache utilities for async checks (the one async advisory check — the duplicate-product call — fires un-debounced from the editor and its failure silently proceeds). Every form screen inherits these gaps. | STD-UX-009/013/014/023 · §11 · AC-UX-05/17/19 | rulings 1, 2, 4, 11 vs frontend audit |

## 3 · Per-screen audit

### Forms and mutation surfaces

| Screen / surface | Verdict | What complies | Gaps (AC-UX ref) |
|---|---|---|---|
| **Product editor** (create/edit) `catalog/product-editor` | **Violates** | Reactive typed form; blur-or-submit timing; cross-field rules exist; 404 handled | One generic message for every field and every cross-field rule (`editor.required` — AC-05); the units banner carries 3 distinct rules in one sentence **and is a field-error-only-in-banner case (STD-UX-020)**; conversion factor/barcode/price rows unvalidated; **the failure banner says "راجع الحقول المميّزة" but no server error is ever projected — nothing gets highlighted** (AP-03, AC-02); no VTF classification (AC-07); no focus (AC-09); checkboxes unlabeled/unerrorable (AC-11); unit-row editor uses submit-only inside a blur-or-submit parent; duplicate-check failure silently proceeds to create; **the qualifying long form of the system — no Validation Summary, no hints, no success states (AC-05/17)** |
| **Purchase create** `purchasing/purchase-create` | **Partial** | Reactive; blur-or-submit; banner adjacent | Generic single message (AC-05); `invoiceDate` cannot error on blur (date-input not CVA — internal timing inconsistency); no VTF mapping — every failure one sentence (AC-07); no focus (AC-09); no announcer (AC-11) |
| **Sale create** `sales/sale-create` | **Partial** | Mirror of purchase create | Same gaps as purchase create |
| **Category dialog** (+ list toggles) `categories` | **Partial** | Server `errors.name` projected inline, cleared on edit (the AC-05 server half — best in app); dialog stays open; list announcer present | `maxLength(100)` violation renders the *required* copy (AC-02/05); submit-only timing; **activate/deactivate failure is completely silent** (AC-01, AP-01); duplicate detection rides the fragile `VTF-VAL-001`+`errors.name` heuristic (§14 AMD-2); no focus (AC-09) |
| **Manufacturer dialog** (+ toggles) `manufacturers` | **Partial** | Byte-for-byte mirror | Same gaps, incl. the silent toggle |
| **Purchase details — add-line dialog** | **Partial** | Per-field distinct messages (AC-05 client half); dialog stays open | Submit-only timing; banner **ignores its `serverError` input and renders a hardcoded generic key**; no VTF classification (AC-07); no focus; selects lack `aria-invalid` |
| **Purchase details — receive dialog** | **Violates** | Per-line expiry requirement checked; confirm dialog exists | **Irreversible inventory operation with zero code classification** — insufficient data, already-received, concurrency all collapse to one sentence (AC-07/08, AP-15); submit-only; no focus to offending line (AC-09/10); date inputs never fire blur |
| **Purchase details — line removal** | **Violates** | — | **Failed DELETE shows nothing** (`purchase-line-items.component.ts:295` — AC-01, AP-01) |
| **Purchase return** `purchases/:id/returns/new` | **Violates** | Banner classification is good (8 mapped failures incl. client-side `noLines`); success banner + state guarantee; `[disabled]` gated on content | **No field validation of any kind** — quantities and date have no rules, no inline errors (AC-05); `[min]` on the number input is inert; 3-step non-atomic save can strand an orphan draft with no statement of partial state (STD-UX-042); no focus (AC-09); no announcer |
| **Sales return** `sales/:id/returns/new` | **Violates** | Mirror; 10 mapped failures | Same gaps as purchase return |
| **Sale details — add-line dialog** | **Partial** | Per-field messages incl. splittability (AC-05 client half); banner renders its input correctly | Submit-only; no VTF classification on add (AC-07); no focus; selects lack `aria-invalid` |
| **Sale details — commit dialog** | **Partial** | **Reference implementation of §4** — classification, metadata naming products, retry relabel (AC-07/08) | No focus to the banner (AC-09); no `aria-live` (AC-11); DEC-INV-022 wording still open |
| **Sale details — line removal** | **Violates** | — | **Failed DELETE shows nothing** (`sale-line-items.component.ts:298` — AC-01) |
| **Inventory adjustment** `inventory/adjustments/new` | **Partial** | **Only screen with per-field distinct messages** (AC-05 messages half); full VTF mapping; success banner + link; state-guarantee copy | Submit-only timing; **rejection banner does not clear when the quantity — the input the rule depends on — changes** (STD-UX-035, AP-14); **product/batch picker load failures degrade to an empty list** (`adjustment.store.ts:42,58` — AC-13, AP-01); selects lack `aria-invalid`; no focus; no announcer |
| **Inventory write-off** `inventory/write-offs/new` | **Partial** | Mirror of adjustment | Same gaps + cross-module key borrowing (`adjustment.error.conflict/notFound` — AP-16) |

### Read-only screens

| Screen | Verdict | Notes |
|---|---|---|
| Product list | **Complies** | Four states, retry, three empty-state branches, announcer |
| Purchase list · Sales list · Inventory list | **Complies** | Same pattern |
| Product details · Purchase details (view) · Sale details (view) | **Partial** | Four states + distinct 404 ✔; **no announcer** (AC-11) |
| Batch viewer · Expiry monitoring · Movement history | **Partial** | Four states + retry ✔; no announcer; batch-viewer 404 handled |
| Filter drawers (6) | **Complies** (thin) | Date pairs constrain via native min/max only; no rules currently documented for them — nothing to validate until one is |

**Tally: 5 Comply · 6 Violate · 12 Partial** (+ 6 thin-complying drawers).
No form screen fully complies — expected, since the §8/§10/§11/§12/§13
infrastructure does not exist yet (§2), and the ruled Progressive-Guidance
surface (F-10) exists nowhere.

## 4 · Backend deviations (details in the standard's §14 AMD table — none ruled yet)

| # | Finding | Severity |
|---|---|---|
| B-1 | All 29 entity-404s carry no `errorCode` and route-flavored copy; frontend forced into status-code branching | High (contract) — AMD-1 |
| B-2 | 12 registered pipeline handlers have no validator; the C5 defect class has no structural guard | High (defect class) — AMD-4 |
| B-3 | `ErrorCatalog.Get`/`ErrorMessages.Get` fail open; no coverage assertion constants ⊆ catalog ⊆ resx | Medium — AMD-5 |
| B-4 | Duplicate category/manufacturer name modeled as field validation, not a coded business rule | Medium — AMD-2 |
| B-5 | Business-rule-as-404 overloads on `returnable-lines` (documented, deliberate — needs a ruling, not a fix) | Medium — AMD-6 |
| B-6 | Nested field keys half-camelCased (`units[0].QuantityInNextUnit`) | Low — AMD-3 |
| B-7 | `title` always English; `type` URIs unresolvable; `ErrorCatalog.All` has no consumer | Low — recorded only |
| B-8 | Exception middleware sits outside CORS (moot same-origin; real on the dev proxy path) | Low — recorded only |
| B-9 | BR-number ↔ code-number alignment claimed by the `*ErrorCodes` docs does not hold for ≥ 6 codes | Low (docs) — recorded only |

## 5 · Estimated implementation effort — in the owner-ruled order

**Owner ruling 12 (binding): Foundation → Shared reusable infrastructure →
Module adoption.** Ranges are focused dev-days including tests and doc sync.
Module packages are independent of each other and can land in any
owner-chosen sequence within Phase 3.

| Phase / package | Contents | Estimate | Depends on |
|---|---|---|---|
| **Phase 1 — Foundation** | UI Kit repairs: `vf-date-input` CVA · `vf-select` `aria-invalid` · `vf-checkbox` label + error channel · success/hint design tokens (extension request to the design system) · a11y primitives (`id`/`aria-describedby` wiring groundwork) | **1.5 – 2 d** | Sync confirmed |
| **Phase 2 — Shared infrastructure** | `vf-form-field` (hint → error → success slot, three-moment timing) · `vf-banner` · `vf-validation-summary` · submit-guidance directive (focus/scroll/auto-open) · `ApiErrorClassifier` + `VTF_ERROR_REGISTRY` · server-error projection · shared validators + debounced/cancelling/cached async-check utility · announcer generalization · message-catalog re-keying (§12 naming, incl. `hint.*`) | **4 – 5.5 d** | Phase 1 |
| **Phase 3 — Catalog** | Editor per-rule messages · Validation Summary adoption (the system's long form) · hints + success states · server projection · focus · VTF mapping · unit-row validation · checkbox a11y · duplicate-flow failure surface + debounced advisory check | **2 – 3 d** | Phase 2 |
| **Phase 3 — Categories + Manufacturers** | maxLength message · silent-toggle fix · three-moment timing · dedicated duplicate handling (after AMD-2, else keep projected field error) | **0.5 – 1 d** | Phase 2 |
| **Phase 3 — Purchasing** | Create-form gaps · add-line dialog · **receive-dialog classification** (largest single UX risk) · silent line removal · return-page field validation + sequence-state reporting | **2 – 3 d** | Phase 2 |
| **Phase 3 — Sales** | Create-form gaps · add-line dialog · commit-dialog focus/announce · silent line removal · return-page field validation | **1.5 – 2.5 d** | Phase 2 |
| **Phase 3 — Inventory** | Adjustment/write-off three-moment timing · banner-clear-on-edit · picker load-error states · key ownership cleanup | **1 – 1.5 d** | Phase 2 |
| **Backend amendments** | AMD-1..6 as ruled (AMD-4/5 are test-only; AMD-1/2 touch contract + frontend consumers) | **1 – 2 d** | Owner rulings on §14 |
| **Docs sync** | Module `ui.md` copy tables for new/changed messages and hints · GLOSSARY debt payment for error vocabulary · `components.md` registration of the new Kit pieces | **0.5 – 1 d** | With each package |
| **Total** | | **≈ 14 – 21 dev-days** | |

**Early-win note (within the ruled order):** once Phase 2's classifier and
banner exist, the receive-dialog classification and the four silent-failure
fixes are roughly one further day and remove the worst per-screen risks.

**Pilot interaction (still the owner's call — standard, Open item 3):** the
ruling of 2026-07-31 bars new features during the Pilot unless required to
keep the system operational. This work is corrective UX, not features; the
implementation *order* is ruled, but **when Phase 1 starts relative to
UAT/Pilot remains the owner's scheduling decision.**
