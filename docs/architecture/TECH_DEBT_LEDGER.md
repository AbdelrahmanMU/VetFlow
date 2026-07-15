# Technical Debt Ledger

> Status: Living record. The single register of accepted debt and deferred work.
> Engineering doc (English per ADR-0002). Each entry is evidence, not a mandate —
> scheduling is the owner's call. IDs `TD-NNN` are stable and never renumbered.
> Source review: Slice 1 review findings (`STATUS.md`) + Slice-2 boundary sweep
> (2026-07-15). Companion: `retrospectives/slice-1-product-list.md`.

Fields per item: **ID · Description · Reason for deferral · Estimated impact ·
Recommended sprint.**

---

## Accepted Debt

Known, deliberately kept as-is; no active breach.

### TD-004 — `CorsOptions` bound off the standard options pipeline
- **Description:** `CorsOptions` documents STD-BE-048 but binds ad-hoc via
  `builder.Configuration.GetSection(...).Get<CorsOptions>()` (`Program.cs:24`),
  bypassing `AddOptions().BindConfiguration().ValidateOnStart()`. (Review R4.)
- **Reason for deferral:** cosmetic/consistency; no runtime defect — CORS is a
  single dev-time policy today. Not worth a churn commit mid-slice.
- **Estimated impact:** Low. Misses fail-fast validation on a malformed CORS
  section; contained to startup config.
- **Recommended sprint:** Slice 2 (fold in while the API host is already open) or
  the first hardening pass that touches `Program.cs`.

### TD-005 — STD-BE-004 enforced by an enumerated entity allowlist
- **Description:** "Api never references domain entities directly" is enforced by
  a hardcoded allowlist (`LayeringTests.cs:62`, `"VetFlow.Domain.Catalog.Product"`)
  that omits real entities (`ProductNature`, `ProductUnit`). No current breach.
  (Review R5 / friction F6.)
- **Reason for deferral:** the rule holds today (those entities are not referenced
  from the Api); the gap is coverage of *future* entities, not a live violation.
- **Estimated impact:** Medium if unaddressed — a new entity could reach the Api
  undetected, silently eroding a Mandatory boundary.
- **Recommended sprint:** Slice 2 (before adding Product-editor entities, which is
  exactly when new domain types appear). See TD-101 for the fix shape.

---

## Deferred Improvements

Improvements with clear value, sequenced for later.

### TD-101 — Namespace-scope the architecture boundary rules
- **Description:** Replace the enumerated allowlist (TD-005) with a namespace-scoped
  NetArchTest so every `VetFlow.Domain.*` entity is covered automatically.
- **Reason for deferral:** best done together with TD-005 when the next entities
  land, to validate the rule against real new types rather than in the abstract.
- **Estimated impact:** Medium — converts a maintenance-prone allowlist into a
  self-maintaining guard; removes a recurring "did we add the entity?" step.
- **Recommended sprint:** Slice 2.

### TD-102 — Close the inline-style lint gap (F3)
- **Description:** STD-FE-041 names Stylelint as the logical-property enforcer, but
  Angular inline component styles are invisible to it (only `.scss` is linted).
  Either move component styles to `.scss` files or add an inline-style extractor.
- **Reason for deferral:** Slice 1 used logical properties throughout (verified by
  review); the gap is preventive tooling, not an active RTL breach.
- **Estimated impact:** Low–Medium — an unreviewed physical property could slip
  into an inline block and break RTL without tripping a gate.
- **Recommended sprint:** when the second UI slice stabilizes the component surface
  (Slice 2/3).

---

## Governance Friction

Places where a standard/process outran its enforcement. (Evidence only — no
governance change without the Governance Change Policy.)

### TD-201 — Named CI enforcement without a CI platform (F4)
- **Description:** Several standards name CI scripts (traceability check,
  error-catalog uniqueness, TODO scan); no CI platform exists, so equivalents run
  as architecture tests or locally.
- **Reason for deferral:** blocked on the CI-platform decision (owner open item);
  no remote/pipeline exists yet.
- **Estimated impact:** Medium — enforcement currently depends on local discipline;
  a contributor without the local run could bypass a "CI-enforced" standard.
- **Recommended sprint:** the sprint that picks the CI platform; then implement the
  named scripts (or restate the standards as architecture-test enforced).

### TD-202 (Resolved) — Claimed test that did not exist (F5 / R1)
- **Description:** STD-BE-020/028 claimed architecture-test enforcement of the
  forbidden-library ban with no such test.
- **Resolution:** fixed in `db0a671` — `LayeringTests` now bans
  MediatR/AutoMapper/FluentAssertions across all four production assemblies
  (proven to bite 2026-07-15); NgRx banned in `web/eslint.config.js`. Standard and
  code now agree. **Closed — retained as a lesson:** audit every "enforced by a
  test" claim against an actually-executing, biting test.
- **Estimated impact:** n/a (closed).
- **Recommended sprint:** done.

---

## Architecture Friction

Design-boundary edges surfaced by implementation.

### TD-301 — ADR-0005 "latest stable Angular" vs. component-foundation lag (F1)
- **Description:** ADR-0005 mandates "latest stable Angular"; PrimeNG (the approved
  foundation) lags one major (Angular 22 out, PrimeNG ≤21). Slice 1 ships on
  Angular 21 to avoid an unsupported pairing (principle 13).
- **Reason for deferral:** requires an owner ruling to amend ADR wording; the
  library-independence seam (ADR-0012) contains the impact meanwhile.
- **Estimated impact:** Low today (contained by the `Vf*` seam); Medium if the ADR
  is read literally by a future contributor and the pairing is forced.
- **Recommended sprint:** next ADR-review checkpoint — amend to "latest stable
  release supported by the approved component foundation."

### TD-302 — Product-image storage undefined (no ADR)
- **Description:** REQ-CAT-004/BR-CAT-049 allow one optional product image (shown
  on the details page only). No storage mechanism is decided — no file/object/blob
  storage ADR exists.
- **Reason for deferral:** image is optional and details-only; needs a small infra
  ruling (storage target, allowed types, max size) before the editor can persist it.
- **Estimated impact:** Medium — blocks the image capability of the Product Editor;
  a wrong default (e.g., bytes in the DB) is expensive to reverse.
- **Recommended sprint:** Slice 2 **decision** (see Phase-5 question Q5); implement
  once ruled, or defer image to the View-Details surface.

---

## Future Optimizations

Deliberately not built yet; no evidence they are needed.

### TD-401 — Caching not yet implemented (ADR-0011)
- **Description:** ADR-0011 defines a caching architecture; Slice 1 ships without
  caching (lookups and list queries hit PostgreSQL directly).
- **Reason for deferral:** "measure before optimizing" (principle). No measured
  latency problem; dataset is single-clinic scale.
- **Estimated impact:** Low at current scale; revisit if list/lookup latency is
  measured over budget.
- **Recommended sprint:** only when a measured budget breach appears (ADR-0016 §5
  budgets pending owner confirmation).

### TD-402 — Confirm CI performance-budget numbers (ADR-0016 §5)
- **Description:** performance budgets referenced by ADR-0016 are not yet confirmed
  by the owner.
- **Reason for deferral:** meaningful only once a CI platform exists (TD-201).
- **Estimated impact:** Low until CI exists; then Medium (defines the perf gate).
- **Recommended sprint:** with the CI-platform sprint.

---

## Cross-reference: Slice-2 blocking questions (not debt — decisions owed)

These are **not** deferred debt; they are undefined business behavior that blocks
Slice 2 and cannot be invented (governance: never invent a business rule). Tracked
here for visibility; full text in the Phase-5 questions to the owner.

| Ref | Blocks | Undefined thing |
|---|---|---|
| Q1 | Create Product | Internal-code **format** (REQ-CAT-008 / BR-CAT-006 / DEC-CAT-016) |
| Q2 | Edit (price change, dangerous unit edit) audit | Audit-log **infrastructure / record shape** (REQ-CAT-045; Audit Log module undocumented) |
| Q3 | Dangerous-operation confirmation | Trigger owned by Inventory/Batch (nonexistent) — DEC-CAT-025 pattern? |
| Q4 | Possible-duplicate detection | Similarity **strictness/threshold** (REQ-CAT-042 / BR-CAT-042) |
| Q5 | Product image | Storage mechanism (TD-302) |
