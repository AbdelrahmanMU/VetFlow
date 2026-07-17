# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-07-17

## Current sprint

**Sprint 3 — Implementation.** The first product code of VetFlow.

Implementation outranks governance. If implementation exposes a weakness in the
foundation: **record it under "Foundation friction" below, keep working if it is
safe, and evaluate the change only after the feature is complete.** Governance
changes require evidence (Governance Change Policy — `docs/architecture/principles.md`).

**Every implementation session starts at `.claude/playbooks/implementation.md`.**

## Session close (2026-07-17) — Purchasing Slice 1 (Purchase List) DONE, owner-APPROVED (final), COMMITTED (`c2669f7` + this docs commit)

**The Purchase List vertical slice is implemented, fully gated, live-verified, owner-approved, and
COMMITTED as two commits** (`c2669f7` `feat(purchasing): Purchase List (Slice 1)` — implementation +
tests; this `docs(purchasing): synchronize repository after Purchase List` — STATUS/_INDEX/GLOSSARY/
module docs/TD ledger, no mixing). **Not pushed** (no remote — standing owner item). Owner ended the
session with an explicit stop after the two commits: do NOT start Purchase Details / Slice 2.

**Scope delivered (exactly the approved docs):** read-only list (REQ-PUR-001) — search (number exact /
supplier contains / reference contains, Arabic-normalized, **not notes**), status + invoice-date-range
filters, whitelisted sort + stable `Id` tiebreaker, default newest-first, pagination, Arabic status
badges (AC-PUR-002), `PUR-000001` format (AC-PUR-003, BR-PUR-002), four data-view states, RTL,
responsive table/cards. **Nothing else** — no create/edit/receive, inventory, suppliers CRUD, ledger,
payments, taxes, discounts, line items (scope lock held).

**Architecture:** new **isolated Purchasing module** (Domain/Application namespaces; arch isolation test
extended to it, green) mirroring the Catalog list pattern — reused query pipeline, RFC-9457 middleware,
`ArabicSearchText`/`SearchTextInterceptor`/`SearchableText`, pagination, and the `PRD-`-style PG sequence
(as `PUR-`). Header-only `PurchaseInvoice` aggregate born Draft (BR-PUR-003); **Received/Cancelled
transition methods deferred** to the receiving slice (non-Draft states seeded directly — the Catalog
`MarkDisabled` precedent). No new architecture, no new library. One new UI-kit primitive `VfDateInput`
(date-range filter) — owner-approved. Dev seed: config-gated (`Database:SeedDevelopmentDataAtStartup`),
**Development only, idempotent, never tests/prod** (DEC-PUR-001) — leaves 5 sample rows in the dev DB.

**Gates (independently re-run, not trusted from prior reports):** `dotnet build` 0/0 Release · `dotnet
format` clean · backend **220** (Domain **79** · Architecture **62** · Integration **79**) · frontend
**109** (+11) · ESLint + Stylelint clean. **Live-verified (headless Chrome, real stack — db :5434 + API
:5080 + ng serve :4200):** list at 1440 (table) and 390 (cards), **dir=rtl and zero horizontal overflow
at both**, 5 rows newest-first, all three badges (مسودة/مستلمة/ملغاة), search-narrows, no-match empty
state, sort-by-total reorders, filters drawer (status select + 2 date inputs render), **zero console
errors** everywhere. API curl checks: normalized supplier search, exact number, reference, status filter,
date range, sort, malformed-date → 400.

**Owner rulings applied (2026-07-17 final approval):** (1) **status sort → explicit lifecycle order
Draft→Received→Cancelled, recorded as TD-108, deferred to a joint consistency pass with Product Status**
(owner-sanctioned: implement both together to keep the system consistent — not a Slice-1 blocker);
(2) supplier reference kept as secondary metadata in the supplier column (documented in `ui.md`, not a
7th column); (3) **TD-107 kept open** — do not raise bundle budgets / relax gates / optimize prematurely
(the slice's eager i18n nudged the initial bundle to 508.14 kB, warning-only, error budget 1 MB, exit 0);
(4) dev seed, (5) query impl, (6) search semantics, (7) indexes — all approved as-is, no change.

**Doc corrections this session:** TS-PUR-001 fixed (five→six columns); supplier-reference display
documented (`ui.md`); state-machine/seed deferral documented (`business-rules.md` BR-PUR-003 +
`decisions.md` DEC-PUR-001); GLOSSARY «فاتورة شراء»/«رقم فاتورة الشراء»; `_INDEX` Purchasing row;
TD-108 added, TD-107 updated. Consistency review clean (no drift, no broken refs, no gate regression).

**Next (NOT started, per owner):** Purchasing Slice 2 — Purchase Details. Untracked and intentionally
not committed (pre-existing, prior sessions): `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Sprint 4 opened (2026-07-16) — Purchasing Slice 1 (Purchase List): DOCS APPROVED, DoR READY, NO CODE

**Sprint 4 objective (owner):** make inventory *real* — a clinic owner must be able to receive
products into inventory via purchase invoices. Optimize for **business value / MVP velocity, not
governance expansion** — no new ADRs/standards/playbooks unless implementation proves a real gap.
Build **small vertical slices**, stop after each for owner review. Recommended order: **1 Purchase
List · 2 Details · 3 Create Invoice · 4 Receive · 5 Inventory Update**.

**Owner scoping rulings (2026-07-16):** (a) **lightweight per-slice discovery** — draft minimal docs
per slice → owner approves → implement; (b) **supplier = free-text name** (no Suppliers module — future
capability, may replace the field without changing purchase identity); (c) **inventory ledger model**
(movement-ledger vs snapshot-balance — expensive to reverse) **decided at Slice 4**, not now.

**Purchasing module → Approved (Slice-1 scope only), owner sign-off in `acceptance.md`.** The DoR gate
(playbook Step 0) is now satisfied for Slice 1. Approved IDs:
- **REQ-PUR-001** — purchase list (search / filters / sort / pagination / 4 states).
- **BR-PUR-001** invoice header · **BR-PUR-002** `PUR-000001` numbering (generated on first draft
  persist, immutable, never reused, gaps OK, not editable, not partial-format-searchable — DEC-CAT-026
  pattern) · **BR-PUR-003** state machine (مسودة→مستلمة | مسودة→ملغاة; مستلمة/ملغاة terminal; forbidden:
  مستلمة→مسودة, ملغاة→مسودة, مستلمة→ملغاة) · **BR-PUR-004** list (6 frozen columns incl. Created At;
  search = number/supplier/ref, **not notes**; filters = status + date range; sort whitelist = number/
  invoice-date/supplier/status/total).
- **AC-PUR-001..003** · **DEC-PUR-001** (scope + header + ledger deferral + dev-seed 2–5 rows approved).
- Deferred (documented out-of-scope, not invented): line-items/cost → Slice 3 · receiving/stock → Slice
  4/5 · ledger ADR → Slice 4 · standalone Suppliers → future. Scope-lock list in `overview.md`.

**Owner APPROVED Slice-1 implementation (2026-07-17).** Implementation started but was **interrupted
during the context-study phase — ZERO code written.** Working tree = the 8 Purchasing docs (now Approved)
+ `_INDEX.md` row + this STATUS entry, plus the two pre-existing untracked items. Nothing committed.

**Resume point: `implementation.md` → Implementation step** (DoR ✅, Context Loading done). Mode = New
Feature (budget Medium ≤60k). Mandate = **reuse/mirror the Catalog Product-List pattern** (do NOT invent
a second list mechanism). Pattern studied this session — canonical **mirror sources** for the next session:

- **Backend query stack (the exact pattern to copy):** `Application/Catalog/Queries/ProductList/`
  (`ProductListQuery` — page/pageSize/sort/direction consts + `MaxPage` overflow guard · `…Validator`
  (FluentValidation, `ValidationMessageKeys`) · `ProductListItemDto` · `ProductListSortField` enum ·
  `ProductStatusFilter` enum) and the handler `Infrastructure/Catalog/ProductListQueryHandler.cs`
  (CQRS-lite projection, `ApplyFilters` → `ArabicSearchText.Normalize` + `EF.Functions.ILike` on
  `SearchableText.PropertyName`, `ApplySorting` whitelist **with `.ThenBy(row.Id)` total-order
  tiebreaker — R3**, `Skip/Take`). Shared: `Application/Common/{PagedResult,IQuery,IQueryHandler,
  SortDirection,MoneyDto,Currencies,ValidationMessageKeys}`.
- **Endpoint/parse:** `Api/Endpoints/Catalog/ProductEndpoints.cs` (`MapGet /api/v1/products` +
  `[AsParameters]`), `ProductListRequest.cs` (token dictionaries → `QueryStringParser`), `Api/Endpoints/
  QueryStringParser.cs`. Register the handler in `Infrastructure/DependencyInjection.cs`, validator in
  `Application/DependencyInjection.cs`, query pipeline in `Api/Composition/QueryPipeline.cs`, endpoint in
  `Program.cs`.
- **Domain + persistence (still to read next session):** `Domain/Catalog/Product.cs` (aggregate factory/
  invariant pattern), `Infrastructure/Catalog/InternalProductCode.cs` (**the `PRD-` PostgreSQL-sequence
  pattern to mirror for `PUR-000001` — BR-PUR-002**), `Persistence/Configurations/ProductConfiguration.cs`,
  `SearchTextInterceptor.cs` + `SearchableText.cs` + `ArabicSearchText.cs` + `NormalizedArabicName.cs`
  (search_text maintenance), `VetFlowDbContext.cs`, and a migration (`ProductWritePath`) for the sequence
  + index migration shape. Frontend mirror: `web/src/app/features/catalog/product-list/` (page/store/
  models/api/columns/components + specs) — the Purchase list is simpler (2 filters: status + date range).

**Purchasing-specific deltas from the Product-List pattern (per Approved docs):** new `Purchasing`
namespace/module (NOT inside Catalog); `PurchaseInvoice` aggregate = header only (BR-PUR-001) with a
3-state enum + state machine (BR-PUR-003, transitions enforced but only Draft is creatable in Slice 1 —
there is no create endpoint yet, so seed 2–5 rows in dev DB for verification per DEC-PUR-001); list
columns = 6 incl. CreatedAt; search = number/supplier/ref (NOT notes); filters = status + date-range;
sort whitelist = number/invoiceDate/supplier/status/total (BR-PUR-004). Snapshot-only entity — **no
stock, no line items, no receiving** (scope lock). Frontend route `/purchases` + shell nav «المشتريات»;
empty-state «لا توجد فواتير شراء حتى الآن» + non-functional CTA «إنشاء فاتورة شراء».

**Task list (this session, carried forward):** #1 study pattern (mostly done — backend query stack read;
domain/persistence + frontend still to read) · #2 backend build · #3 frontend build · #4 tests · #5
gates+self-review+owner report. **Stop condition unchanged: full gate + owner report, then STOP — do NOT
commit until owner review** (Sprint-4 rule: stop after every slice).

## Session close (2026-07-16, Task 4) — Managed Data slice DONE, owner-APPROVED, and COMMITTED (`9e5c99c` + `6263b43`)

**Owner review 2026-07-16: the whole four-task Managed Data slice is APPROVED and now COMMITTED**
(two commits: `9e5c99c` `feat(managed-data): Categories and Manufacturers management` — implementation
only; `6263b43` `docs(managed-data): synchronize repository after Managed Data slice` — STATUS/_INDEX/
ledger only, no mixing). **Not pushed** (no remote — owner item #4). Owner rulings this session:
duplication decision **approved** (leave it — do NOT build a ManagedData framework; rule of three
remains; **TD-106** stays open until a third managed entity exists); **TD-107 accepted as debt — do
NOT raise the warning budget** (future work must shrink the bundle, not relax the gate). No new business
decision — all behavior implements already-approved DEC-CAT-032 / DEC-CTG-002; no DEC/ADR owed.

**Task 4 — Manufacturers Managed Data (REQ-CAT-047/048, BR-CAT-052/053, DEC-CAT-032) — DONE, green,
live-verified.** This completed the Managed Data vertical slice (Tasks 1 Categories backend · 2
Categories frontend · 3 Product-Editor category active-only · 4 Manufacturers).

- **Architecture — mirrored Categories, did NOT abstract (owner directive: prefer copy over premature
  framework; rule of three).** Manufacturers now form a second near-identical managed-data stack. With
  only **two** occurrences, extraction was rejected — see the cross-cutting review below. Manufacturers
  live in the **Catalog** module (REQ-CAT), Categories in their own module; the copy keeps the module
  boundary clean (no cross-import, STD-FE-004 "mirror without importing").
- **Backend:** `Manufacturer` gained `IsActive` (default true) + `Rename`/`Activate`/`Deactivate`
  (BR-CAT-052/053). Reused the command pipeline: `CreateManufacturer`→`ICommand<Guid>`;
  `RenameManufacturer`/`SetManufacturerActive`→`ICommand<Guid?>` (null⇒404); shared
  `ManufacturerNameCommandValidator<T>` base. `ManufacturerListQuery` (normalized-Arabic search, sort
  whitelist name/status + `.ThenBy(Id)`, page cap). Name uniqueness in the handler (per-field 400 →
  `validation.manufacturer.name.duplicate`) **plus** a unique btree index `ix_manufacturers_name_unique`
  on the shared `search_text` column (reuse safe — manufacturers are Arabic-name-only, BR-CAT-007) with a
  `DbUpdateException` 23505 catch for the concurrent-insert race. **`GET /api/v1/manufacturers` repurposed
  to the management list** (`{id,name,isActive}` superset — the product-list filter and editor consumers
  keep working); `POST` create, `PUT {id}` rename, `POST {id}/activate|deactivate`. The old
  `ManufacturerOptionsQuery`/validator/handler were **deleted** (its two consumer tests survive on the
  superset). Migration `20260716131729_ManufacturersManagedData` (`is_active` default **true** +
  the unique index; default true so existing product-referenced manufacturers stay active — BR-CAT-052).
- **Frontend:** new `web/src/app/features/manufacturers/` (mirrors `features/categories/` without importing
  it): models · `ManufacturersApiService` · `ManufacturerListStore` · `ManufacturerListPageComponent` +
  components `manufacturer-table`/`-cards`/`-status-badge`/`-list-skeleton`/`-form-dialog`. Route
  `/manufacturers` (lazy), shell nav «الشركات المصنعة» (pi-building), `ar.ts` `manufacturers.*`. Duplicate
  name → local Arabic message (branch on `VTF-VAL-001` + `errors.name`, STD-FE-037). No optimistic UI.
- **Editor:** `ManufacturerOption` gains `isActive`; `manufacturerOptions()` typed to it; new
  `manufacturerSelectOptions` computed = active only + current inactive preserved (tagged
  «(غير نشط)» via `editor.manufacturer.inactiveSuffix`) — a **deliberate copy** of `categorySelectOptions`
  (two call sites, one file; rule of three → keep the copy). Manufacturer select now active-only.
- **Tests green — all gates independently re-run:** backend **192** (Domain **65** · Architecture **61**
  · Integration **66**), frontend **98** (+25). New: ManufacturerTests (domain), ManufacturerManagementTests
  (integration — named by REQ/BR/AC since no manufacturer TS-CAT scenarios were authored; No Speculation),
  a PUT-keeps-inactive-manufacturer guard test, mirrored frontend specs, and 5 editor manufacturer
  active-only tests. Build 0/0 Release · `dotnet format` clean · ESLint + Stylelint clean.
- **Frontend build finding (not a gate failure):** `ng build` succeeds but the initial JS crossed the
  **500 kB *warning* budget → 503.56 kB** (error budget is 1 MB — build exits 0). Cause: the slice's
  eager `ar.ts` i18n additions (categories + manufacturers copy). **Owner decision needed** — raise the
  warning budget, or lazy-load per-feature i18n. Recorded as open item 11 / friction F8.
- **Live-verified (headless Chrome via CDP, real stack — db :5434 + API :5080 + ng serve :4200) — the
  FULL owner checklist driven in the browser, not just rendered:** manufacturers list at **1440** (table)
  and **390** (cards, inactive row «غير نشط»), **dir=rtl and zero horizontal overflow at both**. Every
  interaction was actually driven: **create** (dialog → type → save → row appears), **rename** (row action
  → dialog → save; dialog closed on success and the API confirms the renamed row persisted), **activate**
  (badge غير نشط→نشط) and **deactivate** (badge نشط→غير نشط) via row actions, **search** (filters to one
  row + «1–1 من 1»), **sorting** (name-header toggle reorders the first row), **pagination** («1 / 2» →
  «2 / 2»). Product-editor **create** page shows the manufacturer select with the deactivated manufacturer
  **absent** (active-only). **Edit-preserve — the signature rule (DEC-CAT-032 option B):** seeded a product
  on an active manufacturer, deactivated it, opened `/catalog/products/{id}/edit` — the editor shows that
  manufacturer **visible, selected, and tagged «غير نشط»**, RTL, zero overflow. **Zero console errors on
  every page.** Note: the test manufacturers/product/category seeded during verify **remain in the dev DB**
  — the cleanup DELETE was blocked by the safety classifier and there is no delete endpoint by design
  (harmless dev data incl. a few garbled-name rows from an earlier bash-UTF-8 mishap; nothing committed).

### Cross-cutting duplication review (the five owner questions) — verdict: LEAVE THE DUPLICATION
1. **Backend extraction warranted?** **No.** Two managed-data stacks exist (Categories module ·
   Catalog manufacturers). A shared `ManagedLookup`/generic name-validator/uniqueness base would couple
   two modules through a framework and add generic indirection (per-entity DbSet/index-name/message-key
   config) — a net complexity increase for only two occurrences. Genuinely shared mechanics are *already*
   shared (command pipeline, `LookupOptionsQuery` base for units/natures, `ArabicSearchText`,
   `SearchableText`, `QueryStringParser`, `SearchTextInterceptor`); what is copied is the per-entity CRUD
   surface — exactly what should be copied at N=2.
2. **Frontend extraction warranted?** **No.** The two feature folders are near-identical, but STD-FE-004
   ("mirror without importing") is the established convention and both are module-local; a generic
   managed-list component/store at N=2 is premature.
3. **Improves readability without indirection?** **No** for the feature/stack extraction (adds
   generics/config). The one borderline spot is the editor's two `*SelectOptions` computeds (same file,
   15-line exact-shape dupe) — a local `activeOnlyWithCurrent()` helper would be low-indirection, but at
   two call sites the owner directive (prefer copy) wins; it becomes worthwhile at a third.
4. **Reduces future maintenance cost?** **No, today.** Categories and Manufacturers have independent
   BR ids and may diverge (manufacturers could later get English names or audited rename while categories
   may not); premature coupling would raise, not lower, maintenance risk.
5. **Violates the Simplicity Budget?** **Yes** — a cross-module managed-lookup framework at N=2 is the
   "grow the architectural surface without measurable value" that ADR-0014 §12 / principle 14 prohibit.
**Rule-of-three trigger recorded:** the NEXT managed-data entity (Suppliers, ProductNature management, …)
is the point to re-evaluate a shared "Managed Lookup" abstraction — and even then weigh the cross-module
coupling cost. Logged to the tech-debt ledger as the watch item.

### Final whole-slice review (all four tasks together)
✓ Categories Backend · ✓ Categories Frontend · ✓ Product-Editor Integration (category + manufacturer
active-only) · ✓ Manufacturers Backend · ✓ Manufacturers Frontend. **No correctness findings.** Every
BR/REQ/AC/DEC id in code matches the approved docs; no invented business logic; module boundary intact
(Manufacturers in Catalog, Categories in its own module, no cross-import); the repurpose-and-delete
mirrors Task 1 exactly with no orphaned query. Real findings: (a) the managed-data duplication rule-of-three
watch (above); (b) the bundle-budget *warning* (open item 11 / F8); (c) Accept-Language (open item 10 / F7,
owner already deferred to a future infra task — both dialogs use local Arabic copy). Nine-question
self-review: **all NO** (no principle/ADR/standard breach; the duplication is deliberate per owner
directive and not an unnecessary-complexity violation; boundary intact; no invented logic; docs synchronized;
no ADR owed — reusing an endpoint client-side + copying a per-entity CRUD surface are cheap-to-reverse
engineering details already covered by DEC-CAT-032 / DEC-CTG-002).

**Owner approved → committed (`9e5c99c` + `6263b43`); not pushed. Next slice recommended below
(Inventory) but NOT started, per owner instruction.**

## Session close (2026-07-16, Task 3) — Product-Editor active-only integration DONE, self-reviewed & live-verified, awaiting owner review, NOT committed

**Task 3 — Product Editor integration (REQ-CTG-005 / DEC-CTG-002 / AC-CTG-005) — DONE, green, live-verified.**
Frontend-only integration (plus one backend guard test). No editor redesign, no new UI pattern, no new
abstraction. The slice stays uncommitted until Task 4 (owner rule — one vertical cut).

- **Architecture decision — reused the existing `GET /api/v1/categories`; did NOT add a
  `/categories/options` endpoint** (diverges from the earlier STATUS plan, per the owner's Task-3
  directive "prefer extending an existing query / justify any new endpoint"). Justification: the list
  already returns `{id,name,isActive}`, and edit mode must show the product's **current inactive**
  value — which an active-only endpoint structurally cannot return in one call. So the editor filters
  client-side; no new query/handler/DI/registration (zero duplication).
- **Frontend:** `CategoryOption` model gains `isActive`; `categoryOptions()` typed to it; generalized
  the private `lookupSignal<T>` (no parallel path). New `categorySelectOptions` computed = **active
  categories only, plus the current value if it is inactive** (tagged «(غير نشط)» via new editor-scoped
  key `editor.category.inactiveSuffix` — kept out of the `categories.*` namespace, STD-FE-004). Logic is
  **mode-agnostic**: a create form starts with no category so it only ever offers active; edit prefills a
  possibly-inactive value that stays selectable until the user picks another, after which it disappears
  and cannot be re-chosen (BR-CTG-005). Manufacturers untouched — active-only there waits for Task 4.
- **Backend:** no production change. Added ONE integration test locking the guarantee that a PUT which
  keeps a now-inactive category returns **204** (historical reference never forced to change).
- **Tests green:** backend **168** (Domain 54 · Architecture 59 · Integration **55** — +1); frontend
  **73** (+4: create active-only TS-CTG-006; edit shows+saves inactive TS-CTG-007; switch drops inactive).
  Build 0/0 Release · `dotnet format` clean · `ng build` clean · ESLint + Stylelint clean.
- **Live-verified (headless Chrome, real stack — db + API :5080 + ng serve :4200):** create dropdown =
  active only (`أعلاف بيطرية`, `مستلزمات جراحية`; inactive `أدوية` hidden); edit of a product on the
  inactive `أدوية` shows **`أدوية (غير نشط)` in the CLOSED select** and offers it + the actives; switching
  to an active value **drops** the inactive from the reopened dropdown; **save unchanged** lands on
  Details still showing `أدوية` (value preserved). RTL; zero overflow at 1440 & 390; **zero console errors**.
- **Nine-question self-review: all NO** (no principle/ADR/standard breach; no duplication; boundary intact;
  minimal; no invented logic; docs already describe this exactly — REQ/BR/AC/DEC approved 2026-07-16; no
  ADR owed — reusing an endpoint client-side is a cheap-to-reverse engineering detail).
- **Cross-cutting (Accept-Language):** Task 3 surfaces **no new** server-localized text, so no active bug.
  Recommendation recorded under open item 10 — classify as a **reusable infrastructure improvement**
  (single `/core` `Accept-Language: ar` interceptor), **pending owner approval; not implemented**.

**Remaining: Task 4 — Manufacturers backend + frontend** (mirror the Categories pattern; add `IsActive`,
then active-only in the editor's manufacturer select — REQ-CAT-047/048, BR-CAT-052/053, DEC-CAT-032).
Commit only after Task 4.

## Session close (2026-07-16, later) — Managed Data Tasks 1 & 2 implemented (backend + frontend), owner-approved, NOT committed

The Managed Data slice resumed from DoR-READY and produced the first two of four tasks. **Owner
rule: the slice is ONE vertical cut — commit only after all four tasks** (1 Categories backend ✓ ·
2 Categories frontend ✓ · 3 Product-Editor active-only integration · 4 Manufacturers
backend+frontend). Nothing committed or pushed this session. Both tasks were owner-reviewed and
approved; a mid-slice checkpoint was taken after Task 1 (backend) before Task 2 (frontend).

**Task 1 — Categories backend — DONE, owner-approved, green.**
- Domain: `Category` gained `IsActive` (default true), `Rename`, `Activate`/`Deactivate`.
- Reused the command pipeline: `CreateCategory`→`ICommand<Guid>`; `RenameCategory`/`SetCategoryActive`
  →`ICommand<Guid?>` (null⇒404); shared `CategoryNameCommandValidator<T>` base (no duplication).
- `CategoryListQuery`: normalized-Arabic search, sort whitelist name/status + `.ThenBy(Id)`, page cap.
- Name uniqueness (BR-CTG-003) enforced **in the handler** (validators are singletons — no scoped
  DbContext): normalized pre-check → per-field 400 (`errors.name` / `validation.category.name.duplicate`),
  **plus** a unique btree index `ix_categories_name_unique` on the shared `search_text` column
  (reuse safe only because a category is Arabic-name-only — BR-CTG-001) as the DB backstop, and a
  `DbUpdateException` 23505 catch mapping the concurrent-insert race to the same 400.
- Endpoints: `GET /api/v1/categories` **repurposed to the management list** (`{id,name,isActive}` —
  a superset, so the existing product-list/editor consumers keep working); `POST` create,
  `PUT {id}` rename, `POST {id}/activate|deactivate`. The old `CategoryOptionsQuery` was **deleted**
  (Task 3 adds an active-only `/categories/options` endpoint when the editor needs it).
- Migration `20260716110003_CategoriesManagedData` (`is_active` default **true** + the unique index).
- **Backend 167 tests green** (Domain 54 · Architecture 59 · Integration 54 — Testcontainers;
  144 → +23), incl. a handler-bypassing test proving the unique index bites and normalization-variant
  duplicate tests. Build 0/0 Release; `dotnet format` clean.

**Task 2 — Categories frontend — DONE, verified live, green.**
- New `web/src/app/features/categories/` module (mirrors the catalog list patterns without importing
  them — STD-FE-004): models · `CategoriesApiService` · `CategoryListStore` (read state + `refresh()`) ·
  `CategoryListPageComponent` (smart) · components `category-table`, `category-cards` (mobile),
  `category-list-skeleton`, `category-status-badge`, `category-form-dialog` (VfDialog + typed form).
- Four data-view states, `VfSkeleton` loading, search/sort/pagination, create/rename dialog,
  activate/deactivate as labelled row actions, **no optimistic UI** (refresh from server — STD-FE-036).
- Duplicate name surfaces a **local Arabic** message (branch on errorCode `VTF-VAL-001` + `errors.name`,
  never message text — STD-FE-037); chosen after live testing showed the server localizes by
  `Accept-Language` (English browser → English text). Routing `/categories` (lazy), shell nav
  «التصنيفات», `ar.ts` `categories.*` copy.
- **Frontend 69 tests green** (+40; 15 files). `ng build` clean; ESLint + Stylelint clean; no physical
  CSS, no `console.*`/`any`/`!`.
- **Live (headless Chrome, real stack — db + API :5080 + ng serve :4200):** RTL; empty state;
  create/rename/deactivate/activate via the UI; search; duplicate→Arabic error (dialog stays open);
  **zero horizontal overflow at 1440 and 390**; mobile cards; **zero application/JS console errors**
  (only the browser's expected network log of the intentional duplicate 400).

**Uncommitted working tree** = Task 1 backend + Task 2 frontend, plus the pre-existing untracked
`docs/releases/` and `docs/ui/product-editor-ux-architecture.md`. **Resume at Task 3.** No new
business decision was made this session — all behavior implements already-approved DEC-CTG/BR-CTG and
DEC-CAT-032; the implementation choices above are engineering details (cheap to reverse, no doc
contradiction), so no `DEC`/ADR is owed. One owner decision is deferred (Accept-Language — open item 10 / friction F7).

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

**DoR status: READY → Tasks 1 & 2 now IMPLEMENTED** (Categories backend + frontend — see the top
"Session close (2026-07-16, later)" block). **Remaining: Task 3** (Product-Editor active-only
integration — REQ-CTG-005 / DEC-CTG-002) **and Task 4** (Manufacturers backend+frontend —
REQ-CAT-047/048, BR-CAT-052/053, DEC-CAT-032). The slice is committed only after all four tasks.

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

**Managed Data slice COMPLETE and COMMITTED (`9e5c99c` impl + `6263b43` docs), owner-approved, not
pushed.** All four tasks (Categories backend/frontend, Product-Editor category active-only, Manufacturers
backend/frontend + editor manufacturer active-only) are done, green (290 tests: backend 192, frontend 98),
and live-verified. Two managed-data stacks kept as deliberate copies (Categories module · Catalog
manufacturers) — rule of three (TD-106) governs any future extraction.

**Recommended next slice — Inventory (stock-ledger foundation), NOT started (owner picks; do not start
unprompted).** Rationale (full comparison in the 2026-07-16 owner report): Inventory is the topological
linchpin of the commercial cycle — it sits directly on Catalog (products + units, done) with **no unmet
upstream dependency**, and Purchasing/Sales/Monitoring/Batch/Reports all depend on it (very high reuse).
Scope the first cut minimally to **stock-on-hand + manual adjustment / opening balance** (the Catalog
Slice-1/2 "start minimal" discipline). **Documentation-First gate applies:** Inventory is "Not documented"
— the slice must begin with a discovery→owner-approval pass **plus one ledger-model ADR** (snapshot-balance
vs. movement-ledger — expensive to reverse), i.e. docs + ADR before any code. Runner-up: **Audit** (no
upstream deps, high reuse, ideally precedes the first write-heavy transactional module) — blocked on the
audit record-shape decision (open item 2 / DEC-CAT-028), which can be decided in parallel. **Auth** and
**Images** are owner-blocked (mechanism / storage undecided). **Sales** is highest-value but most
downstream (needs Inventory + Customers + Cash).

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
10. **Server localization vs. Arabic-only UI (Task 2 finding + Task 3 recommendation).** The API
    localizes user-facing messages by `Accept-Language` (ADR-0007); a non-Arabic browser gets English
    text. No active bug (Categories dialog and the editor use local Arabic copy; Task 3 surfaces no new
    server text). **Task-3 architectural recommendation (owner decision needed; NOT implemented):**
    classify this as a **reusable infrastructure improvement**, not a per-message local workaround —
    the recurring, DRY fix is a **single app-wide `Accept-Language: ar` request interceptor in `/core`**
    so the server's existing ADR-0007 translation point returns Arabic to match the UI, covering every
    future server-message surface (incl. generic RFC-9457 fallbacks) in one place. Low-risk, additive,
    ~10 lines, one location. It touches cross-cutting infra, so it needs **explicit owner approval**
    before implementation (do not add silently). Alternative (keep local copy per message) rejected:
    duplicative and leaves unmapped/infra messages in English. See friction F7.
11. **RESOLVED (owner ruling 2026-07-16) — Frontend initial-bundle budget warning.** `ng build` succeeds
    but the initial JS is 503.56 kB vs the 500 kB `maximumWarning` (error budget 1 MB). **Owner ruled:
    accept as technical debt (TD-107); do NOT raise the warning budget — the warning is intentional, and
    future optimization must reduce the bundle, not relax the gate.** No further action pending; TD-107
    tracks the eventual reduction (e.g. lazy per-feature i18n). See friction F8.

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
| F7 | 2026-07-16 | The API localizes user-facing messages by the `Accept-Language` header (ADR-0007), but the UI is Arabic-only with no language switcher; a browser defaulting to English receives English server text. Surfaced when the Categories dialog first displayed a server validation message (Task 2). Worked around with local Arabic copy for the duplicate message. | 2 | Add an app-wide `Accept-Language: ar` HTTP interceptor so the server's single translation point matches the Arabic UI (owner decision — open item 10). Owner **approved as a future cross-cutting infra task** (Task-4 review); do not implement in the Managed Data slice. |
| F8 | 2026-07-16 | The Managed Data slice's eager `ar.ts` i18n additions pushed the frontend initial bundle to 503.56 kB, crossing the 500 kB `maximumWarning` budget (error budget 1 MB — build still passes). The whole app's UI strings live in one eagerly-loaded `AR` object. | 1 | Owner decision (open item 11): raise the warning budget, or split i18n so each lazy feature ships its own strings. If a third+ feature repeats it, prefer the lazy-i18n split. |
