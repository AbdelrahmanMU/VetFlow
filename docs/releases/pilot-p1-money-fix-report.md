# Pilot P1 — Money Integrity Fix (F1) — Report

> Status: Delivered · Fast Delivery Mode · 2026-07-16
> Scope: **ONLY F1** — Arabic-Indic digit normalization for monetary input.
> Nothing else was touched.

---

## 0. Scope note — how the sprint brief maps onto this repository

The brief referenced `docs/releases/pilot-final-acceptance-report.md` and stated
"authentication already normalizes Arabic-Indic digits correctly." **Neither
exists in this repository:** there is no `docs/releases/` acceptance report, no
authentication module, and — before this change — **no digit-normalization
implementation of any kind** (verified: zero matches for any Arabic-Indic→Latin
normalizer across all `.cs`/`.ts`). The brief describes a later snapshot of the
product than the code here.

Per the owner ruling for this sprint ("**Create the canonical one**"), F1 was
delivered by **creating the single canonical digit normalizer** and routing the
money-input path through it — rather than reusing a nonexistent auth normalizer.
The RULE ("do not introduce a second normalization algorithm") is honored by
construction: there is now exactly **one** algorithm, and any future path
(authentication included) must call it.

---

## 1. Root cause

Monetary values are entered through one component — `VfNumberInputComponent`
(`web/src/app/shared/ui-kit/input/vf-number-input.component.ts`), the price field
in the unit-profile editor (`BR-CAT-016/024/025`). It had **two** compounding
defects:

1. **`type="number"` silently discards Arabic-Indic digits.** The HTML value
   sanitization algorithm blanks `input.value` for anything that is not a valid
   ASCII floating-point number. Arabic-Indic (`٥٠٠`), Persian (`۵۰۰`), and mixed
   (`5٠٠`) strings are not valid floats, so **the digits were gone before any
   JavaScript ran** — the field silently became empty.

2. **`Number()` does not parse non-Latin digits.** Even where a raw string did
   reach the handler, `Number('٥٠٠')` / `Number('5٠٠')` is `NaN`, which the old
   `onInput` mapped to `null`. A user entering `٥٠٠` for a price got a silently
   empty amount — the money-integrity hazard.

There was no shared normalizer to reuse, so the correct fix is to **introduce the
canonical one** and make the money path the first caller.

---

## 2. The fix

**New — the single canonical algorithm**
`web/src/app/core/i18n/digits.ts` → `normalizeDigits(input: string): string`

- Maps Arabic-Indic `U+0660–U+0669` and Extended/Persian `U+06F0–U+06F9` digits
  to Latin `0–9`.
- Maps the Arabic decimal separator `٫` (`U+066B`) to the ASCII point `.`.
- Drops the Arabic thousands separator `٬` (`U+066C`, grouping only).
- Strips bidi / zero-width format marks (LRM, RLM, ALM, ZWSP, ZWNJ, ZWJ,
  BOM) that ride along on copy-paste from RTL documents and otherwise turn a
  valid amount into `NaN`.
- Leaves whitespace for `Number()` to trim at the boundary.
- **Invariant:** valid number in any supported script → the exact same Latin
  number; anything else → a string `Number()` rejects (→ `null`). It never
  produces a *different, plausible* number.

**Changed — route the money input through it**
`web/src/app/shared/ui-kit/input/vf-number-input.component.ts`

- Switched the field from `type="number"` to `type="text" inputmode="decimal"`
  so Arabic-Indic characters survive to be normalized (keeps the decimal keypad
  on mobile). This is **necessary**, not cosmetic: normalizing inside `onInput`
  on a `type="number"` field is a no-op because the value was already blanked.
- `onInput` now funnels the raw value through `normalizeDigits` before parsing,
  and reflects the canonical text back into the field (`٥٠٠` shows as `500`).
- Display text is held separately from the parsed number so an in-progress
  decimal (`123.`) stays typeable instead of being eaten by re-parsing.
- `min`/`step` inputs are retained for caller-API stability (they are no-ops on
  a text input; no call site changed). **No negative-value validation was added
  — that is not F1.**

### Scope honesty

- **Backend / "money parsing" / "payment recording": N/A.** Product prices reach
  the API as JSON numbers, which are ASCII by the JSON spec; the product-list
  query has **no** price filter (verified — no string→decimal parse path), and
  there is no payment module in this repository. No backend path parses
  Arabic-Indic text, so none was changed.
- `VfNumberInputComponent` is shared, so the fix also normalizes the two
  non-money numeric fields (unit conversion factor, open-expiration days). That
  is correct and consistent behavior ("digits behave consistently everywhere"),
  not scope creep.
- **Out of scope (declared, not silently broken):** a Latin grouping comma
  (`1,234.50`) is not parsed — it fails loudly to `null`, never a wrong amount.

---

## 3. Files changed

| File | Change |
|---|---|
| `web/src/app/core/i18n/digits.ts` | **New.** Canonical `normalizeDigits`. |
| `web/src/app/shared/ui-kit/input/vf-number-input.component.ts` | `type="text" inputmode="decimal"`; `onInput` routes through `normalizeDigits`; display text held separately for decimal typing. |
| `web/src/app/core/i18n/digits.spec.ts` | **New.** 11 normalizer regression tests. |
| `web/src/app/shared/ui-kit/input/vf-number-input.component.spec.ts` | **New.** 10 input-boundary regression tests. |

No other production file was modified. No commit/push was made (not requested).

---

## 4. Regression coverage (+21 tests)

**Normalizer contract** (`digits.spec.ts`, 11):
the four acceptance examples (`٥٠٠→500`, `5٠٠→500`, `١2٣٤→1234`, `12٣.٥٠→123.50`),
Persian digits, Arabic decimal separator, Arabic thousands separator, bidi /
zero-width copy-paste marks, whitespace, invalid input (incl. the `1,234.50`
loud-failure case and the "never a plausible wrong number" invariant), and the
no-op-on-canonical case.

**Input boundary** (`vf-number-input.component.spec.ts`, 10):
asserts the field is `type="text"`/`inputmode="decimal"`; drives the four
acceptance examples through a real `input` event and asserts the value reported
to the form; whitespace + RTL-mark paste; canonical digits reflected into the
field; in-progress decimal stays typeable; invalid input → `null`; form-written
value (edit prefill) round-trips.

---

## 5. Verification

| Gate | Command | Result |
|---|---|---|
| TypeScript / Build | `npm run build` | ✅ Bundle generated, 0 errors |
| Lint (ESLint) | `npm run lint` | ✅ All files pass |
| Lint (Stylelint) | `npm run lint:css` | ✅ Clean |
| Unit | `npm test -- --watch=false` | ✅ **50 passed** (11 files); was 29 → +21 |
| Integration (backend) | n/a for F1 | No backend path touched (see §2) |
| E2E | — | **No E2E harness exists** in this repo (no `e2e` script); prior slices were verified via manual headless-Chrome against the running stack. |

**On the jsdom caveat (and why the unit tests are now representative):** the
usual "jsdom doesn't sanitize number inputs" warning applied to the *old*
`type="number"` field. Moving to `type="text"` **removes** that divergence — a
text input's `.value` returns exactly what was assigned in both jsdom and a real
browser — so the component test exercises the identical production code path a
browser would. The one environment-dependent variable in the root cause has been
eliminated, not merely mocked around. A live headless-Chrome paste into the
running price field remains the gold-standard final check and is recommended as
the owner's acceptance step (it needs the full stack + a seeded catalog).

---

## 6. Final verdict

**F1 resolved.** Monetary input now normalizes Arabic-Indic, Persian, and mixed
digits through a single canonical algorithm; an incorrect monetary value can no
longer be produced by mixed-digit entry — valid input yields the exact amount,
invalid input yields an empty field (visible and recoverable), never a wrong
number. All quality gates are green. Only F1 was implemented; nothing unrelated
was touched.

**Follow-ups (not part of F1, do not lose):**
- `STATUS.md` was intentionally **not** updated (the brief said implement only
  F1; STATUS is updated via `/close-session` at the owner's direction).
- When authentication is built, it must call `normalizeDigits` (the canonical
  path) rather than adding a second normalizer.
- If formatted paste (`1,234.50` with grouping) should be accepted, that is a
  separate, tested enhancement — currently it fails safely to empty.

---

## 7. Owner-review hardening pass (2026-07-16)

Performed before committing, at the owner's direction. No code changes were
required; findings below are confirmations.

1. **Global reuse — single implementation confirmed.** A full frontend search
   (`Number(`, `parseInt`, `parseFloat`, `fromCharCode`, `charCodeAt`,
   `codePointAt`, Arabic/Persian digit ranges) found numeric parsing in **only**
   `digits.ts`, its spec, and `vf-number-input.component.ts`. No duplicate
   Arabic/Persian conversion logic exists anywhere; there was nothing to replace.
2. **Money safety — invariant holds by construction.** `normalizeDigits`
   substitutes digit code points 1:1 (Arabic-Indic and Persian → the same Latin
   value), maps only the Arabic decimal separator to its ASCII equivalent, and
   removes only value-less characters (grouping / bidi / zero-width marks). It
   can never alter the numeric value of a valid number, and any residual
   non-numeric character makes `Number()` return `NaN` → `null`. All ten review
   cases (Arabic, Persian, mixed, decimal separator, thousands separator,
   leading/trailing spaces, zero-width chars, RTL marks, empty, invalid) are
   covered by regression tests.
3. **Future reuse — generic.** Signature is `string → string` with no domain
   coupling; auth, search, quantity, price, discount, payment, inventory, and
   batch inputs can all reuse it unchanged.
4. **Architecture — correct layer.** `digits.ts` imports nothing; it holds no
   business/catalog/money knowledge and no dependency on Angular forms or any
   feature module. It lives in `core/i18n/` beside `format.service.ts` as a
   generic UI/infrastructure utility, not a business service.
5. **`type="number"` → `type="text"` regressions — none.** Verified:
   mobile decimal keypad (via `inputmode="decimal"`), accessibility (labels /
   aria intact), paste (now fixed), copy, autofill (N/A), validation (reactive
   model unchanged; backend `BR-CAT-025` still enforces price ≥ 0), and tab
   order. Two intended, non-defect behavior changes: native spinner arrows are
   gone, and non-numeric keystrokes now resolve to `null` instead of being
   blocked — never to a wrong number.
