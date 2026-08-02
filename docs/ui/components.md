# Shared Component Inventory

> Status: Placeholder — pending documentation phase. Do not implement from this file.
> **Exception:** §Validation & guidance foundation below is a live record
> (registered 2026-07-31 per STD-UX-127, Phase 1 of
> `validation-and-guidance.md`); the full inventory of the older components
> still awaits the documentation phase.

Reusable UI components shared across modules.

## Validation & guidance foundation (Phase 1, 2026-07-31)

Delivered by the owner-approved Phase 1 of the Validation & User Guidance UX
Standard (`validation-and-guidance.md` §13). All presentation pieces live in
`web/src/app/shared/ui-kit/form-field/`; the services and utilities in
`web/src/app/core/validation/`.

| Piece | Kind | Responsibility |
|---|---|---|
| `vf-form-field` | UI Kit component | The single field wrapper: label (`for`/`id`), projected control, hint → error → success slot, the three validation moments, message resolution, aria wiring (STD-UX-120) |
| `vf-validation-message` | UI Kit component | The reserved hint/error/success line under a field; `role="alert"`/`"status"`, `aria-describedby` target (STD-UX-091/092) |
| `vf-validation-summary` | UI Kit component | Long-form navigational summary: one linked entry per invalid field, click/keyboard focuses the field (STD-UX-023/076/129) |
| `SubmitGuidanceDirective` (`form[vfSubmitGuide]`) | core/validation directive | Moment 3 for every form: mark touched, emit `validSubmit` or focus first invalid; dialogs trigger it via `trigger()` (STD-UX-122) |
| `ValidationFocusService` | core/validation service | First-invalid focus, summary navigation, banner focus, the `vf-reveal-request` container hook (STD-UX-070/071/074) |
| `VTF_ERROR_REGISTRY` (ValidationRegistry) | core/validation | Every backend error code → category, default message key, retryable flag; completeness pinned by test (STD-UX-110/113) |
| `ApiErrorMapper` | core/validation service | Every API failure → classified failure (field / business / concurrency / notFound / system) with ruled contextual overrides (STD-UX-123) |
| `projectServerFieldErrors` | core/validation utility | `VTF-VAL-001` `errors` dictionary → form controls; unmatched keys returned (STD-UX-019/124) |
| `vfValidators` + `resolveValidationMessage` + `debouncedCheck` | core/validation utilities | Shared documented-rule validators, one-rule-one-sentence resolution, debounced/cancelling/cached advisory checks (STD-UX-101/102/125) |
| `vf-banner` | UI Kit component (`shared/ui-kit/banner/`) | The single operation-level message surface: tones error/success/warning on standard tokens, focusable (`tabindex="-1"`), `role="alert"` for errors / `role="status"` otherwise (STD-UX-062/071/121). Built in the Adoption Epic (C0) as §13 item 2 of the approved architecture |
| `vf-logo` | UI Kit component (`shared/ui-kit/logo/`) | **The only place the brand is rendered** (design-language §2 identity amendment, 2026-08-02). Inputs: `variant` (`full` lockup \| `mark` tile), `height` (px; width follows the artwork ratio), `tone` (`light` \| `dark` — the dark artwork has no consumer while the product is light-only, §11). Artwork is **referenced** from `/assets/branding/`, never inlined, so it stays out of the JS bundle (TD-107). Carries the product name as `alt`, preserving the accessible name the text brand had. **Identity, not an icon** — §12 forbids mixing icon families, so it never occupies an icon slot (row, button, empty state) |

**UI Kit repairs shipped with Phase 1:** `vf-select` is now a
ControlValueAccessor (touched on blur/panel-close; `aria-invalid`/
`aria-describedby` stamped on its combobox), `vf-date-input` is now a
ControlValueAccessor (blur/touched works; legacy `[value]`/`(valueChange)`
contract preserved for filter drawers), `vf-text-input`/`vf-number-input`/
`vf-textarea` cooperate with `vf-form-field` (wrapper owns label + message;
standalone behavior unchanged), `vf-button` gained `type="submit"`.

**UI Kit repairs shipped with the Adoption Epic (2026-08-01):** `vf-checkbox`
is now a ControlValueAccessor with an explicit `id`/`for` label association
and the error channel — `aria-invalid` + `aria-describedby` inputs, wrapper
cooperation with `vf-form-field` (STD-UX-090/091/093; §13 item 8, delivered
under the v1 freeze's accessibility exception; legacy `[checked]`/`(toggled)`
contract preserved for the filter drawers). This closes the two pieces the
owner's Phase-1 scope ruling had deferred (`vf-banner` above, and this error
channel — first needed by the product editor).
