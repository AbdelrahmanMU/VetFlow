# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-07-31 (**Sprint 7 «Sales MVP» — THE FRONTEND SLICE IS FINISHED AND THE WHOLE SPRINT NOW PASSES EVERY GATE FOR REAL: build 0/0 · format clean · Domain 126 · Architecture 92 · Integration 182 · no pending model changes · ESLint + Stylelint clean · frontend 177 · `ng build` exit 0 · live-browser verification done at 1440 and 390 with zero overflow and zero console errors.** Two gates failed first and were **corrected, never bypassed**: `dotnet format` (LF endings across every Sprint 7 file) and three 30-day-boundary integration tests that still derived «today» from UTC while the handlers use `IClinicClock` — the tests were wrong, the production code was right, and the clinic time-zone id now lives in exactly one place. **Nothing committed, nothing pushed. CORRECTION to the previous handoff: the tree is TWO change sets, not three — Sprint 6 can no longer be committed alone because its handlers and tests now depend on `IClinicClock`, a Sprint 7 type, so a Sprint-6-only commit would not compile.** **The owner's five answers arrived unfilled, so no action was taken on any of them — all five remain open.** **Sprint 7 module docs are still `Draft`, which blocks the push gate (ADR-0017 §7), not the commit gate.** Prior line: **IMPLEMENTATION STARTED under owner approval and STOPPED MID-SLICE at the owner's `/close-session`. BACKEND COMPLETE AND FULLY GREEN; FRONTEND INCOMPLETE AND THE `ng build` IS RED.** Docker became available this session, so **every backend gate ran for real**: build 0/0 · Domain **126** · Architecture **92** · Integration **182** (including the **18 Sprint 6 tests that had never been executed**) · `has-pending-model-changes` none. **Nothing committed, nothing pushed.** The five slices exist end-to-end on the server side — Sales aggregate + draft lines + commit, the Inventory consumption contract, FEFO allocation with expired-batch exclusion, sale-line-level traceability, and per-batch concurrency detection. The **Angular sales feature is half-written**: models, API services, stores and the create page exist; **the details page and its three components do not**, no i18n keys, no nav entry, no route registration — the frontend therefore **does not compile**. See the session section immediately below for the exact remaining file list. Prior line: **ALL OWNER DECISIONS APPLIED; NO BLOCKING DECISION REMAINS. Documentation-only, NO CODE, owner STOP.** Final four closed: customer = optional free text (DEC-SAL-002) · money rounding Sales-scoped, quantities never rounded (DEC-SAL-004) · clinic local date from **one configured system-wide time zone**, UTC/server/device forbidden (**BR-INV-060**) · **`BR-CAT-020` AMENDED — stock unit must be the smallest measurable unit, reversing its previous "not required to be smallest" clause (DEC-CAT-033)**. Flagged: existing product configs may violate the amended rule and need correction; BR-INV-059 still invalidates UTC date logic in three implemented handlers. Decision-log audit at close found **one gap and closed it: `DEC-INV-026`** now records the R4 expiry-boundary/Clinic-Local-Date ruling, which had existed only as business rules. **Sprint 7 documentation is IMPLEMENTATION READY; nothing committed; owner STOP in force.** Prior line: architecture review rulings applied (11). New: **BR-INV-058** (stock unit = smallest measurable unit, exact conversion, **no quantity rounding**) · **BR-INV-059** (**Clinic Local Date** is the business date basis, UTC prohibited; `ExpiryDate` = last saleable day — also governs BR-INV-013/022/033/036) · traceability raised to **Sale Line level** · concurrency scoped **per Batch** · R5/R7/R9/R10/R11 accepted and recorded. **Flagged: BR-INV-059 invalidates the UTC date logic in the already-implemented Projection/Batch Viewer/Expiry Monitoring handlers, and no clinic-timezone source is documented.** Prior line: **Sprint 7 owner review applied, documentation-only.** 8 rulings applied across 17 docs: expired stock excluded from FEFO (DEC-INV-021) · concurrency-conflict detection required, mechanism left to implementation (DEC-INV-023) · DEC-INV-024 removed → **REQ-INV-008** traceability · isolation boundary approved (DEC-SAL-006/DEC-INV-019) · price snapshot only (DEC-SAL-003) · IsSplittable honored (DEC-SAL-007) · open package out (DEC-SAL-008) · Inventory History still deferred, roadmap note only (DEC-INV-025). New: REQ-INV-008 · BR-INV-056/057 · AC-INV-045/046/047 · TS-INV-050..053 · AC-SAL-012 · TS-SAL-014. **6 owner decisions still open (DEC-SAL-002/004/005/009 · DEC-INV-020/022).** Prior: DoR drafted, 8 Sales docs from placeholders. Prior line: **Sprint 6 — DoR APPROVED + Batch Viewer & Expiry Monitoring IMPLEMENTED (code-complete), Inventory History DEFERRED.** Non-Docker gates green (build 0/0 · arch 76 · domain 101 · frontend 156 · format/ESLint/Stylelint clean · no schema change). **Docker unavailable → integration tests (18, written+compiling, UNRUN), live-browser & performance capture NOT done → commit gate NOT yet satisfiable.** **NOTHING committed, NOT pushed.** Prior: Batch Viewer DoR approved 2026-07-30; Slice 1 Projection COMMITTED, not pushed.)

## Governance ruling (2026-07-31) — **Continuous Capability Mode** adopted by the owner

**The unit of implementation is now the approved Epic — not the slice, the screen, or the capability.** Inside an Epic the AI continues automatically between capabilities, verifying after each and fixing immediately, and never waits for the owner between them. It stops only at the Epic's **seven conditions** (Epic implemented · all tests · architecture tests · browser verification · performance verification · self review · **Epic Owner Report**), then waits for **Epic Commit Approval** — no commit, no push.

**Recorded in four places, because conversation history is never a source of truth:**
- **ADR-0017 §11 amended in place** — *"completion of a feature slice"* **superseded** by *"completion of an approved Epic"*, with the supersession recorded (the BR-CAT-020 precedent; not annulled, not renumbered). New **§11a** enumerates the seven stop conditions. ADR-0017 was **`Proposed`**, not Accepted, so this is an ordinary amendment, not a governance breach.
- **`.claude/rules/workflow.md`** — new Continuous Capability Mode section (loaded every session).
- **`.claude/rules/ai-governance.md`** — review-checkpoint line updated, pointing at the rule and the ADR.
- **`.claude/playbooks/implementation.md`** — *"slice spans more than one module"* is now a **splitting instruction inside an approved Epic, and still a stop outside one**. (The owner had to authorize this edit explicitly; the first two attempts were refused by the permission classifier as gate-weakening.)

**What did NOT change, stated explicitly in all four so it cannot be misread later:** a **failing gate still stops everything** — "continue without waiting" never means continue past red · the Definition of Ready still gates every capability · nothing is invented to preserve momentum · `budget > Medium` still means **split**, never widen. **Owner escape hatch:** an explicit request for a design review returns that work to slice-by-slice review.

## Session close (2026-07-31) — Sprint 7 «Sales MVP»: **frontend slice FINISHED · every gate executed green · nothing committed**

**Owner directed the frontend to completion and a satisfiable commit gate, without stopping between steps. Done.** The seven missing pieces from the previous handoff exist, `ng build` exits 0, and **every gate in the sweep ran for real** — no gate was downgraded to "verified by inspection".

**The owner's five answers arrived as unfilled `[YES / NO]` placeholders.** None of them gates the frontend, so nothing was assumed: items 1 (flip doc statuses), 2 (BR-INV-058 at receiving), 4 (promote a mechanism to DEC/ADR) and 5 (BR-CAT-020 audit) each require a YES to act and **no action was taken on any of them**; item 3 (commit Sprint 6 separately) is a **proposal** in the report, since nothing may be committed without approval anyway. **All five remain open.**

### Delivered

- **`sale-details-page.component.ts`** — the frozen BR-SAL-008 order (number + badge → invoice facts → line items → notes), four view states with a distinct not-found, actions by status (draft: add/remove/commit, commit disabled with zero lines; committed: none). **No «back to list» action** — DEC-SAL-005 is open and no list was invented; the not-found state offers «إنشاء فاتورة بيع».
- **`components/sale-line-items.component.ts`** — table ↔ stacked cards, server-derived total, draft-only add/remove.
- **`components/add-sale-line-dialog.component.ts`** — product picker → sale units with the default auto-selected, **price displayed read-only** (DEC-SAL-003 — the payload carries no price), quantity, line-total preview, and a **field-level rejection of a fractional quantity for a non-splittable product** (DEC-SAL-007 — the one place mirroring purchasing was not enough; nothing is rounded).
- **`components/commit-sale-dialog.component.ts`** — mandatory confirmation carrying `ui.md`'s exact sentence, and a per-reason refusal message: retry offered **only** on concurrency conflict; insufficient stock names the products and **never says the balance is zero** (DEC-INV-021); inexact conversion names the line.
- **~90 `sales.*` / `saleCreate.*` / `saleDetails.*` / `nav.*` keys** in `core/i18n/ar.ts`; the `sales` route in `app.routes.ts`; a «المبيعات» nav group pointing at `/sales/new`.
- **29 frontend specs** (5 details store · 11 lines store · 5 create page · **8 add-line dialog**). Frontend total **156 → 185**.
  The dialog specs were added after the first Owner Report closed the one gap it declared: the splittability rejection was wired but never executed by a test. They now cover the **auto-selected default sale unit**, **sale-role units only**, **fractional quantity rejected for a non-splittable product with nothing emitted**, the same product accepting a whole quantity, a **splittable** product accepting 2.5, zero/negative rejection, the two-decimal line-total preview, and clearing the product.
- **One small correction to `sale-lines.store.ts`:** `classify` now falls back to `metadata['product']`, because the commit handler names an inexact conversion under `product` (singular) while insufficient stock uses `products`. Without it AC-SAL-013's message could never name the line.

### Gates — every one executed, all green

`dotnet build` **0 warnings / 0 errors** (Release) · `dotnet format --verify-no-changes` **clean** · **Domain 126** · **Architecture 92** · **Integration 182** · `ef migrations has-pending-model-changes` **none** · ESLint **clean** · Stylelint **clean** · frontend unit **185/185** · `ng build` **exit 0** (539.77 kB) · live-browser verification **done**.

**Two gates failed first and were corrected, not bypassed:**

1. **`dotnet format` failed on ~3 575 findings** — every Sprint 7 file had been written with LF endings against a CRLF repository. Fixed by **running the formatter**; whitespace only, no logic touched.
2. **Three integration tests failed** — `InventoryProjection`/`ExpiryMonitoring`/`BatchViewer` 30-day-boundary tests. **A real defect of the same family this sprint exists to fix:** they still derived `Today()` from `DateTime.UtcNow` while the handlers now use `IClinicClock`. Once Cairo rolled past midnight and UTC had not, the boundary was measured against the wrong day. **The production code was correct and was not touched.** `ApiFixture.ClinicToday` now resolves the date through the API's own `IClinicClock`, and all four call sites use it — including `SalesCommitEndpointTests`, which had been carrying its own hard-coded `"Africa/Cairo"`. **The time-zone id now exists in exactly one place.** This is a latent-failure class the tests could not have caught on any day when UTC and Cairo agreed.

### Live-browser verification (headless Chrome over CDP, real stack: db :5434 · API :5080 · ng :4200)

`/sales/new` and `/sales/:id` at **1440×900** and **390×844**: `dir=rtl`, **horizontal overflow 0 px everywhere**, **zero console errors** across every screen, dialog and transition. Verified end-to-end through the UI: frozen header order · «—» for an absent customer and «لا توجد ملاحظات» for absent notes · add-line dialog auto-selecting the default sale unit with the price shown and **no price input in the DOM** · the commit dialog's exact sentence · **commit → badge flips to «مُثبَّتة» and every action disappears** (AC-SAL-010) · a missing invoice reaching the not-found state. **BR-SAL-013 checked with a scoped scan of the Sales page and its dialogs: zero occurrences of دفعة/دفعات/صلاحية/FEFO/تخصيص.**

**Dev-database note:** verification created two sales invoices and **committed one**, which consumed one unit of dev stock. Pre-existing and unrelated: the dev seed products' Arabic names are stored as `????` (an encoding fault at seed time) — every other Arabic string renders correctly.

### Repository state — and a correction to the previous handoff

**Nothing committed, nothing pushed.** The previous section calls the tree **three separable change sets**. **That is no longer true, and the reason is not cosmetic.**

**Sprint 6 can no longer be committed on its own — it would not compile.** `BatchViewerQueryHandler` and `ExpiryMonitoringQueryHandler` take `IClinicClock` (last session's BR-INV-059 correction), and as of today their integration tests resolve the date through it too. `IClinicClock`, `ClinicClock`, `ClinicTimeOptions`, the `Clinic:TimeZone` setting, its startup validation and its DI registration are **all Sprint 7 files**. No hunk-splitting fixes a missing type. (`web/src/app/core/i18n/ar.ts`, `shell.component.ts` and `problem-details.ts` are also touched by both, but that is the lesser obstacle.)

**So the tree is two change sets:** documentation (22 modified `docs/…` paths + `GLOSSARY.md`) and code (**Sprint 6 + Sprint 7, atomic**). Splitting the code would mean reverting a correction BR-INV-059 requires. **Owner question 3 is therefore answered by fact rather than by preference.**

**R10 discharged:** the isolation architecture test now covers Sales in both directions — `AssertModuleIsIsolated("Sales")`, `ConventionTests.cs:167`.

### Doc statuses — **flipped to Approved (2026-07-31) by explicit owner ruling**

The push gate had been blocked by ADR-0017 §7 (no Draft document the implementation depends on). **The owner approved the flip and named the date.** Applied:

- **Sales — all 8 files** flipped to **`Approved (2026-07-31)`**.
- **Inventory** — `workflow.md` flipped; the six already-Approved Slice-1 headers (`overview` · `requirements` · `business-rules` · `acceptance` · `test-scenarios` · `ui`) **annotated, not overwritten**, so the 2026-07-22 Slice-1 approval survives alongside the Sprint 7 one. The Sprint 7 section header inside `overview.md` flipped too.
- **Catalog** — the four amended files annotated with the Sprint 7 amendment approval (DEC-CAT-033 / BR-CAT-020 / AC-CAT-049 / TS-CAT-038); their 2026-07-14 approval is untouched.
- **`_INDEX.md`** — Sales row **Draft → Implemented**, and the Inventory row's Sprint 7 marker likewise. **Zero occurrences of «ينتظر اعتماد المالك الصريح» remain.**

**Re-verified after the flip, all green:** build **0/0** · format **clean** · Domain **126** · Architecture **92** · Integration **182** · frontend **185** · ESLint **clean** · Stylelint **clean** · `ng build` **exit 0** (539.77 kB).

TD-107 unchanged: initial bundle **539.77 kB** against a 500 kB budget — a **warning**, not raised.

### Epic 1 — committed, **PUSH BLOCKED, NOT CLOSED**

The owner approved the push and every push-gate condition now passes. **The push did not happen** — a blocking external dependency, not a gate failure:

`git push origin main` **hangs indefinitely** (killed at 10 min, then again at 60 s with `GIT_TERMINAL_PROMPT=0`). `credential.helper = manager` — Git Credential Manager opens its own authentication window, which no automated session can see or answer, so `GIT_TERMINAL_PROMPT=0` does not make it fail fast either. `gh` is **not installed**. Read access works: `git ls-remote origin main` returns in seconds.

**Local state: 3 commits ready, `main` ahead of `origin/main` by 16.** `origin/main` is still at `6263b43`. Nothing was lost and nothing is half-pushed.

**The owner must authenticate.** Running `git push origin main` in their own shell — completing the credential prompt once — is enough; GCM will then cache it for later sessions. **An AI session must not enter credentials, so this cannot be worked around here.**

**Epic 1 is therefore NOT closed.** It closes the moment the push lands.

## Session close (2026-07-30) — Sprint 7 «Sales MVP» IMPLEMENTATION: **backend complete & green · frontend incomplete & RED · nothing committed**

**Owner approved Sprint 7 and directed implementation without stopping between phases; the session was ended by `/close-session` while the Angular slice was half-written.** This section is the handoff.

### What is DONE and verified

**Backend — all five slices, end to end.**
- **Sales domain** (`Domain/Sales/`): `SalesInvoice` aggregate (draft-born, derived total owned solely by the aggregate — BR-SAL-005), `SalesLineItem` (three snapshots: product name, sale-unit name, **catalog sale price** — BR-SAL-006/DEC-SAL-003; splittability enforced — DEC-SAL-007; line total rounded once, half away from zero — BR-SAL-007), `SalesInvoiceStatus` (Draft/Committed only — no «ملغاة», DEC-SAL-009 untouched), `SalesErrorCodes`.
- **Application** (`Application/Sales/`): create / add-line / remove-line / commit commands + validators, sale-details and sale-line-items queries. **Primitive-only** — no Catalog or Inventory type crosses into it.
- **Inventory consumption** (`Application/Inventory/` + `Infrastructure/Inventory/InventoryConsumptionWriter.cs`): the public contract `IInventoryConsumptionWriter` (mirror of `IInventoryReceiptWriter`), aggregation per product with per-line attribution preserved (BR-INV-046), **one ordered candidate query** with expired stock excluded **in the WHERE clause** (DEC-INV-021), sufficiency measured on saleable batches only, in-memory FEFO, staged decrements + on-hand decrease + traceability rows, no commit (BR-INV-048).
- **Commit path** (`Infrastructure/Sales/CommitSalesInvoiceCommandHandler.cs`): resolves products through the sanctioned Catalog read, converts each line **exactly** into the stock unit, calls the contract, transitions the aggregate, one `SaveChanges`. **Sales never sees a batch** (BR-SAL-013).
- **API**: `GET/POST /api/v1/sales-invoices`, `…/{id}`, `…/{id}/lines`, `DELETE …/lines/{lineId}`, `POST …/{id}/commit`. **No list endpoint** — not one of the five slices, not invented (DEC-SAL-005 still open).
- **Migration** `20260730201949_SalesAndInventoryConsumption`: `sales_invoices`, `sales_line_items`, `inventory_consumptions`, the `sales_invoice_number_seq` sequence. **Hand-edited deliberately** to drop the scaffolder's `AddColumn/DropColumn` for `xmin` — a PostgreSQL *system* column that cannot be created or dropped; the model snapshot still carries the property, so `has-pending-model-changes` is clean.
- **The three UTC handlers are corrected** (the carried-forward defect): Projection, Batch Viewer and Expiry Monitoring now take `IClinicClock`, not `TimeProvider`. **UTC no longer decides any business date** (BR-INV-059/060).

**Gates executed — all green.** `dotnet build` 0 warnings / 0 errors (Release) · **Domain 126** (+25) · **Architecture 92** (+16) · **Integration 182** (+58, zero failures) · `ef migrations has-pending-model-changes` = none.

**Docker came up this session** (Docker Desktop started without admin), which cleared the Sprint 6 blocker: **the 18 Batch Viewer / Expiry Monitoring integration tests ran for the first time.** 17 passed as written; **one was defective and was fixed** — see below.

### Implementation mechanisms chosen (the owner reserved these for implementation; recorded here, none invented as business rules)

1. **Concurrency detection = PostgreSQL `xmin` as a row-version token on `InventoryBatch`.** Scope is exactly **per batch** as ruled (R6): EF puts `xmin` in the WHERE of every batch UPDATE, so only a change to an **allocated** batch fails the sale; a concurrent sale on another batch of the same product does not. No new field, no DDL. `DbUpdateConcurrencyException` → `VTF-INV-056` → 409 + retry.
2. **Traceability model = a new Inventory-owned entity `InventoryConsumption`** (batch, product, **sale line**, quantity, timestamp), written inside the same unit of work. `SaleLineId` is a plain `Guid` with no cross-module FK — the exact precedent of `InventoryBatch.PurchaseLineId`. **It is not a movement ledger** (no movement type, no source module, no screen); DEC-INV-015 stays deferred.
3. **Clinic local date = `IClinicClock` over one configured time zone.** `Clinic:TimeZone` in `appsettings.json`, seeded `Africa/Cairo`, **validated on start** — an absent or unresolvable zone refuses to boot; **there is no UTC fallback path in the code** (BR-INV-060).
4. **Exactness test for the conversion = multiply-back** (`converted × factor(stockUnit) == quantity × factor(saleUnit)`). Chosen over a remainder test, which would wrongly reject 2.5 units of a *splittable* product sold in the stock unit and so contradict DEC-SAL-007.
5. **Problem Details gained a `metadata` extension member** so a rejection can name the products that fell short (AC-INV-041 / AC-SAL-009 require naming them) without putting UI copy in the domain.
6. **`ProductUnitConversion` extracted** into `Infrastructure/Catalog/` and now shared by receiving and the sale commit — the conversion lives once, in the module that owns unit profiles. Receiving's behaviour is unchanged.

**None of these was written into a module `decisions.md` and no ADR was created** — every one is a mechanism the owner explicitly assigned to implementation ("الآلية تخصّ التنفيذ ولا تُقرَّر في التوثيق"). **Owner question 4 below asks whether any should now be promoted to a DEC or an ADR.**

### What is NOT done — the frontend, precisely

**Written:** `sales.routes.ts` · `sale-create/` (models, forms, api service, page) · `sale-details/` (models, `sale-lines.models`, both api services, `sale-details.store`, `sale-lines.store` incl. commit-rejection classification) · `components/sale-status-badge.component.ts`.

**Missing — the build fails on these:**
1. `sale-details/sale-details-page.component.ts` (referenced by `sales.routes.ts` → **TS2307**).
2. `sale-details/components/sale-line-items.component.ts`.
3. `sale-details/components/add-sale-line-dialog.component.ts` (product picker → sale units with the default auto-selected, **read-only price**, quantity, line-total preview).
4. `sale-details/components/commit-sale-dialog.component.ts` (**mandatory** confirmation; retry on concurrency conflict).
5. **All `sales.*` i18n keys** in `core/i18n/ar.ts` → **TS2345** on `sales.status.draft` today, and every other key after it.
6. The `sales` route in `app.routes.ts` and a nav entry in `shell.component.ts`.
7. Frontend unit specs for the two stores and the create page.

**`npx ng build` currently exits 1.** Frontend unit tests, ESLint, Stylelint and `dotnet format` were **not run** this session. Live-browser verification and the performance capture were **not done** — though `TS-INV-048` (constant query count) *is* covered by a real SQL-command-counting integration test, and the FEFO/atomicity/traceability behaviour is covered by the 182 green integration tests.

### Findings the owner must see

1. **`BR-INV-058` is not enforced at receiving.** The rule says it governs **both** movements, but enforcing it there would add a new rejection path to an approved, implemented slice the Sprint 7 brief does not name. The shared converter already computes the exactness flag and receiving deliberately ignores it, with a comment saying so. **Owner ruling needed** (question 2).
2. **A Sprint 6 test was defective**, found only because Docker finally allowed it to run: `BatchViewerEndpointTests.Default_order_is_receive_date_descending_tie_broken_by_batch_id_BR_INV_031` assumed three batches shared one receive instant, but the seeder stamped `DateTimeOffset.UtcNow` per call, so nothing ever tied and the tie-break was never exercised. **Fixed by pinning the instant** (a new optional `receivedAt` on `InventorySeeder.AddBatchWithProvenanceAsync`) — the test now actually tests BR-INV-031. **The production handler was correct and was not touched.**
3. **Residual risk — on-hand drift under one specific race.** Two sales committing concurrently against **different** batches of the same product can lose one on-hand decrement, because `ProductOnHand` carries no concurrency token — deliberately, since one would produce exactly the false failures R6 forbids. **This cannot cause overselling:** sufficiency is measured on `Σ RemainingQuantity` of saleable batches (BR-INV-052), never on `OnHandQuantity`, so the consequence is a display inaccuracy in the projection. It sits inside the accepted-risk table (R5: no BR-INV-005 reconciliation in Sprint 7).
4. **The Sprint 7 module docs are still marked `Draft`.** The owner declared documentation approved in the implementation brief, but no doc status line was flipped and `_INDEX.md` still reads Draft for Sales. **Not done unasked** (question 1).
5. **GLOSSARY sync remains deferred** and «المبيعات» is still not a GLOSSARY module name — unchanged from before this session.
6. Pre-existing and unrelated: the `_INDEX.md` **Purchasing** row still calls Slice 5 "not yet implemented".
7. **Two choices were made conservatively where a decision is still open, and neither was invented into a rule.** (a) **DEC-INV-022** — the insufficient-stock rejection names **only the products** that fell short; it does **not** report available-versus-required, because that detail is precisely what is unresolved. (b) **DEC-SAL-005** — with no sales list, `/sales` simply **redirects to `/sales/new`**, and the planned nav entry points at the create screen; **no list was built and no list route exists**. The navigational gap the decision describes is therefore real and visible, not papered over.

### Repository state

**Nothing was committed and nothing was pushed.** The tree now carries **three** distinct change sets: Sprint 7 documentation (from earlier sessions), **Sprint 6's Batch Viewer + Expiry Monitoring code — whose verification is now GREEN and which is therefore committable for the first time**, and this session's Sprint 7 implementation. A docs-only or Sprint-6-only commit must be staged path by path.

### Next — resume here

1. Write the four missing Angular files, the `sales.*` i18n keys, the route and the nav entry; then re-run `ng build`.
2. Frontend specs, then the full gate sweep: `dotnet format`, ESLint, Stylelint, frontend unit tests, `ng build`.
3. Live-browser verification of `/sales/new` and `/sales/:id` at desktop and mobile widths (RTL, zero horizontal overflow, no console errors).
4. Then, and only then, the commit gate.

### Open questions for the owner

1. **Flip the Sprint 7 doc statuses Draft → Approved** now (Sales/Inventory/Catalog headers + `_INDEX.md`), since the brief declared documentation approved?
2. **`BR-INV-058` at receiving** — enforce it now (it would reject receipts for products misconfigured against the amended BR-CAT-020), or keep it Sales-only this sprint?
3. **Sprint 6 is now verifiable and green.** Commit it as its own change set immediately, or hold everything until Sprint 7's frontend is finished?
4. **Should any of the six implementation mechanisms above be promoted** to a module `DEC` or an ADR — the concurrency token and the traceability entity are the two candidates — or do they stay implementation detail as ruled?
5. **DEC-CAT-033 follow-up:** existing product configurations still have not been audited against the amended BR-CAT-020. The commit path now **rejects** an inexact conversion at sale time, so a misconfigured product will surface as a failed sale rather than silently rounding. Audit now or on first failure?

## Final readiness rulings applied (2026-07-30) — Sprint 7 «Sales MVP»: **last 4 blockers CLOSED — documentation only, ZERO code, owner STOP**

**Owner ruled the four remaining blockers; all applied. Documentation only — no code, no migration, no DB change, no endpoint, no UI. Nothing committed.**

**Rulings applied (4):**
1. **Customer optional — `DEC-SAL-002` APPROVED.** Sales invoice carries an **optional free-text customer name**, mirroring the free-text supplier (BR-PUR-001). Invoice identity stays the system number (BR-SAL-002), so a future Customers module can replace the field without changing it. No new module, table, or migration. BR-SAL-001 / AC-SAL-002 already matched; markers flipped.
2. **Money rounding — `DEC-SAL-004` APPROVED (option A).** `BR-SAL-007` stands as a **Sales-scoped** rule: Round Half Away From Zero to exactly 2 dp, banker's rounding forbidden. **Scope narrowed explicitly to monetary values** — cross-module promotion recorded as a future item. **Quantities are never rounded** (BR-INV-058 / DEC-CAT-033); the two rules no longer overlap.
3. **Clinic Local Date source — `BR-INV-060` NEW** (+ AC-INV-050, TS-INV-056). The clinic local date derives from **one system-wide configured time zone** — a single value for the whole system (single-clinic deployment, same basis as DEC-INV-002). **Explicitly forbidden:** UTC, server/OS time, browser or device time, or anything varying per machine or session; **silent fallback to UTC is prohibited**. Storage/configuration mechanism left to implementation, and **no Settings screen is designed** — the Settings module stays undocumented and may surface the value later without changing the rule (same shape as free-text supplier pending a Suppliers module).
4. **Catalog Stock Unit rule — `DEC-CAT-033` APPROVED; `BR-CAT-020` AMENDED** (+ AC-CAT-049, TS-CAT-038). **This was not an addition — it was a reversal.** BR-CAT-020 previously stated the stock unit *"is not required to be the mathematically smallest, but rather the operationally most suitable"*, the direct opposite of R1. That clause is now **superseded** and the rule requires the **smallest measurable unit**, making every purchase/sale conversion exact. Amended in place with the supersession recorded — **BR-CAT-020 was not annulled or renumbered**, and no competing rule was added on the same subject. This is the **enforcement source** for BR-INV-058: a non-exact configuration can no longer be saved.

**IDs added (6):** `BR-INV-060` · `AC-INV-050` · `TS-INV-056` · `AC-CAT-049` · `TS-CAT-038` · `DEC-CAT-033`.
**IDs modified:** `BR-CAT-020` (amended, clause superseded) · `BR-SAL-001` / `BR-SAL-007` (markers + scope) · `DEC-SAL-002` / `DEC-SAL-004` (Draft → Approved) · `BR-INV-059` (source now points to BR-INV-060) · range references across both modules.
**Totals:** Inventory BR-INV-046..060 · AC-INV-037..050 · TS-INV-037..056. Sales AC-SAL-001..013 · TS-SAL-001..015. Catalog BR-CAT-054+ unused; new IDs are AC-CAT-049 / TS-CAT-038 / DEC-CAT-033.

**Implementation consequence recorded (not solved here):** **existing product configurations may violate the amended BR-CAT-020.** Any product whose stock unit is not the smallest unit — including dev seed data — must be found and corrected. **No migration or remediation path was designed**; DEC-CAT-033 records the obligation only.

**Carried forward, unchanged:** BR-INV-059 still invalidates the UTC date logic in the committed Projection and the code-complete Batch Viewer / Expiry Monitoring handlers (`InventoryProjectionQueryHandler.cs:30`, `BatchViewerQueryHandler.cs:29`, `ExpiryMonitoringQueryHandler.cs:31`) — correcting them is Sprint 7 implementation scope.

**No blocking decision remains.** Still open but **non-blocking, scope-widening only:** DEC-SAL-005 (Sales List) · DEC-SAL-009 (Cancelled state) · DEC-INV-020 (NULL-expiry placement, carried as nulls-last) · DEC-INV-022 (insufficient-stock message detail).

### Session close (2026-07-30) — decision-log audit + handoff

**Decision-log audit performed at close.** Every ruling made this session was checked against the decision-routing rule (module-scoped → that module's `decisions.md`). **One gap found and closed:** the **R4 ruling** (expiry boundary = last saleable day · Clinic Local Date · its source) had been captured **only as business rules** BR-INV-059/060 with **no decision record**, even though it changed the reference date of four already-Approved rules. Recorded as **`DEC-INV-026`** (new, contiguous after DEC-INV-025). All other rulings verified already recorded: DEC-SAL-001..009 · DEC-INV-019..026 (024 tombstoned) · DEC-CAT-033 · the accepted-risk table (R5/R7/R9/R10/R11) in `inventory/decisions.md`. **No ADR was created and none is owed** — every Sprint 7 decision is module-scoped, and the two candidates that could have required one (concurrency mechanism, traceability persistence model) were explicitly reserved for implementation by owner ruling.

**Final ID state.** Sales: REQ-SAL-001..003 · BR-SAL-001..013 · AC-SAL-001..013 · TS-SAL-001..015 · DEC-SAL-001..009. Inventory: REQ-INV-006..008 · BR-INV-046..060 · AC-INV-037..050 · TS-INV-037..056 · DEC-INV-019..026 (**024 tombstoned, reserved**). Catalog: BR-CAT-020 **amended** · AC-CAT-049 · TS-CAT-038 · DEC-CAT-033. **Validated mechanically: all ranges contiguous, zero duplicates, no Approved ID renumbered.**

**Repository state — read carefully, the tree is not clean.** **This session wrote documentation only**: 3 modules (`sales/` 8 files, `inventory/` 7, `catalog/` 4) plus `_INDEX.md` and this file. **Zero production code, zero migrations, zero schema changes, zero endpoints, zero UI written this session.**

**However the working tree ALSO still carries the uncommitted Sprint 6 code** — Batch Viewer + Expiry Monitoring (backend queries/handlers/endpoints, frontend features, 18 integration tests, DI + routing + i18n edits, `InventorySeeder`). **Untouched by this session**, unchanged since the Docker-blocked session, and **still uncommitted because its verification never ran**. So a `git status` shows modified/untracked `src/`, `tests/`, and `web/` paths that are **Sprint 6's, not Sprint 7's**. **Nothing was committed or pushed this session.** Do not mistake that code for Sprint 7 work, and do not commit it as part of Sprint 7.

**Next — do NOT start without explicit owner approval:**
1. **Owner approval to implement Sprint 7.** Documentation is Implementation Ready; the STOP is the only thing holding it.
2. When approved, the sprint **spans two modules**, exceeding the single-module New-Feature stop condition — **split per slice**.
3. **Three corrections belong to Sprint 7 implementation, not to a separate sprint:** (a) fix the UTC date logic in the three implemented handlers per DEC-INV-026; (b) audit and correct existing product configurations against the amended BR-CAT-020; (c) extend the isolation architecture test to cover Sales in both directions.

**Open questions for the owner:**
1. **Sprint 6 verification is still outstanding and unaffected by all of this** — the 18 integration tests, live-browser run, and performance capture remain unrun because Docker was unavailable. Do you want that cleared before Sprint 7 implementation starts, or folded into it?
2. **GLOSSARY sync** has been deferred since Sprint 6 and Sprint 7 added ~11 more terms, including **«المبيعات» — which is still not a GLOSSARY module name** while the New-Module pattern expects one. Sync before implementing, or keep deferring?
3. **DEC-SAL-005 / DEC-SAL-009 / DEC-INV-020 / DEC-INV-022** remain open. None blocks, but DEC-SAL-005 (no Sales List) leaves the details screen without a navigational entry point — worth a conscious yes/no.
4. **Should the Sprint 7 documentation be committed now** (docs-only commit, nothing to build), or held until implementation lands alongside it? **Note the coupling:** the Sprint 6 code sitting uncommitted in the tree is a *separate* change set — if you want a docs-only Sprint 7 commit, it must be staged path-by-path so Sprint 6's unverified code does not ride along.

## Architecture review rulings applied (2026-07-30) — Sprint 7 «Sales MVP»: **11 rulings APPLIED — documentation only, ZERO code, owner STOP**

**Owner reviewed the Architecture Readiness Review and ruled on all 11 findings; applied this session. Documentation only — no code, no migration, no DB change, no endpoint, no UI. Nothing committed.**

**Rulings applied:**
- **R1 (business rule) — `BR-INV-058` NEW.** Inventory must never rely on quantity rounding. **Stock Unit is always the smallest measurable unit**; purchase and sale units convert **exactly**; **no fractional residue**; **no quantity rounding policy exists**. Non-exact conversion is **rejected, never rounded** (BR-SAL-010/012, AC-SAL-013, TS-SAL-015). This removes the cumulative-drift risk against BR-INV-005 at its root. **+ AC-INV-048, TS-INV-055.**
- **R4 (approved) — `BR-INV-059` NEW.** `ExpiryDate` = **the last saleable day**; expired **iff `ExpiryDate < Clinic Local Date`**; **UTC is prohibited for business decisions**. Scoped as the module's single date-basis rule, so it also governs **BR-INV-013 / BR-INV-022 / BR-INV-033 / BR-INV-036** — their meaning is unchanged, only the reference date. **+ AC-INV-049, TS-INV-050 boundary case.**
- **R3 + R8 — traceability is a requirement, at Line level.** REQ-INV-008 / BR-INV-057 / AC-INV-047 / TS-INV-053 raised from sale-level to **Sale Line level**, so future Returns can identify which line consumed which batch. BR-INV-046 now states that **sufficient traceability information is a precondition of accepting a consumption request**, and that product aggregation for allocation **must not dissolve line attribution**. **+ TS-INV-054.** No persistence model prescribed.
- **R6 (approved) — concurrency validation scoped per Batch.** BR-INV-056 / AC-INV-046 / TS-INV-052 now state that only a change to an **allocated batch** fails the sale; a concurrent sale on a different batch of the same product does **not**. Prevents false-failure retry storms. Mechanism still undocumented per the earlier ruling.
- **R2 — recorded only.** DEC-INV-025 and both overviews now record that **Inventory History must be redesigned before implementation when it returns to the roadmap**, because the preserved design (BR-INV-040, projection over batches) cannot represent Consume. **No redesign performed, feature not reintroduced.**
- **R5 · R7 · R9 · R10 · R11 — accepted, recorded.** New **accepted-risk table** in `inventory/decisions.md`: R5 (no reconciliation in Sprint 7 → future Inventory Adjustments; note that BR-INV-058 removes the largest drift source) · R7 (Catalog conversion-read N+1 accepted as tech debt) · R9 (stranded expired stock expected until Write-Off exists → roadmap debt) · R10 (semantic coupling invisible to arch tests; isolation test must still be extended to Sales at implementation) · R11 (no change).

**IDs added (7):** `BR-INV-058` · `BR-INV-059` · `AC-INV-048` · `AC-INV-049` · `TS-INV-054` · `TS-INV-055` · `AC-SAL-013` · `TS-SAL-015`.
**IDs modified (no renumbering):** BR-INV-013 (date basis note) · BR-INV-046 · BR-INV-050 · BR-INV-056 · BR-INV-057 · REQ-INV-006 · REQ-INV-008 · AC-INV-045/046/047 · TS-INV-050/052/053 · BR-SAL-010/012 · AC-SAL-012 · TS-SAL-014 · REQ-SAL-003 · DEC-INV-025.
**Totals:** Inventory REQ-INV-006..008 · BR-INV-046..059 · AC-INV-037..049 · TS-INV-037..055 · DEC-INV-019..025 (024 tombstoned). Sales REQ-SAL-001..003 · BR-SAL-001..013 · AC-SAL-001..013 · TS-SAL-001..015 · DEC-SAL-001..009.

**Two consequences the owner must see before implementation:**
1. **R4 invalidates existing implemented behaviour.** The committed Inventory Projection and the code-complete Batch Viewer / Expiry Monitoring all compute `today` as `DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)` (`InventoryProjectionQueryHandler.cs:30`, `BatchViewerQueryHandler.cs:29`, `ExpiryMonitoringQueryHandler.cs:31`). BR-INV-059 makes that **wrong for all three**. Correcting them is **Sprint 7 implementation scope**, not a Sprint 6 defect to re-open separately.
2. **Clinic timezone has no documented source.** The Settings module is `Not documented`, so nothing defines the clinic's local date. **No source and no default were invented.** This must be settled before BR-INV-059 can be implemented.
3. **R1 imposes a constraint whose source is Catalog.** "Stock Unit = smallest measurable unit" constrains the Catalog **unit profile**. The canonical rule is recorded in Inventory (the module the invariant protects); **the corresponding rule was NOT added to Catalog's Approved docs** — doing so unilaterally would allocate a new BR-CAT ID in a closed module. Recommended as a follow-up owner decision.

**Still open from the previous review (4, unchanged):** DEC-SAL-002 (customer field) · DEC-SAL-004 (Sales rounding — **money only**; quantity rounding is now settled by BR-INV-058) · DEC-SAL-005 (Sales List) · DEC-SAL-009 (Cancelled state). **DEC-INV-020** (NULL-expiry placement) and **DEC-INV-022** (message detail) also remain.

## Owner review applied (2026-07-30) — Sprint 7 «Sales MVP»: **8 owner rulings APPLIED across 17 docs — documentation only, ZERO code, owner STOP**

**Owner reviewed the Sprint 7 DoR and issued rulings; all applied this session. Documentation only — no implementation code, no migration, no DB change, no endpoint, no UI. Nothing committed.** Docs remain **Draft** pending explicit owner approval to implement.

**Rulings applied (8):**
1. **DEC-INV-021 — APPROVED.** **Expired inventory is not saleable.** FEFO **excludes expired batches before allocation begins**; allocation operates on **saleable batches only** (active **and** not expired). Ordering fixed: **Expiry ASC · Receive Date ASC · Batch Identifier ASC**. Applied to BR-INV-050/051/052/053/054, AC-INV-038/041/**045**, TS-INV-038/041/**050**/**051**, both workflows, Sales BR-SAL-010/012 + edge cases. **No fourth batch state created** — DEC-INV-011/012 stay intact; "expired" remains **derived**, and *saleable* is an **allocation predicate**. **Consequence recorded, not hidden:** stock can read positive in the Projection yet be unsellable, and with no write-off/adjustment path it stays **stranded**.
2. **DEC-INV-023 — REPLACED per owner.** Consumption **must detect concurrency conflicts**; **silent overwrite prohibited**; if inventory changes **between allocation and commit** the sale **fails and requires retry**. Written as **business outcome only** — **no RowVersion, no concurrency token, no locking strategy anywhere in the docs**, per the explicit instruction that mechanism belongs to implementation. New **BR-INV-056**, **AC-INV-046**, **TS-INV-052**, Sales **AC-SAL-012**, **TS-SAL-014(ب)**, retry message in `sales/ui.md`. DEC-INV-002 left **unmodified** and still governs *receiving* — divergence recorded as intentional.
3. **DEC-INV-024 — REMOVED, converted to a Requirement.** Now **REQ-INV-008 — Consumption Traceability** («Sale → Consumed Batch», with per-batch quantity), plus **BR-INV-057 · AC-INV-047 · TS-INV-053**. **No persistence model prescribed** — no ledger, no allocation table, no new entity. **DEC-INV-024 is tombstoned** (repo precedent REQ-CAT-026): ID reserved, never reused, **DEC-INV-025 NOT renumbered**.
4. **DEC-SAL-006 / DEC-INV-019 — APPROVED.** Architectural boundary recorded verbatim: **Sales expresses intent · Inventory performs execution · Sales never performs FEFO · Inventory owns allocation.** Applied to BR-SAL-013, both overviews, both workflows.
5. **DEC-SAL-003 — APPROVED.** **Price Snapshot only**; Price Override **deferred**. **No discounts, no override reason, no audit, no permissions** introduced anywhere. **Catalog requirements left intact** — REQ-CAT-028 / BR-CAT-029 / AC-CAT-028 remain **Approved and unfulfilled**, now recorded in an explicit **Deferred Capabilities** table in `sales/decisions.md`.
6. **DEC-SAL-007 — APPROVED.** Sales honors **`Product.IsSplittable`** (enforcing existing REQ-CAT-030 / BR-CAT-032). **No additional rules** — no auto-rounding, no coercion, no exception path.
7. **DEC-SAL-008 — APPROVED.** Open Package tracking **out of scope**; **no new model, no new requirements**. REQ-CAT-031..033 / BR-CAT-033..036 stay Approved and unfulfilled, listed as deferred.
8. **Inventory History — unchanged, roadmap reference only.** DEC-INV-015 **stays Approved and unmodified**; all its IDs preserved. **DEC-INV-025 now records that after Sprint 7 (Receive + Consume) the original reopen condition is satisfied** — feature **not** reintroduced, no REQ/BR/AC/TS added. Roadmap refs updated in `inventory/overview.md`, `sales/overview.md`, `_INDEX.md`, here. Distinction stated explicitly: **REQ-INV-008 is data traceability, not the History screen.**

**IDs created:** **REQ-INV-008** · **BR-INV-056, BR-INV-057** · **AC-INV-045, AC-INV-046, AC-INV-047** · **TS-INV-050..053** · **AC-SAL-012** · **TS-SAL-014**.
**IDs removed:** **DEC-INV-024** (tombstoned — converted to REQ-INV-008; reserved, never reused).
**Totals now:** Sales REQ-SAL-001..003 · BR-SAL-001..013 · AC-SAL-001..012 · TS-SAL-001..014 · DEC-SAL-001..009. Inventory REQ-INV-006..008 · BR-INV-046..057 · AC-INV-037..047 · TS-INV-037..053 · DEC-INV-019..025 (024 tombstoned).

**Still awaiting owner ruling (6, not invented around):** **DEC-SAL-002** (customer field) · **DEC-SAL-004** (Sales rounding rule — BR-PUR-008 scopes itself to Purchasing) · **DEC-SAL-005** (Sales List in/out) · **DEC-SAL-009** (Cancelled state) · **DEC-INV-020** (**NULL-expiry placement** — the three ordering keys were ruled, but nulls-last was not stated explicitly; carried from the proposal, marked unconfirmed) · **DEC-INV-022** (insufficient-stock message detail; full-rejection principle itself is settled).

**Documentation debt unchanged:** GLOSSARY sync still deferred from Sprint 6; **«المبيعات» still not a GLOSSARY module name**. Not fixed unasked.

**Next (NOT started — owner STOP).** Await explicit owner approval before any code. When approved, note the sprint **spans two modules**, exceeding the single-module New-Feature stop condition — split per slice. Sprint 6's Docker-blocked verification remains outstanding and untouched.

## Doc session (2026-07-30) — Sprint 7 «Sales MVP»: **Sprint DoR DRAFTED — documentation only, NOTHING approved, ZERO code, owner STOP**

**Documentation-First DoR session.** Drafted the whole Sprint 7 DoR across **two modules** (Sales + Inventory). **No implementation code, no migration, no DB change, no endpoint, no UI — none written, none started.** Nothing committed. **All new/changed docs are `Draft`**; approval is the owner's step.

**Sprint scope (5 slices, owner-specified — DEC-SAL-001):** 1 فاتورة البيع · 2 التفاصيل · 3 إثبات البيع (Sales) · 4 استهلاك المخزون · 5 تخصيص FEFO (**Inventory**). Introduces the **second and only new inventory movement: Consume** (alongside Receive).

**Docs written/updated (17).** **Sales (8, all were placeholders → drafted):** `overview` · `requirements` · `business-rules` · `workflow` · `ui` · `acceptance` · `test-scenarios` · `decisions`. **Inventory (7):** `overview` (Sprint 7 section) · `requirements` (REQ-INV-006/007) · `business-rules` (BR-INV-046..055) · `acceptance` (AC-INV-037..044) · `test-scenarios` (TS-INV-037..049) · `decisions` (DEC-INV-019..025) · **`workflow` filled** (was Placeholder — first inventory *write* flow). **Plus:** `docs/modules/_INDEX.md` (Sales + Inventory rows) · this `STATUS.md`. **Untouched:** all Catalog/Purchasing docs, `write-kernel.md`, `GLOSSARY.md`, every ADR.

**New IDs (contiguous, nothing renumbered, nothing reused).** Sales: **REQ-SAL-001..003 · BR-SAL-001..013 · AC-SAL-001..011 · TS-SAL-001..013 · DEC-SAL-001..009**. Inventory (continuing after the verified maxima REQ-005/BR-045/AC-036/TS-036/DEC-018): **REQ-INV-006..007 · BR-INV-046..055 · AC-INV-037..044 · TS-INV-037..049 · DEC-INV-019..025**.

**Central architectural proposal (needs owner ruling — DEC-SAL-006 / DEC-INV-019):** Sales states **business intent** («استهلك N من P») via a **public contract**; **Inventory owns batch selection and FEFO**. Mirrors the receiving contract exactly (BR-PUR-010 / DEC-PUR-008 / DEC-INV-001) and keeps the **module-isolation architecture test** green — putting FEFO in Sales would make it depend on Inventory internals and break that test. **No new field/table/migration/abstraction/ADR** for the base design: `RemainingQuantity` already exists **for this exact purpose** (BR-INV-001).

**Gaps REPORTED, not invented around (as the brief directed):**
1. **No Movement Ledger → post-sale traceability is unrecoverable.** If consumption only decrements, *which sale consumed which batch* is never stored — the Sprint-6 traceability chain stops at the sale. Options laid out in **DEC-INV-024** (nothing / allocation rows owned by Inventory / full ledger). **No structure invented.**
2. **No «open package» concept in the inventory model.** Approved Catalog docs REQ-CAT-031..033 / BR-CAT-033..036 (one open package per product, second-open exception + audit, post-open expiry) have **no representation** in `InventoryBatch` and would need new structure + an undocumented Audit Log. **Recommended out of scope** — DEC-SAL-008.

**Architectural risks reported:** (a) **DEC-INV-023 — overselling.** DEC-INV-002 knowingly accepted optimistic overwrite for *receiving* (worst case: a lost increment). Under *consumption* the same gap means **selling stock that isn't there** and silently breaking BR-INV-005, **with no returns or adjustments to correct it**. Options include optimistic concurrency / row versioning — **which would change a ruled decision and likely needs an ADR; none written, owner rules first.** (b) **DEC-INV-021 — FEFO picks expired batches first by construction** (earliest-expiry-first). A **veterinary safety** question, not a preference; recommended exclusion, with the honest cost that excluded stock becomes **stranded** (no write-off path exists). (c) **BR-INV-049** — consumption is the first operation able to break the BR-INV-005 invariant; kept documentation-only per the owner's earlier ruling but now covered by an explicit test (TS-INV-044).

**Documented conflict with Approved Catalog docs (DEC-SAL-003 — blocking).** Sprint 7 excludes «Discounts», but **REQ-CAT-028 / BR-CAT-029 / AC-CAT-028** are **Approved** and say the cashier may override the sale price, the invoice keeping original + actual price + discount + optional reason, explicitly **«التنفيذ في وحدة المبيعات»** and «متطلب أساسي». **Resolved as deferral, not denial** — Sprint 7 prices are Catalog snapshots and those Catalog IDs stay **Approved and unfulfilled**. **No approved document was edited.** Owner must confirm it is a deferral. Also flagged: price override drags in **audit** (REQ-CAT-027), and Audit Log is undocumented (DEC-CAT-028).

**Other open owner decisions:** DEC-SAL-002 (customer = optional free text, mirroring supplier) · DEC-SAL-004 (**Sales needs its own rounding rule — BR-PUR-008 scopes itself to Purchasing**; silent reuse rejected) · DEC-SAL-005 (**no Sales List in the 5 named slices** — navigational gap flagged, list NOT invented) · DEC-SAL-007 (enforce REQ-CAT-030 partial-selling; `Product.IsSplittable` **already exists** so it is cheap) · DEC-SAL-009 (Cancelled state — not in the brief, not invented) · DEC-INV-020 (FEFO ordering: expiry asc, **nulls last**, then received-at, then batch id — each key matching an existing implemented pattern) · DEC-INV-022 (insufficient stock = full rejection).

**Rescan of DEC-INV-015 (DEC-INV-025 — recorded, not changed):** History was deferred because **only one movement type existed**. Sprint 7 adds the second, so **the stated deferral condition is nearly met**. Per the brief, History **stays out of scope and DEC-INV-015 stays Approved and unmodified**, all its IDs preserved. Recorded so the two Approved docs do not silently contradict.

**Read-side integration:** Projection · Batch Viewer · Expiry Monitoring get **no doc or code change**; consumption only moves their numbers. Because this is the **first real source of Depleted batches**, integration is evidenced by **AC-INV-044 + TS-INV-045/046** rather than asserted.

**Known documentation debt (not fixed this session, deliberately):** **`GLOSSARY.md` sync remains deferred from Sprint 6**; Sprint 7 adds ~11 terms staged in `sales/ui.md` the same way. **«المبيعات» is not yet a GLOSSARY module name** — and the New-Module pattern expects an approved Arabic module name there. Owner to decide: sync GLOSSARY before implementing Sprint 7, or keep deferring. **Scope was not widened to fix it unasked.**

**Next (NOT started — owner STOP).** Owner rules on **DEC-SAL-002..009 + DEC-INV-019..025** → apply rulings → flip docs Draft→Approved → only then implement. **Do NOT write code, migrations, endpoints, or UI.** Note for implementation planning: the sprint **spans two modules**, which exceeds the single-module New-Feature stop condition — expect to split it per slice. Pre-existing untracked, intentionally not committed: `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`. **Sprint 6's Docker-blocked verification below is still outstanding and unaffected by this session.**

## Session close (2026-07-30) — Sprint 6 Inventory Read Experience: DoR APPROVED, Batch Viewer + Expiry Monitoring IMPLEMENTED — **verification Docker-blocked, NOTHING committed, owner STOP for commit**

**Two-phase session (owner-directed).** **Phase A — DoR rulings applied**, Sprint 6 DoR flipped Draft→**Approved**. **Phase B — implemented Batch Viewer + Expiry Monitoring** (Inventory History **deferred**, not built). **Nothing committed; STATUS updated at owner's `/close-session` direction; commit still awaiting owner approval per standing instruction.**

**Owner rulings applied (2026-07-30):**
1. **DEC-INV-014 (Approved)** — Expiry Monitoring scope = **active batches with a non-null expiry, clinic-wide** (excludes depleted & no-expiry).
2. **DEC-INV-017 (Approved)** — Expiry Monitoring belongs to **Inventory** (visibility); **Monitoring** module = alerts/notifications/background-jobs/escalations only (roadmap, out of scope).
3. **DEC-INV-018 (Approved — NEW this session)** — Expiry Monitoring is a **projection owning no expiry state**; expiry computed from `InventoryBatch.ExpiryDate` at query time — **no cached values / materialized tables / scheduled refresh / duplicated state**.
4. **DEC-INV-015 (Approved → DEFER)** — **Inventory History removed from Sprint 6.** Reason: **no Movement Ledger** and only **one committed movement type (Receive)** — no Consume/Adjustment/Transfer/Return — so it would not represent true movements. **All IDs preserved** (REQ-INV-005 / BR-INV-039..045 / AC-INV-031..036 / TS-INV-031..036 / DEC-INV-016), marked **Deferred** until multiple movement types exist. DEC-INV-016 deferred with it.

**Implemented (code-complete, builds clean):**
- **Batch Viewer** (REQ-INV-003): `GET /api/v1/inventory/{productId}/batches` → per-product 9-field batch list, derived Active/Depleted, Purchase Reference nav-link, status/expired/expiring filters, sort + deterministic default (receive-date desc, batch-id asc), 404 not-found vs empty. Frontend `features/inventory/batch-viewer/` + route `/inventory/:productId`.
- **Expiry Monitoring** (REQ-INV-004): `GET /api/v1/inventory/expiry` → clinic-wide 4-field list of active batches with a real expiry, search/category/expired/expiring filters, deterministic expiry-asc order, no sort/alerts. Frontend `features/inventory/expiry-monitoring/` + route `/inventory/expiry` (static-before-`:productId`) + shell nav «مراقبة الصلاحية».
- **Architecture:** CQRS-lite projections (ADR-0014 §5); cross-module reads (Catalog unit, Purchasing invoice for the reference via 2-hop plain-Guid join) confined to Infra handlers; **Application DTOs primitive-only (isolation arch test green)**. **No new module/service/abstraction; no migration; no schema change** (`ef has-pending-model-changes` = none). Mirrored the two screens, did NOT extract a shared batch abstraction (rule-of-three not met).

**Gates — executed & green:** build 0/0 Release · `dotnet format` clean · **Architecture 76** · **Domain 101** · **Frontend unit 156** (+12 new store specs) · ESLint + Stylelint clean · `ng build` exit 0 (529.55 kB — **TD-107** warning, not raised) · `ef migrations has-pending-model-changes` = none.
**Gates — NOT executed (environment-blocked):** **18 integration tests** (`BatchViewerEndpointTests` 11 + `ExpiryMonitoringEndpointTests` 7 — written, **compile clean**, UNRUN) · **live-browser verification** · **performance SQL capture**. All require Postgres via **Testcontainers/Docker, whose daemon is unavailable here** (service start denied without admin). Backend query SQL (2-hop join; expiry WHERE-clauses) is therefore **build- and pattern-verified only** (structured to mirror the already-tested projection handler), **not execution-verified**. **The commit gate is not yet satisfiable** until these run green where Docker is available.

**Known characteristics / TD:** (a) **Batch Viewer is not literally one query** — one O(1) product-existence/header lookup **+** one batch projection SELECT **+** COUNT; the header lookup is **required by AC-INV-022** (not-found vs empty). Expiry Monitoring **is** single SELECT + COUNT. (b) Batch identifier shown truncated (first GUID segment; full in `title`) — display choice, no new field. (c) **TD-107** unchanged. (d) **Verification debt** (the 18 unrun tests + browser + perf) is the gating item for commit.

**Docs synchronized (9, drift-swept):** `overview.md` · `requirements.md` · `business-rules.md` · `acceptance.md` · `test-scenarios.md` · `ui.md` · `decisions.md` (DEC-INV-014..018) · `_INDEX.md` · this `STATUS.md`. `GLOSSARY.md` sync still deferred (Expiry-monitoring terms staged in `ui.md`; History terms deferred with the slice). `workflow.md` untouched (read-only screens).

**Proposed commits (NOT executed — gated on the unrun integration/browser/perf passing green):**
1. `feat(inventory): Batch Viewer + Expiry Monitoring (Sprint 6)` — backend + frontend + tests.
2. `docs(inventory): approve Sprint 6 DoR, defer Inventory History, synchronize` — rulings + `_INDEX` + this STATUS.

**Open questions for the owner:**
1. **Commit gate:** run the 18 integration tests + live-browser + performance SQL capture in a **Docker-enabled** environment before committing. Can you start Docker Desktop (needs admin) so I run them, or will you run them?
2. **Expiry Monitoring default view (non-blocking):** with no filter it lists **all** active batches with any expiry (even far-future), soonest-first, Expired/Expiring as optional filters — per approved DEC-INV-014. If you intended the **base view** = *expired ∪ expiring-soon(30d)* only, that's a one-line rule change.
3. **GLOSSARY sync** (Expiry-monitoring terms) — do at commit-time or a follow-up?

**Next:** owner runs/enables the blocked verification → if green, commit the two commits above → (do not push, standing rule). Do NOT build Inventory History (deferred). Pre-existing untracked, intentionally not committed: `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Doc-approval session (2026-07-30) — Inventory Slice 2 (Batch Viewer): DoR owner-APPROVED, rulings applied — **NO CODE, nothing committed, owner STOP**

**Documentation-only session.** The owner reviewed the Batch Viewer DoR and issued rulings; all applied and the DoR flipped **Draft→Approved (2026-07-30)**. **Zero implementation** — no code, migration, endpoint, or UI; **nothing committed** (owner directed STOP for implementation approval).

**Owner rulings applied (2026-07-30):**
1. **DEC-INV-008 (Approved w/ clarification):** Batch Viewer is an **Inventory read screen**; `InventoryBatch` is an **Inventory-owned domain entity** — **NOT a new Batch module**. DEC-INV-007 stays Approved & unchanged in meaning — its "Batch module" reference is the **future navigation target only, not module ownership**. Supersedes **only** the ownership ambiguity. (Planned Batch module stays a roadmap item — `overview.md` 3-module split + `_INDEX` Batch row untouched.)
2. **DEC-INV-009 (Approved):** display the **existing stable Batch Identifier** only — **no human Batch Code, no new field, no generated number, no business logic.**
3. **DEC-INV-010 (Approved):** Purchase Reference = **navigation link** opening Purchase Invoice Details; viewer stays read-only.
4. **DEC-INV-011/012 (Approved):** batch status is **derived, Active/Depleted only**; **Expired is never a status — filter only.**
5. **BR-INV-031 (new rule):** deterministic default ordering — **Receive Date desc, tie-break Batch Identifier asc** — for stable pagination. (Tie-break key = the same stable identity `Id` already used in BR-INV-027 — written as one key, not two.)
6. **BR-INV-030 (strengthened):** **single projection query — no per-row lookups, no N+1, no lazy loading.** (Folded the "no lazy loading / single projection query" constraint into the existing performance rule rather than a duplicate ID — flagged for owner.)
7. **GLOSSARY synced:** 7 genuinely-new Batch-Viewer concept terms added (Batch viewer, Batch identifier, Batch status, Depleted batch, Expired batch, Purchase reference; Active defined inline). Field-display labels (Receive/Original/Remaining/Unit-cost) left out as non-new (write-kernel field labels). **Pre-existing drift fixed & flagged:** the «دفعة» row's stale "TODO (Batch module docs)" corrected to Inventory ownership (BR-INV-001, DEC-INV-008) — predates this slice (write-kernel defined `InventoryBatch` 2026-07-22).

**Final ID set (contiguous, none reused/renumbered).** REQ-INV-003 · **BR-INV-018..031** · **AC-INV-014..024** · **TS-INV-014..024** · **DEC-INV-008..013**. (BR-INV-031 / AC-INV-024 / TS-INV-024 are the deterministic-ordering rule + its traceability additions — the AC/TS are mine for coverage, not owner-specified.)

**Docs updated (11):** `overview.md` · `requirements.md` · `business-rules.md` · `acceptance.md` · `test-scenarios.md` · `ui.md` · `decisions.md` · `docs/shared/GLOSSARY.md` · `docs/modules/_INDEX.md` · this `STATUS.md`. (`workflow.md` intentionally left as-is — a read-only screen has no workflow, mirroring Slice 1.)

**Remaining owner decisions: none** — DEC-INV-008/009 were the only two open; both ruled. **Next (NOT started — owner STOP):** implement Batch Viewer per the Approved docs only after implementation approval. Do NOT begin code/migration/endpoint/UI. Pre-existing untracked, intentionally not committed: `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Current sprint

**Sprint 3 — Implementation.** The first product code of VetFlow.

Implementation outranks governance. If implementation exposes a weakness in the
foundation: **record it under "Foundation friction" below, keep working if it is
safe, and evaluate the change only after the feature is complete.** Governance
changes require evidence (Governance Change Policy — `docs/architecture/principles.md`).

**Every implementation session starts at `.claude/playbooks/implementation.md`.**

## Session close (2026-07-29) — Inventory Slice 1 (Inventory Projection): IMPLEMENTED, owner-APPROVED, COMMITTED (feat + docs-sync) — NOT pushed

**Slice 1 = COMPLETED.** The Inventory Projection read model is implemented, fully gated, three-way verified, owner-approved, and **COMMITTED as two commits** (`85f2021` `feat(inventory): Inventory Projection (Slice 1)` — backend + frontend + tests; + `docs(inventory): synchronize repository after Inventory Projection` docs-sync — STATUS/module docs/`write-kernel.md` terminology/`_INDEX`/GLOSSARY, no mixing). **Not pushed** (standing owner rule). Owner STOP: do NOT begin Batch Viewer / Expiry Monitoring / adjustments / the deferred Low Stock capability.

**What the slice is.** A **read-only projection** (REQ-INV-002) at `GET /api/v1/inventory` + `/inventory` — one row per product with a `ProductOnHand` record (BR-INV-007): Product · On-Hand (stock unit) · Stock Unit · Batch Count (active) · Nearest Expiry («—» when none). Filters: Search (product name) · Category · Expiring Soon (30-day horizon, BR-INV-013) · Out of Stock (BR-INV-011). Sort: Product · On-Hand · Nearest Expiry (nulls-last, `Id` tiebreaker). Four view states, RTL, table↔cards, removable chips. Row → placeholder navigation to the future Batch Viewer (BR-INV-015). **No new business logic; owns no state; no write surface (BR-INV-006).**

**Owner rulings this session (2026-07-29), applied:** (1) **Terminology — Option B (full sync):** all legacy `وحدة التخزين` / idafa `وحدة تخزين المنتج` in `write-kernel.md` unified to the canonical GLOSSARY phrase (`وحدة المخزون` / `وحدة المخزون القانونية للمنتج` — the form already in `business-rules.md` BR-INV-008). Documentation only; no code/class renamed. (2) **Dedicated `Projection_never_shows_uncommitted_state_BR_INV_016`** integration test added (a write inside an uncommitted transaction is invisible to the projection on a separate connection — proves committed-only).

**Architecture (no new abstraction / module / library / ADR / migration).** CQRS-lite read projection (ADR-0014 §5); cross-module read confined to the Infrastructure query handler (§2 — `Application.Inventory` Query/DTO are **primitive-only**, isolation arch test green); reused query pipeline (§6/§9), Arabic search, pagination (ADR-0015), `QueryStringParser`, `TimeProvider` for the horizon; frontend mirrors the Product/Purchase list feature (STD-FE-004); UI Kit only. **No schema change** — `ef migrations has-pending-model-changes` = none.

**Backend.** `Application/Inventory/Queries/InventoryProjection/` (Query · Dto · SortField · Validator — primitive-only) · `Infrastructure/Inventory/InventoryProjectionQueryHandler.cs` (single joined read: `product_on_hands ⋈ products ⋈ units` + inline correlated subqueries for active batch count / MIN expiry; row filters for out-of-stock/expiring-soon; nulls-last sort) · `Api/Endpoints/Inventory/` (Endpoints + Request). Registered in Infrastructure DI, Application DI (validator), QueryPipeline, Program.cs. **Frontend.** `features/inventory/` — routes + `inventory-list/` (models · api · store · page · table/cards/filters-drawer/skeleton · 3 specs); route `/inventory`, shell nav «المخزون», i18n `inventory.*`.

**Gates (all green):** `dotnet build` 0/0 Release · `dotnet format` clean · backend **301** (Domain **101** · Architecture **76** · Integration **124**, +15) · `ef migrations has-pending-model-changes` = none · ESLint + Stylelint clean · `ng build` exit 0 (**521.08 kB** — TD-107 warning, **not raised**) · frontend **144** (+12). **Verification (three-way):** **live browser** (headless Chrome/CDP, real stack db :5434 + API :5080 + ng :4200) drove `/inventory` at desktop 1440 + mobile 390 — dir=rtl, **0px overflow**, 5-column table / cards, nav active, «—» for null expiry, search empty-state, **zero console errors**; **integration** (15 tests, real HTTP + Postgres) — every testable BR/AC incl. BR-INV-007/009/010/011/013/016 + boundary (30 vs 31 days), read-only 405, malformed-boolean 400; **unit/store** — sort default, empty states, chip removal, navigation intent. **Performance (BR-INV-017):** captured EF SQL — the page result is **one SELECT** (correlated subqueries run in-statement, one round-trip); the only companion is the standard pagination COUNT (same two-query pattern as the approved Product/Purchase list handlers). **No N+1.**

**Nine-question self-review: all NO.** No principle/ADR/standard breach; no duplicated business logic (read-only mirror, none exists); **boundary strengthened** (primitive-only Application types, isolation test green, cross-module read isolated to the Infra handler); minimal, scope-locked; no invented logic (every rule → REQ-INV-002 / BR-INV-006..017 / DEC-INV-003..007; default sort product-asc is a pattern-consistent engineering default); docs synchronized; no ADR owed. **TD/limitations:** TD-107 unchanged (521.08 kB, not raised — inventory i18n +~3.8 kB); "today" for the 30-day horizon is UTC-based (consistent with the system clock; can shift the boundary by a day near local midnight); row navigation targets `/inventory/:productId` (future Batch Viewer) which resolves via the wildcard until the Batch module lands. **Dev DB** unchanged (verification read the pre-existing Slice-5 data only; created no rows). Dev stack + Chrome stopped; Postgres container left running.

**Pre-existing repo note (not this slice, flagged for owner):** the `_INDEX.md` **Purchasing** row still reads Slice 5 "not yet implemented" (committed state) though STATUS records Slice 5 as implemented & committed — a stale line to reconcile in a future docs pass.

**Next (NOT started — owner STOP):** do NOT begin Batch Viewer / Expiry Monitoring / inventory adjustments / the deferred Low Stock capability. Pre-existing untracked, intentionally not committed: `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Sprint 5 opened (2026-07-22) — Inventory Slice 1 (Inventory Projection): DOCS owner-APPROVED, DoR READY, **NO CODE, nothing committed**

**Sprint 5 = the Inventory module proper begins.** First slice = **Inventory Projection (read model)** — a read-only screen to view current physical inventory. **Documentation-only session**: drafted the DoR, surfaced the gaps, owner ruled, applied rulings, flipped docs Draft→**Approved**. **Zero implementation code, zero migrations, zero DB/endpoint/UI changes; nothing committed** (owner directed STOP for implementation approval). Working tree = 9 modified docs (7 Inventory module files + `docs/modules/_INDEX.md` + `docs/shared/GLOSSARY.md`) + the two pre-existing untracked items (`docs/releases/`, `docs/ui/product-editor-ux-architecture.md`).

**What the slice is.** A **read-only projection** that only displays data **already produced by Purchase Receiving** (`InventoryBatch` + `ProductOnHand`, from the Write Kernel). **No new business logic; it never owns inventory state.** Computed **on-the-fly at query time** (approved CQRS-lite pattern, ADR-0014 §5 — mirrors Product/Purchase list handlers), so it is inherently **disposable/rebuildable** — no materialized table, no migration, no new abstraction. Screen = one row per product **that has an inventory record**: Product · Current On-Hand · Stock Unit · Batch Count · Nearest Expiry. Filters = Search · Category · Expiring Soon (30d) · Out of Stock. Sort = Product · On-Hand · Nearest Expiry. Each row **navigates to the future Batch Viewer (navigation only — not designed here)**.

**DoR gaps surfaced (not invented) → owner ruled.** The DoR exposed that **"Low Stock" has no basis in the data model** (Product has no reorder/min-stock field) and **"Expiring Soon" had no horizon**. Surfaced both rather than inventing thresholds. **Owner rulings (2026-07-22), applied:**
1. **DEC-INV-003** — projection source = **physical inventory only** (`ProductOnHand` rows; never-received products don't appear). Represents inventory, not the Catalog.
2. **DEC-INV-004** — **Low Stock DEFERRED**: filter **removed** from this slice; documented as a future capability depending on a **per-product Reorder Level owned by Catalog**; **no placeholder logic**.
3. **DEC-INV-005** — **Expiring Soon = 30 calendar days**, global fixed horizon, now a documented business rule (**BR-INV-013**); future configurability out of scope.
4. **DEC-INV-006** — reuse the sanctioned Catalog read path but **one efficient read query joining reference data; no N+1, no per-row lookups, no new abstraction, no duplicated Catalog data** (constraint = **BR-INV-017**).
5. **DEC-INV-007** — row → future Batch Viewer, **navigation only**.
6. **GLOSSARY synced** — 7 Inventory terms are now canonical in `docs/shared/GLOSSARY.md` (On-hand quantity/الرصيد المتاح, Inventory projection/إسقاط المخزون, Batch count/عدد الدفعات, Nearest expiry/أقرب صلاحية, Expiring soon/قرب انتهاء الصلاحية, Out of stock/نفاد المخزون, Reorder level/حد إعادة الطلب).
7. New rule **BR-INV-016** — projection is **eventually consistent**, shows **committed state only**, never uncommitted operations.
8. New rule **BR-INV-017** — implementation constraint: **single read query, no N+1**.

**Approved IDs (contiguous, none reused/renumbered).** REQ-INV-002 · **BR-INV-006..017** (BR-INV-012 = Low Stock *deferred*) · **AC-INV-004..013** · **TS-INV-004..013** · **DEC-INV-003..007**. Kernel IDs (REQ-INV-001 / BR-INV-001..005 / AC-INV-001..003 / TS-INV-001..003 / DEC-INV-001..002) remain in `write-kernel.md`, **untouched**. Docs Draft→**Approved**: overview, requirements, business-rules, acceptance, test-scenarios, ui, decisions (+ `_INDEX` Approved, + GLOSSARY). `workflow.md` intentionally left Placeholder (a read-only screen has no workflow). Drift check clean (traceability intact; Low Stock consistently marked deferred; stock-unit term aligned to GLOSSARY canonical «وحدة المخزون» in the new docs).

**Open question for the owner (non-blocking).** Minor **terminology reconciliation**: new Projection docs + GLOSSARY use canonical **«وحدة المخزون»** (Stock-keeping unit, BR-CAT-020); the **Approved** `write-kernel.md` uses the older **«وحدة التخزين»** for the same concept. Left the Approved kernel doc untouched; a one-line unifying edit to `write-kernel.md` is available if the owner wants module-wide term unification.

**Next (NOT started — owner STOP for implementation approval).** Implement Inventory Projection Slice 1 per the Approved docs (mirror the Product/Purchase **list** pattern: `InventoryProjectionQuery` + handler → single joined read; read-only endpoint; frontend list with the 4 view states, RTL; row→future Batch-Viewer navigation stub). New-Feature mode, budget Medium ≤60k. Do **not** design Batch Viewer, Monitoring, adjustments, or the deferred Low Stock capability.

## Session close (2026-07-22) — Purchasing Slice 5 (Purchase Receiving) + Inventory Write Kernel: owner-APPROVED, rulings applied, COMMITTED (`fbc55e6` + docs-sync) — NOT pushed

**Slice 5 = COMPLETED.** Receiving — the business event that moves inventory — is implemented, fully gated, verified, owner-approved (final rulings applied), and **COMMITTED as two commits** (`fbc55e6` `feat(purchasing): Purchase Receiving (Slice 5)` — backend + frontend + Inventory write kernel + tests; this `docs(purchasing): synchronize repository after Purchase Receiving` — STATUS/module docs/kernel DoR/_INDEX, no mixing). **Not pushed** (remote exists but standing owner rule: do not push). Owner STOP: do NOT begin Inventory Projection / Batch Viewer / Expiry Monitoring.

**DoR path (this session).** The DoR exposed that receiving-as-documented creates Inventory Batches, but Inventory was an undocumented placeholder and DEC-PUR-001 had deferred the inventory-model decision here — a real Documentation-First + cross-module conflict. Surfaced it (did not invent); owner ruled **Option A**: a **minimal Inventory Write Kernel** (`docs/modules/inventory/write-kernel.md`, owner-approved) that exists solely to satisfy the receiving contract — NOT the Inventory module. Kernel DoR approved with rulings: quantities stored in the **Product canonical stock unit** (receiving converts via the existing Catalog unit profile — no new rule), **persisted per-product `OnHandQuantity`** incremented atomically (not derived), and batch field **`RemainingQuantity` = Quantity** (forward-compat, no consumer this slice).

**Approved IDs.** Purchasing: **REQ-PUR-005** · **BR-PUR-009** (receiving event/preconditions: one-time, no-partial, ≥1 line) · **BR-PUR-010** (inventory effect via the Inventory public contract — one line → one batch, references the line, stores product/qty/unit-cost/optional-expiry, quantities in stock unit) · **BR-PUR-011** (immutability after receiving) · **BR-PUR-012** (blocking validations) · **BR-PUR-013** (product-driven expiry) · **AC-PUR-014..018** · **TS-PUR-025..033** · **DEC-PUR-007** (receiving policy) · **DEC-PUR-008** (public contract with Inventory — no ADR) · **DEC-PUR-009** (expiry = Product definition). Inventory kernel: **REQ-INV-001** · **BR-INV-001..005** · **AC-INV-001..003** · **TS-INV-001..003** · **DEC-INV-001..002**.

**Scope delivered (exactly the approved docs).** Draft→Received, atomic (all-or-nothing), one-time, irreversible; one Inventory Batch per line + per-product on-hand increment, both in the canonical stock unit (converted). Product-driven required expiry. No returns/partial/undo/adjustments/projection/reporting/payments/taxes/discounts/batch-viewer/expiry-monitoring.
- **Backend:** `PurchaseInvoice.Receive()` (Draft + ≥1 line guards → Received; immutability reuses existing Draft-only guards); codes `VTF-PUR-006`/`VTF-PUR-007`. Inventory kernel — `InventoryBatch` (7 fields incl. `RemainingQuantity`), `ProductOnHand` (ProductId PK, `Increase()`), public contract `IInventoryReceiptWriter` + `InventoryReceiptLine`. `ReceivePurchaseInvoiceCommandHandler` reads products via the sanctioned `ProductDetailsQuery` (STD-BE-005), enforces expiry, converts purchase→stock unit (existing Catalog factors), transitions the aggregate, then the kernel **stages** batch + on-hand on the **shared scoped `DbContext`** — **one `SaveChanges` = atomic**. On-hand row deduped (same product on two lines → one row). Migration `20260722141555_InventoryWriteKernel` (2 tables, **reference-only — no cross-module FK, no cascade**). Isolation test extended (Inventory = 4th isolated module). `POST …/{id}/receive`. `requiresExpiry` added as a **live** field on the Slice-4 lines read.
- **Frontend:** Purchase Details — **Receive** button (Draft-only) → **confirmation dialog** (irreversible; the deliberate contrast to DEC-PUR-005 no-confirm delete) with product-driven required per-line expiry; `PurchaseLinesStore.receive()`; on success the header re-reads (status → Received, actions disappear). i18n `purchaseDetails.receive.*`.

**Owner final-review rulings applied (2026-07-22):** (1) **DEC-INV-002** — MVP intentionally accepts optimistic-overwrite risk for `ProductOnHand` (single-clinic, very low concurrency, simplicity over locking; future work may add optimistic concurrency/row versioning) — intentional trade-off, not a defect. (2) **BR-INV-005** — canonical Inventory invariant: `ProductOnHand = Σ RemainingQuantity across active batches` (documentation only, no validation). (3) **Migration review** — confirmed **no** cascade delete/update and **no** FK between Purchasing and Inventory (only PK constraints; configs declare no navigation; ids stored as plain Guid) — reference-only.

**Gates (independently re-run this session, all green):** `dotnet build` 0/0 Release · `dotnet format` clean · backend **286** (Domain **101** · Architecture **76** · Integration **109**) · `ef migrations has-pending-model-changes` = none · ESLint + Stylelint clean · `ng build` exit 0 (517.28 kB — TD-107 warning, not raised) · frontend **132**. **Verification (three-way, owner reporting standard):** **live browser** (headless Chrome/CDP, real stack) drove the **no-expiry** receive flow at desktop 1440 + mobile 390 — dir=rtl, 0px overflow, Draft→مستلمة, Receive/add buttons present then gone (immutability), confirm dialog + irreversible warning, **zero console errors**; **integration** (11 receive tests, real HTTP + Postgres) — batch-per-line + on-hand, **unit conversion both directions** (×120=240 and ÷ path 2 cartons→24 boxes with a mid-chain storage unit), one-time/empty/cancelled rejection, immutability, expiry required-reject + stored + not-required→null, same-product-two-lines dedup, atomic-reject persists nothing; **unit/store** — Receive() transitions, kernel invariants, dialog blocks-until-required + emits payload, store receive success/failure. Live API/DB spot-check: 204 then 409 `VTF-PUR-003`; DB batch (qty 20 = 2 pieces × 10, remaining 20, unitCost 100, expiry NULL, line-ref) + on-hand 20.

**Nine-question self-review: all NO.** No principle/ADR/standard breach; no duplicated logic (conversion applies existing Catalog factors; guards are defense-in-depth per STD-BE-010); **boundary strengthened** (Purchasing never depends on Inventory; contract via port; isolation test extended); minimal, scope-locked kernel; no invented logic (every rule traces to approved Slice-5/kernel DoR); docs synchronized; **Q9 no ADR** — the cross-module atomic write is an application Unit of Work, owner-waived (DEC-PUR-008/DEC-INV-001). **TD/limitations:** DEC-INV-002 optimistic-overwrite (owner-accepted MVP trade-off, not debt); `requiresExpiry` adds N `ProductDetailsQuery` reads on the details path and extends the committed Slice-4 lines contract (accepted MVP cost); TD-107 unchanged (517.28 kB, not raised). **Dev DB** holds harmless verify data (received invoices + inventory batches/on-hand); nothing committed. Dev stack stopped (Postgres container left running).

**Next (NOT started, owner STOP):** do NOT begin Inventory Projection / Batch Viewer / Expiry Monitoring. Pre-existing untracked, intentionally not committed: `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Session close (2026-07-21) — Purchasing Slice 4 (Purchase Line Items): owner-APPROVED (final review), rulings applied, COMMITTED (`4527176` + docs-sync) — NOT pushed

**Slice 4 = COMPLETED.** The owner gave **final approval (2026-07-21)** and directed a fixed set of rulings — applied this session (documentation only; no code change), then committed as **two commits** (`4527176` `feat(purchasing): Purchase Line Items (Slice 4)` — backend + frontend + tests; this `docs(purchasing): synchronize repository after Purchase Line Items` — STATUS/module docs/_INDEX, no mixing). **Not pushed** (no remote — standing owner item). Owner directed a hard **STOP**: do NOT begin Purchase Receiving / inventory movement / the next slice.

**Owner final-review rulings applied (2026-07-21):**
1. **Rounding policy (permanent):** all Purchasing monetary values use **Round Half Away From Zero to exactly 2 dp** — **not banker's**. New **BR-PUR-008** (canonical rule) + **DEC-PUR-004**. Code already matched (`PurchaseLineItem` line total `MidpointRounding.AwayFromZero`) — documentation-only.
2. **Delete UX:** line deletion stays **immediate, no confirmation** — **DEC-PUR-005** (intentional UX decision; draft lines have no inventory/accounting effect, re-adding is cheap). ui.md updated (superseded the old "follows Catalog pattern" note).
3. **Legacy seed exemption:** implementation note under **BR-PUR-006** — pre-Slice-4 dev seed data (header-only, DEC-PUR-002) is exempt from `Invoice Total = Σ line totals`; every invoice created after Slice 4 must satisfy it.
4. **Product picker (MVP approved):** max 100 active products · client-side filtering · no server search/paging/virtualization — **DEC-PUR-006** (optimize later in a dedicated slice, not now). Verified against `PurchaseLinesApiService.ProductPageSize = 100`; ui.md note added.
5–7. **Reaffirmed, no change:** aggregate is the only place the total is computed (BR-PUR-006/DEC-PUR-003); snapshot immutability (BR-PUR-007); shared extensions `ApiClient.delete` + `FormatService.decimal` are legitimate, no new abstraction.

New IDs are contiguous (no gaps/tombstones): BR-PUR-008 · DEC-PUR-004/005/006. `_INDEX` Purchasing row (was stale "Slice 3 IN PROGRESS") now reads "Slices 1–4 implemented & committed"; no "line items pending" references remain.

**DoR (this session).** Drafted Slice-4 docs from the owner's spec (no invention), confirmed 3 rulings (purchase-role units only · snapshot names at add-time · quantity 3dp), then applied the owner's 6 review rulings and flipped docs Draft→Approved. Approved IDs: **REQ-PUR-004** · **BR-PUR-005** (line structure), **BR-PUR-006** (total derived in the aggregate — the single canonical place; handlers/repos/controllers/DB/frontend never compute it), **BR-PUR-007** (snapshot immutability) · **AC-PUR-008..013** · **TS-PUR-016..024** · **DEC-PUR-003** (entities inside the `PurchaseInvoice` aggregate; catalog referenced by id + name snapshot — no isolation breach, no new ADR; expires DEC-PUR-002's time-scoping of BR-PUR-001's total clause). ui.md froze the Details top-level layout: Header → Invoice Information → **Purchase Line Items** → Notes.

**Scope delivered (exactly the approved docs).** Line items on a **Draft** invoice: add/remove, list, Details displays them, invoice **Total derived from lines** and persisted, always reflecting the lines. No receiving, inventory, batch, expiry, taxes, discounts, payments, supplier mgmt, header edit, or line edit (remove+re-add only).
- **Backend:** `PurchaseLineItem` entity **inside** the `PurchaseInvoice` aggregate; `AddLine`/`RemoveLine` guard Draft-only (BR-PUR-003 → 409 `VTF-PUR-003`) and call the single `RecalculateTotal()` (Σ line totals; line total = qty×price **rounded once to EGP** so header == Σ displayed lines). New `PurchasingErrorCodes` (`VTF-PUR-003`, `VTF-PUR-005`+reason → 400). Commands `AddPurchaseLineItem` (`ICommand<Guid?>`, null⇒404) + `RemovePurchaseLineItem`; the add handler resolves the product/unit + snapshots names via the **sanctioned cross-module read path** (`ProductDetailsQuery` handler — STD-BE-005, not Catalog internals) and enforces "unit is a purchase unit of the product" (TS-PUR-020); query `PurchaseLineItems` + endpoints `GET/POST .../{id}/lines`, `DELETE .../lines/{lineId}`. Migration `20260720231856_PurchaseLineItems` (purchase_line_items — snapshot cols, qty(18,3)/money(18,2), cascade FK to invoices, **no cross-module FK**). Registered in both pipelines + DI; 4 new validation keys + ar/en; ErrorCatalogTests extended to cover PurchasingErrorCodes.
- **Frontend:** extended Purchase Details (no new page) — `PurchaseLineItemsComponent` section (table desktop / stacked cards mobile, RTL, server total, Draft-only add/remove) + `AddPurchaseLineDialogComponent` (VfDialog: active-product picker + purchase-unit select loaded on product change + qty/price + line-total preview, field-by-field validation). `PurchaseLinesStore` (reactive read + add/remove refresh, no optimistic UI). Added `ApiClient.delete` (completes ADR-0013) + `FormatService.decimal`. i18n `purchaseDetails.lines.*`.

**Gates (independently re-run this session, final review — all green):** `dotnet build` 0/0 Release · `dotnet format --verify-no-changes` clean · backend **261** (Domain **92** · Architecture **72** · Integration **97**) · `ef migrations has-pending-model-changes` = none · ESLint + Stylelint clean · `ng build` exit 0 (515.56 kB — TD-107 warning, **not raised**) · frontend **127**. **Browser verification (integration/unit-store vs live browser reported independently — owner reporting standard):** the automated gates above cover the integration (97) and unit/store (127) layers; the **live browser** run was done the **prior implementation session on this exact code** (headless Chrome/CDP, real stack — db :5434 + API :5080 + ng :4200: desktop 1440 + mobile 390, **dir=rtl, 0px horizontal overflow, zero console errors**; total **600.00 ج.م.** derived from 2 lines; add button drafts-only per AC-PUR-012; API curl add→201, non-purchase unit→400 `VTF-PUR-005`). The final-review changes are **markdown-only (zero runtime impact)**, so the browser run was **not re-driven** this session.

**Nine-question self-review: all NO** (no principle/ADR/standard breach; total logic in one place — the aggregate; boundary intact — isolation test green, cross-module read via the sanctioned query handler; minimal, no new abstraction/library/ADR; no invented business logic — advisor-confirmed NOT to gate on active-only server-side, that's a picker rule per DEC-PUR-003; docs Approved + synchronized). **Findings — both now owner-ruled (2026-07-21):** (a) line-total rounds half-**away-from-zero** to EGP 2dp → owner adopted this permanently, **not banker's** (BR-PUR-008/DEC-PUR-004); (b) row delete is direct (no confirm dialog) → owner confirmed **immediate, no confirmation** (DEC-PUR-005). **TD:** none new; TD-107 unchanged, not relaxed. **Dev DB** holds harmless verify data from the implementation session (category/manufacturer/product `PRD-000003` + 2 lines on `PUR-000001`) — DB state only, never part of any commit.

**Next (NOT started — owner STOP in force):** Purchasing Slice 5 — Receive (the inventory-ledger ADR lands there). Do NOT begin receiving / inventory movement / the next slice — wait for the next implementation session. Pre-existing untracked (intentionally not committed): `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Session close (2026-07-17) — Purchasing Slice 3 (Create Purchase): COMPLETED, owner-APPROVED, COMMITTED (`375e2ab` + this docs commit)

**Slice 3 = COMPLETED.** The Create Purchase vertical slice is implemented, fully gated, live-verified,
owner-approved, and COMMITTED as two commits (`375e2ab` `feat(purchasing): Create Purchase (Slice 3)` —
backend + frontend + tests; this `docs(purchasing): synchronize repository after Create Purchase` —
STATUS/module docs/_INDEX, no mixing). **Not pushed** (no remote — standing owner item). Owner directed:
do not squash, do not push.

**DoR (owner-approved 2026-07-17, prior session).** Drafted minimal Slice-3 docs; owner approved with the
explicit ruling **Total = 0 on create** (DEC-PUR-002). Approved IDs: **REQ-PUR-003** (create header only —
supplier name required · reference optional · invoice date required · notes optional; number auto-`PUR-`;
born **Draft**; **total 0 EGP**; success → Details) · **AC-PUR-006** (success) · **AC-PUR-007**
(field-by-field validation) · **TS-PUR-012..015** · **DEC-PUR-002** (Total 0; BR-PUR-001 total clause
time-scoped to a later line-items slice). All five module doc files Approved + sign-off.

**Scope delivered (exactly the approved docs — nothing else):** the first write path in Purchasing.
- **Backend:** `Application/Purchasing/Commands/CreatePurchaseInvoice/` (Command · Result · Validator —
  supplier + invoiceDate required, length caps reuse `TextTooLong`); `Infrastructure/Purchasing/
  CreatePurchaseInvoiceCommandHandler.cs` (allocates the `PUR-` number from `purchase_invoice_number_seq`,
  builds the aggregate Draft/**total 0**/`TimeProvider.GetUtcNow()`, one `SaveChanges` — mirrors
  `CreateProductCommandHandler`); `Api/Endpoints/Purchasing/CreatePurchaseInvoiceRequest` +
  **`POST /api/v1/purchase-invoices`** (201 + Location → Details). Registered: Infrastructure DI (handler +
  `AddSingleton(TimeProvider.System)`), `CommandPipeline`, Application DI (validator); new
  `ValidationMessageKeys.SupplierNameRequired`/`InvoiceDateRequired` + ar/en resx. **No migration**
  (table/sequence/indexes exist from Slice 1). **No new architecture/library/ADR.**
- **Frontend:** `purchase-create-page.component.ts` mirroring the approved product-editor create path
  (STD-FE-004) — typed reactive form (STD-FE-016), submit → markAllAsTouched → invalid returns, POST →
  success navigates to `/purchases/:id`, error → banner, `errorFor` on submitted/touched. Route
  `/purchases/new` **before** `:id`; list header «إنشاء فاتورة شراء» button + wired empty-state CTA (both
  → `/purchases/new`); `VfDateInput` additive `error`/`required` inputs (backward-compatible); i18n
  `purchaseCreate.*` + `purchases.create`. Supporting models/forms/api service (from the prior session).

**Gates (independently re-run):** `dotnet build` 0/0 Release · `dotnet format` clean (also fixed stray LF
line-endings in the new integration test) · backend **231** (Domain **80** +1 · Architecture **64** ·
Integration **87** +4) · frontend **118** (+5) · ESLint + Stylelint clean · `ng build` exit 0 (initial
538.18 kB — TD-107 warning budget, not raised). **Live-verified (headless Chrome/CDP, real stack — db :5434
+ API :5080 + ng :4200, dev-seeded):** desktop 1440 + mobile 390 both `dir=rtl`, **zero horizontal
overflow**, **zero console errors**; empty submit → 2 field errors «هذا الحقل مطلوب.», no navigation
(AC-PUR-007); create round-trip → Details `PUR-000007`, badge **مسودة**, reference **—**, total **0.00 ج.م.**
(DEC-PUR-002), zero console errors (AC-PUR-006). API curl: 201 + Location and per-field 400.

**Nine-question self-review: all NO** (no principle/ADR/standard breach; mirror-not-duplicate; boundary
intact; minimal; no invented logic — no date cap, no total input; docs were Approved last session so
code-only; no ADR owed). **Findings:** none blocking — the empty-state CTA was wired but not live-driven
(dev DB non-empty); it calls the same `goToCreate()` handler as the live-verified header button. **TD:**
none new; TD-107 unchanged and not relaxed. Dev DB holds a few harmless verify invoices (`PUR-000006/007`).

**Next (NOT started):** Purchasing Slice 4 — Receive (per the Sprint-4 order). Pre-existing untracked
(intentionally not committed): `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Session close (2026-07-17) — Purchasing Slice 2 (Purchase Details): COMPLETED, owner-APPROVED (final), COMMITTED (`a0cf372` + this docs commit)

**Slice 2 = COMPLETED.** The read-only Purchase Details vertical slice is implemented, fully gated,
live-verified, owner-approved (final rulings applied), and COMMITTED as two commits (`a0cf372`
`feat(purchasing): Purchase Details (Slice 2)` — implementation + tests; this
`docs(purchasing): synchronize repository after Purchase Details` — STATUS/_INDEX/module docs/TD ledger,
no mixing). **Not pushed** (no remote — standing owner item). Owner ended the session with an explicit
stop: do NOT start Slice 3 / Create Purchase.

**Scope delivered (exactly the approved docs — REQ-PUR-002, AC-PUR-004/005, TS-PUR-008..011):** open any
invoice from the list (table row click · keyboard **Enter** · mobile card tap → `/purchases/:id`) into a
read-only Details page showing the complete header in the **frozen canonical order** (owner ruling): 1 رقم
النظام · 2 الحالة (شارة) — both in the minimal header — then 3 المورد · 4 مرجع المورد · 5 تاريخ الفاتورة ·
6 الإجمالي (EGP) · 7 تاريخ الإنشاء · 8 الملاحظات. Four data states (تحميل/بيانات/غير موجود/خطأ), RTL,
responsive, zero overflow. Empty/null notes → standard placeholder «لا توجد ملاحظات» (owner standard).
**Nothing else** — no edit/receive/inventory/line-items/cost/taxes/discounts/payments/supplier-CRUD (scope lock held).

**Architecture:** reused the query pipeline + CQRS-lite projection (ADR-0014 §5), RFC-9457 404, `MoneyDto`,
the existing `PurchaseInvoiceStatusDto`, `FormatService`, the existing `PurchaseStatusBadgeComponent`, and
the VetFlow UI Kit — mirroring the Catalog Product-Details screen (STD-FE-004 mirror-without-importing).
New: `PurchaseDetailsQuery`/`Validator`/`Dto` (Application) + `PurchaseDetailsQueryHandler` (Infrastructure)
+ `GET /api/v1/purchase-invoices/{id}` (404 → NotFound); frontend `purchase-details` feature + route
`/purchases/:id`; list `open` outputs wired on the table/cards for row navigation. **No new architecture,
no new library, no new ADR.**

**Owner rulings applied (2026-07-17 final review):** (1) routing decision documented in `ui.md`
(`/api/v1/purchase-invoices/{id}` API + `/purchases/:id` frontend — "frontend optimized for navigation,
backend resource-oriented"); (2) header minimal = number + status badge only (supplier moved out of the
header — fixed in self-review); (3) canonical Details order **frozen** in `ui.md` (no reorder without a UX
decision); (4) empty/null notes → «لا توجد ملاحظات» standard; (5) navigation via row click + Enter + card
tap verified; (6) intentional-404 browser noise reported as **expected**, not a defect; (7) **TD-109**
logged (null supplier reference must not render an empty label — future UI consistency pass, **NOT modified
in Slice 2**).

**Gates (independently re-run):** `dotnet build` 0/0 Release · `dotnet format` clean · backend **224**
(Domain **79** · Architecture **62** · Integration **83**, +4) · frontend **113** (+4) · ESLint + Stylelint
clean. **Live-verified (headless Chrome/CDP, real stack — db :5434 + API :5080 + ng :4200, dev-seeded 5
invoices):** list→details via row click, keyboard Enter, and mobile card tap; received/cancelled/no-ref(—)/
null-notes(«لا توجد ملاحظات»)/not-found(«فاتورة الشراء غير موجودة») all rendered; desktop 1440 (table) +
mobile 390 (cards); dir=rtl and zero horizontal overflow at both on list/details/not-found; **zero
application console errors** (the only 404 is the intentional missing-id fetch — expected). API curl: full
header round-trip + 404.

**Next (NOT started, per owner):** Purchasing Slice 3 — Create Purchase. Untracked/intentionally not
committed (pre-existing): `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

## Session close (2026-07-17) — Purchasing Slice 1 (Purchase List) DONE, owner-APPROVED (final), COMMITTED (`c2669f7` + this docs commit)

**The Purchase List vertical slice is implemented, fully gated, live-verified, owner-approved, and
COMMITTED as two commits** (`c2669f7` `feat(purchasing): Purchase List (Slice 1)` — implementation +
tests; this `docs(purchasing): synchronize repository after Purchase List` — STATUS/_INDEX/GLOSSARY/
module docs/TD ledger, no mixing). **Not pushed** (no remote — standing owner item). Owner ended the
session with an explicit stop after the two commits: do NOT start Purchase Details / Slice 2.
*(Superseded 2026-07-17 — Slice 2 has since been implemented, approved, and committed; see the top entry.)*

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

**Next:** Purchasing Slice 2 — Purchase Details **(since COMPLETED — see the top entry).** Untracked and
intentionally not committed (pre-existing, prior sessions): `docs/releases/`, `docs/ui/product-editor-ux-architecture.md`.

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
