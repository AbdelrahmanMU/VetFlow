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

## Slice 2 follow-ups (recorded 2026-07-15, owner review of Create + View Details)

### TD-006 — Field-length caps are engineering constraints (Accepted, owner-ruled)
- **Description:** the create validator applies max-length caps (Arabic/English name 300,
  size/concentration/barcode 100, internal notes 2000). No documented business limit exists.
- **Reason for deferral:** owner ruled these are **engineering constraints, not business
  rules — approved as-is, no change required**.
- **Estimated impact:** none (accepted). Adjustable if a documented limit ever appears.
- **Recommended sprint:** none.

### TD-103 — Every business failure should resolve to a VTF error code (owner-ruled follow-up)
- **Description (owner wording):** "Every business failure should eventually resolve to a VTF
  error code. Infrastructure failures may remain generic." Today the Product-details 404 returns
  a bare `about:blank` problem+json (valid RFC 9457, no `VTF-` code).
- **Reason for deferral:** owner accepted the current behavior for this slice; **do not modify the
  implementation in this slice**.
- **Estimated impact:** Low — consistency of the error surface, not correctness.
- **Recommended sprint:** a future error-surface pass (candidate: alongside the Edit slice or an
  API-standards hardening pass).

### TD-104 — Transaction decorator for multi-step commands
- **Description:** the command pipeline has no transaction decorator; the single-aggregate
  `CreateProduct` is inherently one `SaveChanges` (STD-BE-024), so none is needed yet. The seam
  is documented for the first command that spans multiple writes.
- **Reason for deferral:** YAGNI — no multi-step command exists.
- **Estimated impact:** Low until a multi-step write appears; then Medium (atomicity guarantee).
- **Recommended sprint:** the first write slice that mutates more than one aggregate.

### TD-105 — In-app managed-data screen (categories / manufacturers) + add-from-select
- **Description:** creating a product requires a pre-existing category + manufacturer; there is no
  in-app way to add them yet (seeded/DB-inserted for now). `ui.md` §5.2's "add a new value from
  within the select" is deferred with this screen.
- **Reason for deferral:** managed-data / Categories module is a separate slice (DEC-CAT-025); out
  of the Create scope.
- **Estimated impact:** Medium — on an empty catalog, Create cannot be exercised without seeding.
- **Recommended sprint:** the Categories / managed-data slice (S6).
- **Status (2026-07-16, uncommitted):** the **management screens are built** — Categories
  (`/categories`) and Manufacturers (`/manufacturers`) list/search/sort/paginate + create/rename/
  activate/deactivate. The remaining piece — inline **add-a-value-from-within-the-select** (ui.md §5.2)
  — is still deferred (not in the Managed Data slice scope).

### TD-106 — Managed-data duplication: rule-of-three watch (recorded 2026-07-16)
- **Description:** Categories (own module) and Manufacturers (Catalog) are two near-identical
  managed-data stacks — aggregate + name-command validator base + list query/handler + name-uniqueness
  helper + endpoints/request DTOs on the backend, and a full feature folder + the editor's
  `*SelectOptions` computed on the frontend. This is a **deliberate copy** (owner directive: prefer copy
  over premature framework; rule of three not met at N=2). Cross-cutting review (Task 4) answered all
  five extraction questions **No**.
- **Reason for deferral:** at two occurrences, a shared "Managed Lookup" abstraction would couple two
  modules through a framework and add generic indirection — a net Simplicity-Budget loss (ADR-0014 §12).
  The entities may also diverge (manufacturers could later gain English names or audited rename).
- **Estimated impact:** Low today; duplication is module-local and independently readable.
- **Recommended sprint:** re-evaluate when a **third** managed-data entity appears (Suppliers,
  ProductNature management, …) — and even then weigh the cross-module coupling cost before extracting.

### TD-107 — Frontend initial-bundle warning budget crossed (F8, recorded 2026-07-16)
- **Description:** the Managed Data slice's eager `ar.ts` i18n additions pushed the initial JS to
  503.56 kB vs the 500 kB `maximumWarning` budget (`maximumError` 1 MB — build still passes). The whole
  UI string table lives in one eagerly-loaded `AR` object. The Purchase List slice added its own eager
  `purchases.*` strings (~4.5 kB → 508.14 kB), continuing the trend.
- **Reason for deferral:** warning-level only; raising a budget is a gate change needing owner approval,
  and lazy-splitting i18n is an architectural change out of the slice scope.
- **Estimated impact:** Low now; grows as more features add eager strings.
- **Recommended sprint:** **owner ruling (2026-07-17): keep TD-107 open — do NOT raise budgets, do NOT
  relax gates, do NOT optimize prematurely.** Bundle optimization happens only after measurable user
  impact or future growth. Candidate fix stays lazy per-feature i18n.

### TD-108 — Status sort uses lexicographic string order, not lifecycle order (recorded 2026-07-17, owner-ruled)
- **Description:** "sort by status" orders by the persisted enum string (`HasConversion<string>`), i.e.
  alphabetically — Cancelled → Draft → Received for Purchasing (`PurchaseListQueryHandler.ApplySorting`),
  and Active → Disabled for Catalog (`ProductListQueryHandler.ApplySorting`). Not a business lifecycle order.
- **Owner ruling (2026-07-17):** business ordering must always be **explicit** — never depend on enum names
  or localized strings. Define the Purchasing lifecycle order as **Draft → Received → Cancelled** via an
  explicit internal mapping (ordinal / switch / expression). Reuse the same business-ordering pattern for
  Product Status to keep the system consistent. **Explicitly NOT a blocker for the Purchase List slice** —
  recorded as a small follow-up to implement **together with Product Status in a later consistency pass**,
  so both modules gain explicit ordering at once (avoids a cross-module divergence mid-slice).
- **Estimated impact:** Low — status sort is deterministic today, just not lifecycle-ordered; consistent
  across both modules (both lexicographic).
- **Recommended sprint:** a small cross-module consistency pass covering Purchasing + Catalog status sort.

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

## Cross-reference: Slice-2 questions — all RESOLVED (owner rulings 2026-07-15)

These were undefined business behavior that blocked Slice 2. The owner ruled them all;
recorded here with their resolution.

| Ref | Was blocking | Resolution |
|---|---|---|
| Q1 | Create Product | **DEC-CAT-026** — `PRD-` + ≥6-digit zero-padded sequence. **Done.** |
| Q2 | Edit audit | **DEC-CAT-028** — deferred with the Edit slice (scope wins, final); audit record shape still owed before Edit. |
| Q3 | Dangerous-operation confirmation | **DEC-CAT-029** — deferred with the Edit slice; seam designed inert (no stock source). |
| Q4 | Possible-duplicate detection | **DEC-CAT-027** — fuzzy Arabic name + same manufacturer, pg_trgm ≥ 0.4. **Done.** |
| Q5 | Product image | **TD-302** — storage undefined; deferred (DEC-CAT-029). |
| — | Open-expiration granularity | **DEC-CAT-030** — days only (MVP); `TimeSpan` stored; sub-day is an approved extension point. **Done.** |
