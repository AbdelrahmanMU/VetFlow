# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-07-16

## Current sprint

**Sprint 3 — Implementation.** The first product code of VetFlow.

Implementation outranks governance. If implementation exposes a weakness in the
foundation: **record it under "Foundation friction" below, keep working if it is
safe, and evaluate the change only after the feature is complete.** Governance
changes require evidence (Governance Change Policy — `docs/architecture/principles.md`).

**Every implementation session starts at `.claude/playbooks/implementation.md`.**

## Session close (2026-07-16) — three outcomes, then implementation begun & interrupted

This session produced, in order:
1. **F1 money-integrity fix** — canonical Arabic-Indic digit normalization for numeric inputs
   (`web/src/app/core/i18n/digits.ts` + `vf-number-input`). Committed **`f5c2443`** (frontend only;
   50 tests green; **not pushed** — no remote configured). The pilot report
   `docs/releases/pilot-p1-money-fix-report.md` is written but **uncommitted** (untracked).
2. **Managed Data documentation** — discovery → owner rulings → Approved. Two commits **`e61a19c`**
   (`docs(categories)`) and **`70542a4`** (`docs(catalog)`). Details below.
3. **Managed Data implementation** — owner authorized "Categories first, then Manufacturers." Work
   was set up (5 tasks; pattern-mapping step) but **interrupted before any code was written**.
   **Zero implementation code exists; the working tree is clean.** Resume from DoR-READY.

Nothing pushed this session (no git remote — owner item #4). Untracked and intentionally not
committed: `docs/releases/` (money report) and `docs/ui/product-editor-ux-architecture.md` (prior).

## Just completed (2026-07-16) — Managed Data slice (Categories / Manufacturers): DOCS APPROVED, DoR READY (no code)

Slice 4 (Managed Data) hit the `implementation.md` **Definition of Ready gate**: Categories docs
were placeholders and manufacturer management was only "add + select" (REQ-CAT-013). Owner ran a
**lightweight discovery**, then **finalized all business rulings (2026-07-16)** and directed
promotion to Approved. Documentation-only; **no code written**.

**Owner rulings (2026-07-16):**
- **DEC-CTG-002 = option (B):** deactivation always allowed (even when referenced); existing
  products keep their reference unchanged (no auto-modify, no delete); inactive values hidden from
  **new** product selection; on editing a product that already references an inactive value it
  stays visible/selected, but once changed it can't be re-selected unless reactivated.
- **Category fields:** single **Arabic name only** (no English, no multilingual).
- **Rename:** allowed, **no audit** in MVP; deferred audit decisions unchanged.
- **Delete:** prohibited while referenced by any product; **deactivate** is the official operation.

**Approved artifacts:**
- **Categories module** (`docs/modules/categories/*`, 8/8 **Approved**): REQ-CTG-001..005,
  BR-CTG-001..008, AC-CTG-001..005, DEC-CTG-001 (lifecycle) + DEC-CTG-002 (option B). New prefix **CTG**.
- **Catalog extensions** (**Approved**): REQ-CAT-047/048, BR-CAT-052/053, AC-CAT-047/048,
  **DEC-CAT-032**. BR-CAT-051 rule text unchanged, with an added cross-ref note time-scoping its
  audit clause for MVP (BR-CAT-053/DEC-CAT-032) — the audited-rename conflict is resolved, not silent.
- `_INDEX.md` Categories row → Approved.
- Consistency review clean: full REQ↔BR↔AC traceability, no orphans, no duplication, no conflict
  with existing Catalog decisions.

**DoR status: READY.** Implementation was authorized this session and set up, but **interrupted with
no code written** (see Session close). **Next session resumes here.** Suggested vertical cut:
Categories-first (create · rename · activate/deactivate · list) then Manufacturers (structurally
identical → cheap second), reusing the command/query pipelines, pg_trgm Arabic-name normalization,
and the Vf UI kit; editor options filter to active-only with inactive current values preserved
(REQ-CTG-005 / DEC-CTG-002).

**Implementation plan carried into next session (tasks 1–5):**
1. **Categories backend** — `Category` gains `IsActive` + `Rename`; Create/Update(rename)/SetActive
   commands (reuse the command pipeline STD-BE-020/021, no duplication); `CategoryListQuery`
   (search normalized-Arabic-name / pg_trgm, sort whitelist + `.ThenBy(Id)`, offset paging + page cap);
   EF migration (`is_active` + normalized-name column & gin index + unique index); endpoints
   (`/api/v1/categories` list/create; `{id}` rename; activate/deactivate); DI + `CommandPipeline`/
   `QueryPipeline` registration; error-catalog + ar/en resx; domain + integration tests.
2. **Categories frontend** — management screen reusing `VfTable`/`VfSearchInput`/`VfPagination`/
   `VfBadge`/`VfDialog`/`VfTextInput`; store (4 states); create/edit dialog (single Arabic name);
   row activate/deactivate; `ApiClient` calls; route + nav entry; vitest tests. RTL, logical CSS.
3. **Editor integration** — `CategoryOptions`/`ManufacturerOptions` queries filter to **Active only**
   for new products; edit mode preserves an inactive current value (REQ-CTG-005 / DEC-CTG-002).
4. **Manufacturers** — mirror the Categories pattern in the Catalog module (REQ-CAT-047/048,
   BR-CAT-052/053).
5. **Verify** — build · all tests · architecture tests · ESLint · Stylelint · browser + responsive +
   RTL + zero console errors; nine-question self-review; then stop for owner review (do not commit
   until reviewed, per the slice's stated stop condition).

## Just completed (2026-07-16) — Edit Product slice: COMPLETE (backend + frontend), owner-approved, COMMITTED

**The Edit Product slice (DEC-CAT-031 — non-audited unified Create/Edit editor) is fully
implemented, tested, live-verified, owner-reviewed, and COMMITTED: implementation `2e139ad`
(`feat(catalog): Edit Product — unified non-audited editor`) + a separate docs-sync commit
(this update). 173 tests green (backend 144 · frontend 29); all gates green. The owner ended
the session with an explicit stop after the two commits (no push; no new slice).**

### A) Product Editor UX Architecture doc — refined and owner-approved *in principle*
`docs/ui/product-editor-ux-architecture.md` (header still `Status: Draft`, **uncommitted**).
Two owner refinement passes applied (no redesign): per-section **Sources** traceability
blocks + **Implementation Notes** (design-system concepts only, no Angular/PrimeNG);
a **[تجربة] Defense Review** table (all 10 UX-only decisions defended, zero removed) +
a **UX-Decision Lifecycle** policy; a new **§15 Future UX Seams** (Audit History
DEC-CAT-028, Product Images DEC-CAT-029/TD-302) with a **Reserved-Seam-First** rule; and
a final **Product Editor UX Acceptance Checklist**. §0 made uniform. Owner said the doc is
**"approved / authoritative"** — but I was not asked to flip the header, so it still reads
Draft (see open item 1).

### B) Edit Product — re-scoped by DEC-CAT-031, backend built and green
- **DoR gate outcome:** Edit's audited paths (price change, dangerous unit edit) were
  BLOCKED by the pending audit-log record shape (DEC-CAT-028) + deferral (DEC-CAT-029).
  Surfaced the contradiction; owner ruled **DEC-CAT-031** (recorded in
  `docs/modules/catalog/decisions.md`): re-scope to a **non-audited unified Create/Edit
  editor** — one implementation, differences by configuration. Excluded (inert seams,
  untouched): selling-price editing, audit log/history, dangerous-op confirmation, images.
- **Architecture review (owner-required gate) PASSED** — the Create stack generalizes
  cleanly with no forced duplication (shared write contract + config, not a second path).
- **Backend COMPLETE & GREEN (committed `2e139ad`):**
  * Domain: `Product.Update()` sharing one invariant gate (`ValidateInvariants`) +
    `ApplyDetails` with the constructor; new `ProductUnitDraft`. Prices preserved by
    `UnitId` and never mutated; `InternalCode`/`Status` never touched; the dangerous-op
    path is an **inert code seam** (TODO citing DEC-CAT-028/029/031) — no visible UI.
  * Shared write contract: `IProductWriteCommand`/`IProductUnitWriteInput` + generic
    `ProductWriteCommandValidator<T>`; **Create** command/validator refactored onto it
    (validation not duplicated); new `UpdateProductCommand`/`Validator`/`Handler`
    (`ICommand<Guid?>`, null ⇒ 404).
  * `ProductDetailsDto` + handler gained `CategoryId/ManufacturerId/NatureId` (edit prefill).
  * `PUT /api/v1/products/{id}` (204 / 404); handler registered in Infrastructure DI +
    `CommandPipeline`; validator in Application DI.
  * **EF fix (keep):** `ProductUnitConfiguration` Id `ValueGeneratedNever()` — required so
    an edit that grows/replaces the unit profile INSERTs new rows instead of a spurious
    UPDATE (root-caused via change-tracker states: new units were marked Modified).
    `has-pending-model-changes` = none, **no migration needed**.
  * **Gates green:** `dotnet build` 0/0 (Release), `dotnet format` clean, **144 backend
    tests** (Domain 43 · Architecture 57 · Integration 44 — Testcontainers; Slice 2 was
    127 → +17). New tests: 11 domain (`ProductUpdateTests`: scalar edit, price preserved
    by unit / new unit null / role-change drops price, profile replace, re-enforced minimum,
    reject-without-mutation) + 6 integration (`ProductUpdateEndpointTests`: round-trip,
    price preservation, 404, per-field 400, **rename refreshes the search index**, edit-ids
    exposed).
- **FRONTEND COMPLETE & GREEN (committed `2e139ad`):** unified `product-editor-page.component`
  (`mode` from route `data`, `id` from param via `withComponentInputBinding`) **replaced**
  `product-create-page.component` (deleted); `buildProductForm()`/`unitRowFrom()` factories in
  `product-editor.forms.ts`; `EditProduct`/`UpdateProductPayload` models; editor API `load`/
  `update`; `ApiClient.put` (completes the ADR-0013 single access point); `unit-profile-editor`
  gained `priceEditable`/`currency` (edit shows price **read-only**, never mutated); route
  `products/:id/edit` (`data:{mode}`); «تعديل» button on the details page. **29 frontend tests**
  (create both-mode logic + edit load/prefill/PUT-without-price/no-duplicate-check). Edit runs
  **no** possible-duplicate advisory (create-only); internal code shown read-only; `InternalCode`/
  `Status` never touched; dangerous-op confirmation remains an inert code seam.
- **Live-verified (headless Chrome, real stack):** edit round-trip (prefill → rename → PUT →
  Details shows the new name), multi-unit role-select prefill (storage/default-sale/default-purchase
  all populated), read-only price in edit + editable price on create, RTL correct, **zero
  page-level horizontal overflow at 1440 and 390 px**, **zero console errors/warnings**. `PUT`
  preserves `internalCode` and `status`; the two dev-DB names touched during verify were reverted.



**A design-only session** (owner-directed: no production code, no changes to any
rule/requirement/ADR/standard/workflow/acceptance/playbook, no `ui.md` edit).
Produced one new document; **uncommitted** and awaiting owner review.

- **`docs/ui/product-editor-ux-architecture.md`** (Draft) — the definitive Product
  Editor UX reference, framed as the frontend equivalent of ADR-0014. Extends
  `design-language.md` + `ui.md` §5 **without redefining or modifying them**, and
  **invents no business rule** — every behavior cites its `REQ/BR/DEC/WF` id; every
  pure interaction choice is tagged **[تجربة]**. Written in Arabic per ADR-0002 +
  the design-doc precedent (owner-chosen).
- **Covers:** editor philosophy · screen anatomy (why each region exists) · the
  create journey · section-by-section form design · a deep Unit-Profile treatment ·
  recovery-oriented validation + duplicate comparison dialog · save experience ·
  future Edit experience · read-mode Details page · three independent responsive
  layouts (desktop rail / tablet chips / mobile stepper) · micro-interactions ·
  accessibility · a premium critique (vs Stripe/Linear/Notion/Shopify) · an
  implementation review (reusable patterns) · and the closing block (Executive
  Summary · 13 numbered UX Decisions UXD-ED-001..013 · Risks · Future Extensions ·
  Self-Critique · Readiness).
- **Decisions recorded IN the document** (its proper home — a design-architecture
  doc, like an ADR records its own): UXD-ED-001..013. These are **design proposals
  pending owner approval (Draft)**, not business rulings — no `DEC-CAT`/`DECISION_LOG`/
  ADR entry is owed (no business behavior was decided this session).
- **Deliberately NOT designed** (left as defined placeholders, per prior rulings):
  audit-log panel content (DEC-CAT-028), image-storage flow (TD-302 / DEC-CAT-029),
  danger-dialog activation (owned by not-yet-built units).

## Just completed (2026-07-15) — Slice 2: Catalog → Create Product + View Details (S2/S3)

**Implemented, tested, live-verified, owner-reviewed, and COMMITTED** (`ea107cc`,
2026-07-15). Scope per DEC-CAT-029: Create Product + read-only View Details only; Edit,
image storage, dangerous-op confirmation, and audit events remain deferred (DEC-CAT-028/029).

**Owner review rulings (2026-07-15, approving the slice):**
- **DEC-CAT-030** — open-expiration granularity is **days only** for MVP (no hours selector);
  value stored as `TimeSpan` as implemented; sub-day precision is an approved future extension.
- **Field-length caps** (name 300, short-text 100, notes 2000) accepted as **engineering
  constraints, not business rules** — no change (ledger TD-006).
- **RFC 9457 `about:blank` 404** accepted as-is this slice; follow-up recorded (ledger TD-103):
  "Every business failure should eventually resolve to a VTF error code; infrastructure failures
  may remain generic."
- **Command pipeline approved as the reusable write-side foundation** — every future write slice
  **must reuse** `ICommand`/`ICommandHandler` + the Validating/Logging decorators + `CommandPipeline`
  composition; **do not duplicate** it (ledger TD-104 covers the future transaction decorator).

- **First write path in the system.** A command pipeline mirroring the query side:
  `ICommand<TResult>` + `ICommandHandler<,>` (STD-BE-020/021), a `ValidatingCommandHandler`
  and `LoggingCommandHandler` decorator (`Application/Common/Behaviors`), composed
  explicitly in `Api/Composition/CommandPipeline.cs` and registered like `QueryPipeline`.
  One command = one `SaveChanges` (STD-BE-024). No MediatR/AutoMapper (arch-test banned).
- **Domain:** `Product` gained `InternalCode` (required, held) + `InternalNotes` (optional);
  creation invariants stay in the aggregate (BR-CAT-001/009/016/020/021/022/025/036).
- **Internal code (DEC-CAT-026):** PostgreSQL sequence `product_internal_code_seq`, `nextval`
  allocated at persist time → `PRD-000001` (`InternalProductCode`); unique index proves
  uniqueness under concurrency. Never entered, not a search key (DEC-CAT-016).
- **Possible-duplicate (DEC-CAT-027):** a separate **advisory read** — pg_trgm
  `similarity() >= 0.4` (named tunable const) on a new normalized-Arabic-name column
  (interceptor-maintained, own gin index) **AND** same manufacturer. Never blocks the write
  (BR-CAT-042 / DEC-CAT-018); the UI surfaces the comparison dialog and the user decides.
- **Endpoints:** `POST /api/v1/products` (201 + Location + `{id, internalCode}`),
  `GET /api/v1/products/{id}` (404 → problem+json), `GET /api/v1/products/possible-duplicates`,
  and a new `GET /api/v1/units` lookup for the unit-profile editor. Per-field VTF-VAL-001
  validation (AC-CAT-043) + 15 new ar/en `validation.*` keys (localization test extended).
- **Frontend (Angular 21, zoneless, OnPush, typed reactive forms):** new `Vf*` form-input
  kit behind the PrimeNG seam — `VfTextInput`/`VfNumberInput`/`VfTextarea` (ControlValueAccessor),
  `VfDialog`, and additive `error`/`required` on `VfSelect`; the Create page (identity ·
  classification · capabilities · unit-profile editor · prices/notes) with the duplicate-warning
  dialog; the read-only Details page with the four data-view states incl. not-found; «منتج جديد»
  CTA wired on the list + empty state. Arabic-first RTL, logical CSS only, resource strings.
- **Tests (+24):** Domain 28→32 (internal code, notes), Architecture 30→57 (command-handler
  naming/location conventions + validation-key localization theory), Integration 29→38
  (create round-trip, code format+uniqueness+ascending, per-field 400, open-expiration,
  localization, 404, duplicate manufacturer-scoping, /units), frontend 18→26 (create form
  validation/happy-path/duplicate-warning, details store ready/not-found/error, CVA input).
- **Gates green — INDEPENDENTLY re-run by the orchestrator (not trusting the build agent):**
  `dotnet build` 0/0 (Release), `dotnet format` clean, **127 backend** (Domain 32 · Architecture
  57 · Integration 38 — Testcontainers), frontend prod build clean (493 kB), ESLint + Stylelint
  clean, **26 frontend**. Total **153 tests** (Slice-1 105 → +48). The R1 forbidden-library
  NetArchTest re-proven to bite.
- **Live-verified (orchestrator, real stack):** compose db + API (:5080) + `ng serve` (:4200).
  Own curl round-trips: create → 201 `PRD-000003`, second → `PRD-000004` (ascending — sequence
  does not reset), GET details (unit storage/default flags, Money EGP), 404 `application/problem+json`,
  per-field 400 `VTF-VAL-001`, en-localized messages, possible-duplicates endpoint 200.
  **Browser (headless Chrome, correct routes `/catalog/products{,/new,/:id}`):** create form and
  details page render correctly in RTL; **zero page-level horizontal overflow at 390 px** (CDP
  `scrollWidth==innerWidth` on both); **zero console errors/warnings** on both. (Note: the build
  agent's "routes serve 200" claim used `/products/new` — that path hits the `**` redirect to the
  list; the real editor route is `/catalog/products/new`. App-internal navigation uses the correct
  nested paths — verified.)
- **Scope boundary noted (not a defect):** on an empty catalog there is no in-slice way to add
  categories/manufacturers (managed-data / Categories module slice — DEC-CAT-025); the editor
  consumes existing category/manufacturer/nature/unit lookups.
- **Docs synced:** `_INDEX.md` Catalog row; GLOSSARY rows for «الكود الداخلي» / «تكرار محتمل».

## Just completed (2026-07-15) — Slice 1: Catalog → Product List (S1)

**The first vertical slice is implemented, tested, verified in the browser, and
committed** (`db0a671`, owner-reviewed 2026-07-15). Commit gate re-verified green
at commit time: build 0 warnings, 105/105 tests passing (backend 87 — Domain 28,
Architecture 30, Integration 29; frontend 18).

- **Scope per DEC-CAT-025** (owner rulings 2026-07-15): read-only list — search
  (Arabic/English/barcode, write-time Arabic normalization + pg_trgm), the 8
  Catalog-owned filters, whitelisted sorting, offset pagination, all four data
  states, RTL premium UI per the Design Language, adaptive mobile card list.
  Deferred to their own slices: row/bulk actions + «منتج جديد» (S2/S3/S5),
  stock surfaces (Inventory/Monitoring), authentication (ADR-0010 open items),
  internal-code generation format (needs an owner ruling), context menu,
  camera barcode scan, saved views.
- **Backend:** .NET 10 solution per ADR-0014 (Domain/Application/Infrastructure/
  Api; modules as namespaces `Catalog` + minimal `Categories` scaffold);
  query-side CQRS pipeline (own `IQueryHandler<,>` + validation/logging
  decorators; no command pipeline yet — no commands exist); EF Core + Npgsql,
  global snake_case convention, migration `InitialCatalogSchema` (seeds the 13
  default units + 5 natures); RFC 9457 middleware as the single translation
  point with an Error Catalog (VTF-VAL-001, VTF-CAT-009/016/020/021/022/025/036)
  and ar/en resx messages; TraceId/CorrelationId; Serilog; validated typed
  options; endpoints `GET /api/v1/products|categories|manufacturers|product-natures`.
- **Frontend:** Angular 21 (see friction F1) + VetFlow UI Kit over PrimeNG
  (imports fenced by ESLint per ADR-0012/STD-FE-003): VfTable/VfDrawer/VfSelect/
  VfPopover/VfButton/VfSearchInput/VfBadge/VfChip/VfCheckbox/VfSkeleton/
  VfEmptyState/VfPagination + VetFlow theme preset; shell with right-side
  sidebar; signals + RxJS only at HTTP/debounce boundaries; strict TS, OnPush,
  zoneless; Arabic resource file; IBM Plex Sans Arabic; stylelint bans physical
  left/right properties.
- **Tests (102):** 28 domain unit (BR-named per ADR-0016), 29 architecture
  (NetArchTest: dependency rule, module boundaries, conventions, error-catalog
  integrity), 27 integration against real PostgreSQL (Testcontainers — search
  normalization, barcode→owning product, all 8 filters, sorting incl.
  price-nulls-last, pagination cap, Problem Details shapes, localization,
  correlation header), 18 frontend (vitest: UI Kit components + store states).
- **Verified live:** compose db + API + `ng serve` with proxy; screenshots
  reviewed at 1440×900 and 390×844 — search, filters drawer, empty states,
  zero console errors, zero horizontal overflow.
- **Docs synchronized:** DEC-CAT-025 added; GLOSSARY seeded with the Catalog
  terms (from the approved docs — closes part of the cross-module debt);
  `_INDEX.md` Catalog row updated.

### How to run (dev)

`copy .env.example .env` (choose a local password) → `docker compose up -d db`
→ `dotnet run --project src/VetFlow.Api` with env
`Database__ConnectionString=Host=localhost;Port=5434;Database=vetflow;Username=…;Password=…`
→ `cd web && npm start` → http://localhost:4200. Tests: `dotnet test` (Docker
needed for integration) · `cd web && npm test && npm run lint && npm run lint:css`.

## Slice 1 review findings (2026-07-15)

Self-review + repository/governance review complete (7 dimensions, adversarially
verified; gates independently re-run green: build 0/0, format clean, 102/102).
`spec-conformance` and `doc-sync` dimensions returned **zero** findings — no scope
creep beyond DEC-CAT-025, nothing in-scope missing, and every concrete STATUS
claim (102 tests, 13 units + 5 natures, 8 filters, 4 endpoints, VTF-* codes)
verified against code. `.gitignore` gap already closed; staging dry-run clean.
**5 confirmed findings — none breaks the commit gate; none has an active breach:**

| # | Sev | Finding | Fix |
|---|---|---|---|
| R1 | Major | Forbidden-library ban (STD-BE-020/028) claims architecture-test enforcement but **no such test exists** — banned libs verified absent, so no breach, but the preventive gate is missing and the standard contradicts the code (`ai-governance.md` **Never**: "leave documentation contradicting the code"). | Add a NetArchTest asserting no assembly depends on MediatR/AutoMapper/FluentAssertions/NgRx (~10 lines). |
| R2 | Minor | Unbounded `?page` overflows int32 offset → runtime 500 (`ProductListQueryHandler.cs:59`; also `LookupOptionsQueryHandler.cs:33`). Validator caps only `Page >= 1`. | Upper-bound `Page` in the query validators. |
| R3 | Minor | Offset pagination has no unique tiebreaker (`ProductListQueryHandler.cs:156-158`); duplicate names (BR-CAT-042/DEC-CAT-018) can skip/duplicate rows across pages. | Append `.ThenBy(row => row.Id)` to every sort branch. |
| R4 | Minor | `CorsOptions` documents STD-BE-048 but binds ad-hoc (`Program.cs:24`), bypassing `AddOptions/ValidateOnStart`. | Register via `AddOptions().BindConfiguration().ValidateOnStart()`, or drop the claim. |
| R5 | Minor | STD-BE-004 arch test uses a hardcoded 4-entity allowlist (`LayeringTests.cs:52`); `ProductNature`/`ProductUnit` omitted. No current breach. | Make the rule namespace-scoped. |

Refuted (2, correctly): `@for track part` NG0955 (Angular 21 warns, not throws)
· frontend query-param assertions (ADR-0016 deliberately favors server-side proof).
Full report: session scratchpad `slice1-final-report.md`.

**Resolution (owner ruling Path A, 2026-07-15):** **R1, R2, R3 FIXED** and
covered by new tests; **R4, R5 remain as approved follow-up work** (do not
lose). Fixes and gates:
- **R1** — `LayeringTests.Production_code_depends_on_no_forbidden_library_STD_BE_020_STD_BE_028`
  now bans MediatR/AutoMapper/FluentAssertions across all four production
  assemblies (NetArchTest); NgRx is banned in `web/eslint.config.js` (a .NET
  test cannot see npm packages). STD-BE-020/028 now match the code — friction
  **F5 resolved**.
- **R2** — `MaxPage = int.MaxValue / MaxPageSize` added to `ProductListQuery` and
  `LookupOptionsQuery`; both validators reject over-range pages with new key
  `validation.page.max` (ar/en). Overflow `?page` is now a 400, never a 500.
- **R3** — `.ThenBy(row => row.Id)` appended to every sort branch in
  `ProductListQueryHandler.ApplySorting` → total order, stable pagination.
- **Gates re-run green (2026-07-15):** build 0/0, format clean, **105/105**
  tests (28 domain · 30 architecture (+1) · 29 integration (+2) · 18 frontend),
  ESLint + Stylelint clean.

**R4/R5 are still open** — see the follow-up list below and friction F6.

## Hardening verification + Slice 2 boundary (2026-07-15)

Owner directive: hardening pass (R1–R3) → re-run every gate → retrospective →
tech-debt ledger → start Slice 2 (Product Editor).

- **Phase 1 (R1–R3) was already complete in `db0a671`** — verified in *source*,
  not just docs (R1 `LayeringTests.cs:79`; R2 `MaxPage` in both validators; R3
  `.ThenBy(row.Id)` at :163). The directive predated the fix; not redone.
- **Phase 2 — all gates re-run green (not trusting prior results):** build 0/0
  (Release), `dotnet format` clean, **105/105** (arch 30 · domain 28 · integration
  29 · frontend 18), ESLint + Stylelint clean. **R1 proven to *bite*:** a probe
  adding a present dependency turned the NetArchTest red; ESLint errors on an
  `@ngrx` import. Working tree clean.
- **Phase 3 — retrospective:** `docs/architecture/retrospectives/slice-1-product-list.md`.
- **Phase 4 — tech-debt ledger:** `docs/architecture/TECH_DEBT_LEDGER.md`
  (TD-004/005 accepted, TD-101/102 deferred, TD-201/202 governance, TD-301/302
  architecture, TD-401/402 future).
- **Phase 5 — Slice 2 sweep done; owner rulings received and recorded; build
  deferred to a fresh implementation session (no code written).** The sweep of the
  Product Editor capability list found undefined business behavior at the core
  paths; the owner ruled and the rulings are recorded as **DEC-CAT-026…029**:
  - **DEC-CAT-026 (Q1):** internal-code format = `PRD-` + ≥6-digit zero-padded
    unique sequence (`PRD-000001`).
  - **DEC-CAT-027 (Q4):** possible-duplicate = fuzzy Arabic name (pg_trgm) **AND**
    same manufacturer; initial threshold **0.4** (delegated to engineering, tunable).
  - **DEC-CAT-028 (Q2):** audit-log — the Q2-vs-scope contradiction is **resolved
    FINAL by the owner (2026-07-15): scope wins**. Slice 2 = Create + View only;
    **no audit implementation in this slice**; the audit model is designed when the
    Edit slice begins.
  - **DEC-CAT-029 (scope):** implement **Create Product + View Details only** now
    (fully specified by DEC-CAT-026/027); **defer Edit's audited paths** (price
    change, dangerous unit-profile edit + confirmation, Q3) and **product image**
    (Q5 — storage undefined, ledger TD-302). Create introduces the **first write
    path (Command) in the system**; dangerous-op confirmation seam designed inert
    (no stock source yet — DEC-CAT-025 pattern).
  - **Why the build is deferred, not started here:** this session's context already
    carries Phases 1–4 + the full doc sweep + the Q&A + decision-authoring. Slice 2
    bootstraps the entire write side (first Command pipeline) **and** the first form
    UI-kit components — a Medium-plus, bootstrap-heavy slice (F2 occurrence #2). The
    playbook mandates a clean, budgeted `implementation.md` session for exactly this;
    starting it in a full window risks corner-cutting (owner forbade) or running dry
    mid-slice. Deliverables are durable; nothing is lost. **DoR is satisfied**
    (Catalog docs Approved; DEC-CAT-026/027/029 close the gaps; IDs named below).
  - **Next session — cut vertically (owner verifies slices live in the browser):**
    **Create-first** (POST + domain aggregate/factory + internal-code sequence +
    duplicate-warning query + `PRD-` sequence + the form UI-kit it needs + tests),
    then **View Details** (GET + details page). IDs: REQ-CAT-001/007/008/009/010/
    011/013/014/015–025/042/043; BR-CAT-001/005/006/008/009/011/012/014/016/024/
    025/042/043. **Before the Edit slice:** get the audit record shape and confirm
    the DEC-CAT-028 reconciliation.

## In flight / next

**The Edit Product slice is DONE and committed** (`2e139ad` feat + the docs-sync commit that
carries this update). Nothing is in flight. **No slice is authorized to start** — the owner
ended this session with an explicit stop (no Audit, no Images, no Categories).

**Candidate next slices (owner picks — do not start unprompted):**
- **Categories / managed-data (S6, ledger TD-105)** — the recommended next step: today a
  product cannot be created on an empty catalog without DB-seeding categories/manufacturers,
  and the editor's "add a value from the select" (ui.md §5.2) is deferred to this screen.
  Fully additive; no blocked owner input.
- **Audited Edit paths** — BLOCKED on the audit-log record shape (DEC-CAT-028, owner input)
  + dangerous-op confirmation-seam activation; do not start until those are defined.
- **Auth (ADR-0010 open items)** — mechanism undecided.
- **Product images (TD-302)** — BLOCKED on a storage ruling.

**Then (separate sessions):** raise the UX doc's 5 new design components to
`docs/ui/components.md`; the audited Edit paths (price change, dangerous unit edit) remain
blocked on the audit-log record shape (DEC-CAT-028) + confirmation-seam activation.

Environment: Node 24 LTS via nvm; host port 5434 for the dev DB; API on :5080. Note: a
leftover `VetFlow.Api` dev process can hold DLL locks — if a build fails with MSB3021/MSB3026,
stop that process first.

## Open items for the owner

1. **`docs/ui/product-editor-ux-architecture.md` — NOT promoted to Approved; deviation reported
   (2026-07-16, post-implementation Task 2).** The promotion was conditional on the implementation
   matching the doc. It does **not**: per the owner's binding guardrail this slice built the
   *minimal* editor (existing create-page fidelity + mode config) and deliberately **none** of the
   doc's architecture — no section rail, dirty-tracking, before→after diff dialog, stepper, or the
   three independent responsive layouts; the doc itself labels Edit as «مستقبلي» (future). Flipping
   it to Approved would assert an implemented design that mostly does not exist in code (docs-vs-code
   contradiction — forbidden). **Left `Status: Draft` and uncommitted, pending an owner ruling:**
   keep it as a forward-looking design reference (e.g. a "Design-approved, not yet implemented"
   status), or re-scope it to what shipped. No `DEC`/ADR is owed.
2. **Audited Edit paths (owed before they can be built):** define the audit-log **record
   shape** (table/fields/retention — DEC-CAT-028) and confirm the dangerous-op confirmation
   seam activation (DEC-CAT-029 / DEC-CAT-031 / ledger Q3). The *non-audited* Edit slice
   (DEC-CAT-031) proceeds without them; price-editing + dangerous-unit edits stay deferred.
3. Amend **ADR-0005** wording per friction F1 (Angular pinned to 21, not latest,
   because PrimeNG lags one major) — owner ruling required.
4. CI platform is undecided (no remote, no pipeline). ADR-0016's CI enforcement
   currently runs locally only — pick a platform so the gates become CI.
5. Approve the Sprint 1 shared docs and the `BD-*` registry (carried over).
6. Answer `domain-overview.md` TODOs 2–6 (carried over).
7. Flip ADR-0003…0019 Proposed → Accepted when ready (carried over).
8. Confirm the CI performance budget numbers (ADR-0016 §5) (carried over).
9. Catalog `overview.md` purchase-cost negative-boundary statements
   (DEC-CAT-024 follow-up) — unanswered since Sprint 1 (carried over).

## Sprint 2 — Engineering Foundation (COMPLETE, 2026-07-14)

| Layer | Where | Contents |
|---|---|---|
| Constitution | `docs/architecture/principles.md` | 14 principles · authority hierarchy · Governance Change Policy |
| Map | `docs/architecture/overview.md` | System shape · Engineering Decision Matrix |
| Decisions | `docs/architecture/decisions/` | ADR-0001 … ADR-0019 |
| Standards | `docs/architecture/standards/` | 137 executable standards |
| AI rules | `.claude/rules/ai-governance.md` | Always loaded |
| Execution | `.claude/playbooks/implementation.md` | The only implementation playbook |

**The stack:** ASP.NET Core (LTS) · EF Core · PostgreSQL · Angular (stable) ·
VetFlow UI Kit over PrimeNG · Docker.
**Rejected — do not re-propose without evidence:** MediatR · AutoMapper ·
FluentAssertions · generic repositories · `Result<T>` · NgRx.

## Sprint 1 — Documentation (carried forward)

- Catalog module: 8/8 documents **Approved 2026-07-14**.
- Shared docs (Draft): `VISION.md`, `GLOSSARY.md` (Catalog terms seeded
  2026-07-15), `personas.md`, `domain-overview.md`, `PROJECT_CONTEXT.md`.
- `DECISION_LOG.md`: 31 `BD-*` decisions, all Draft.

## Cross-module debt (do not lose)

- ~~Seed `GLOSSARY.md` with the Catalog workshop terms~~ — **done 2026-07-15**
  (Arabic forms still pending owner approval with the rest of the glossary).
- Confirm/extend Catalog events in `docs/shared/events.md`.
- Amend `VISION.md` principle 5 + `personas.md` + BD-SEC-002 per DEC-CAT-015.
- Future discovery agenda: low-stock threshold ownership → Monitoring/Inventory ·
  purchase-cost model → Purchasing · duplicate-match strictness → Catalog UI review.
- `docs/ui/components.md` / `navigation.md` placeholders: the UI Kit now has 12
  concrete `Vf*` components defined in practice by Slice 1 — document them there
  when the second UI slice stabilizes the surface.

## Foundation friction (evidence for future governance change)

| # | Date | Friction | Occurrences | Proposed change |
|---|---|---|---|---|
| F1 | 2026-07-15 | ADR-0005 mandates "latest stable Angular", but PrimeNG (the approved component foundation) lags one major: Angular 22 is out, PrimeNG supports ≤21. Running an unsupported pairing would violate principle 13, so Slice 1 ships on Angular 21. | 1 | Amend ADR-0005 wording to "the latest stable release **supported by the approved component foundation**" — owner ruling required. |
| F2 | 2026-07-15 | The first vertical slice cannot fit the New Feature context budget (Medium ≤60k): it inherently bootstraps the solution, the UI Kit, the theme, Docker, and the test harness on top of the feature. | 1 | None yet (bootstrap is one-time). If a future module scaffold hits it again, add a "Bootstrap" mode to the playbook. |
| F3 | 2026-07-15 | STD-FE-041 names Stylelint as the enforcer, but Angular inline component styles are invisible to stylelint; only `.scss` files are checked. Slice 1 uses logical properties throughout (verified by review). | 1 | Either move component styles to `.scss` files or add an inline-style extractor to the lint pipeline. |
| F4 | 2026-07-15 | Several standards name CI scripts/analyzers as enforcement (traceability check, error-catalog uniqueness, TODO scan) but no CI platform exists yet; equivalents run as architecture tests or locally. | 1 | Blocked on the CI platform decision (owner item 3); then implement the named CI scripts. |
| F5 | 2026-07-15 | STD-BE-020/028 claim architecture-test enforcement of the forbidden-library ban ("no mediator library", "no AutoMapper reference"), and ADR-0014 says "a rule without a test is a wish" — but no such test exists (banned libs verified absent, so no breach). Review finding R1. | 1 | Add the NetArchTest so the claimed enforcement is real; the standard currently overstates enforcement. |
| F6 | 2026-07-15 | STD-BE-004 (Mandatory, "Api never references domain entities directly") is enforced by a hardcoded entity allowlist that omits real entities (`ProductNature`, `ProductUnit`), so it under-enforces a Mandatory rule. Review finding R5. | 1 | Prefer namespace-scoped architecture rules over enumerated allowlists so new entities are covered automatically. |
