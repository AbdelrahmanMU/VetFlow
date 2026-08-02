# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-08-02 · nineteenth cycle · **THE «SAAS FOUNDATION» EPIC (ADR-0022, Accepted) IS IMPLEMENTED IN FULL, GATED GREEN, BROWSER-VERIFIED, OWNER-APPROVED, AND COMMITTED AND PUSHED** — single commit **`d187dfa`** «feat(saas): SaaS Foundation Epic — multi-tenancy, identity, scope-owned numbering» (**141 files, +15 512/−305**) on `pilot/docs-fixes-and-cloud-deployment`; **local = remote**, the branch is **9 commits ahead of `origin/main`**, and `main` is deliberately **not** fast-forwarded (it would otherwise carry ADR-0021 while it is still `Proposed` — the same condition as the last two cycles). **The full commit gate was re-run green immediately before committing and again after; the tree is clean.** **Phase 0 and the Phase 1/2 backend rode in this commit rather than a separate one** — they had been uncommitted since the previous session, and splitting them would have left a commit where tenancy exists without the isolation that makes it defensible; **the commit message says so rather than hiding it.** **Phases 1–4 all landed.** **PHASE 1 REMAINDER — the clinic clock is now tenant-resolved (DEC-ORG-007, AC-ORG-009):** `ClinicClock` was a singleton resolving **one configured zone at construction**, correct for at most one tenant; it now reads the **tenant's own** zone through a per-tenant cache (`TenantTimeZones`), and **BR-INV-060 is unchanged — only its source moved.** UTC fallback stays prohibited on both ends: `Clinic:TimeZone` still **refuses to boot** when absent or unresolvable (it is now the value a newly seeded clinic starts with, ADR-0022 §10), and an unresolvable **tenant** zone throws rather than guessing. **Proved by two tenants 26 hours apart producing different clinic dates** — a gap no configured-zone implementation could show. **⛔ THE LARGEST FINDING OF THE CYCLE, FOUND BY AUDIT AND NOT BY A TEST: ROW-LEVEL SECURITY DID NOT EXIST.** ADR-0022 §8 makes **four** mitigations mandatory and §12.7 declares the shared-database decision of §2 **void** without all four; three were in place (query filters · the architecture test · composite foreign keys) and **the database-level second net was simply absent** — zero policies, no `app.tenant_id`, nothing. **It is now implemented and, more importantly, it is now EXERCISED.** Policies on **all 16 tenant-scoped tables**, `ENABLE` **and** `FORCE` (an owner is exempt from its own policies unless forced), the predicate **null-safe** so a connection with no tenant published matches **no** rows rather than every row — **fail-closed by construction**. **DEC-ORG-012 records the operational constraint that makes it real: PostgreSQL exempts a `superuser` from RLS unconditionally, so an application running as one would install the second net and never once execute it.** Neon's deployed role is unprivileged — **and the integration suite now creates and runs as an unprivileged role too**, so all 268 tests meet the same policies production does. **This was verified empirically before it was adopted, not assumed.** **RLS immediately earned its keep: it exposed a real latent defect** — two hand-built test contexts wrote `inventory_movements` rows with an **empty tenant id**, invisible to every filtered read and undetectable before the policies existed. **Fixed at the wiring, not at the assertion.** **PHASE 3 — the five database-global sequences are DROPPED.** Numbers now come from a counter row per `(tenant, scope, series)`, allocated with `INSERT … ON CONFLICT DO UPDATE … RETURNING` **inside the transaction that inserts the document** — so the allocator **refuses to run outside one** (structural, not a comment). **Gapless by owner ruling:** proved by a failed create leaving the counter exactly where it was, and by five concurrent creates taking five **consecutive** numbers. **The format is byte-identical** — `PUR-000001` in the browser, same prefixes, same six digits. **A second clinic's first invoice is `PUR-000001`, not `PUR-000002`** (§12.12), proved end to end through a real second tenant that signs in through the real endpoint. `PRD-` is tenant-scoped, `PUR`/`SAL`/`PRT`/`SRT` branch-scoped. **PHASE 2 — the login screen and its plumbing.** `/login` outside the shell (there is no navigation to offer someone who is not signed in), a `CanMatch` guard so an unauthenticated visit never even downloads a feature bundle, an HTTP interceptor attaching the bearer token and turning a refused one into a **visible** return to the login screen, and sign-out at the foot of the sidebar. **The nine approved Arabic strings are used verbatim and no tenth was invented.** **The landing route is unchanged — `catalog/products`, no dashboard built (DEC-IDN-007).** «انتهت جلستك» is distinguished **on the client** by whether a token was actually held (DEC-IDN-016) — a request sent with no token was never a session, and is not told one ended; **no second server contract was invented for it.** The token lives in `localStorage` (DEC-IDN-015) because BR-IDN-009 allows a session to end **only** by explicit sign-out or expiry, and an in-memory token would end it by a **third** route: a page refresh. **⛔ SECOND FINDING, ALSO UNIMPLEMENTED: `ICurrentUser` WAS REGISTERED AND NEVER CONSUMED** — so **BR-INV-066 as amended** (every operation belongs to an authenticated performer, REQ-IDN-008 / AC-IDN-011) did not exist. **Now implemented in the DEC-ORG-011 shape**: a shadow `PerformedByUserId` stamped from the token's claims by an interceptor, so **no call site carries it and none can forget it**, and an **unauthenticated write is refused rather than attributed to nobody**. **`ActorName` is untouched** — historical values stay readable, it is simply no longer the source. Pinned by an architecture test **and** by a test proving a client-supplied `actorName` changes attribution not at all. **⛔ THIRD FINDING, CAUGHT IN SELF-REVIEW: ADR-0022 §5's LAST SENTENCE WAS UNMET** — «the GIN trigram indexes must be rescoped alongside their unique B-tree counterparts». The unique indexes had been rescoped; **the six tenant-scoped GIN indexes had not** (categories · manufacturers · products ×2 · purchase invoices · sales invoices). **Not a leak** — the filter and RLS both still apply — **but an explicit «must» in an Accepted ADR, and it was about to be reported as satisfied.** Now rescoped to lead with the tenant, which needs the **`btree_gin`** extension to carry a `uuid` inside a GIN index; **it is a trusted extension and the unprivileged role created it successfully in the verification database before it was adopted** (the `pg_trgm` precedent). **`product_natures` is deliberately left alone** — platform-global, no tenant to lead with. Recorded as **DEC-ORG-016**. **GATES, ALL GREEN: backend build 0 warnings / 0 errors · `dotnet format` clean · Domain 163 · Architecture 140 (+1) · Integration 268 (+25) · frontend ESLint clean · Stylelint clean · 332 unit tests (+17) · `ng build` exit 0 (600.64 kB, +4.60 kB over the branding cycle — TD-107's budget warning is pre-existing and unchanged in kind).** **TRACEABILITY: the 29 approved scenarios of the two new modules are now named in test method names — TS-IDN-001..015 and TS-ORG-001..013 map onto real tests; TS-IDN-016 is the browser script, which is a run and not a named test.** **LIVE BROWSER VERIFICATION 45/45 at 1440×900 AND 390×844, zero console errors** — headless Chrome over CDP against the real API and a real PostgreSQL **running as the unprivileged role**: the guard, the three validation moments, the single failure message, the banner taking focus and clearing on edit, a submit button never disabled for invalidity, the ruled landing screen, sign-out, a **refused token** producing `/login?expired=1` with the approved sentence, the ten existing screens as regression, and the 44 px touch target. **It found one real gap the unit tests could not: the login action was 39 px tall on the phone** — the compact tier's own navigation holds itself to 44, so the full-width action now does too. **The run also re-checks, deliberately, the exact defect the seventeenth cycle found in the browser and could never find in jsdom (which implements neither `visibility` nor `inert` for focus): opening the drawer at 390 must move focus INTO it. It does — and it matters more now than then, because the shell moved from the application root into a routed layout, so its focus deferral runs on a router-driven mount.** **Performance: p50 5–10 ms across five endpoints under RLS; the extra `set_config` per connection open is not measurable at clinic volume.** **FIVE IMPLEMENTATION-TIME DECISIONS RECORDED IN THE ESTABLISHED «اكتُشف وقت التنفيذ» IDIOM rather than deviating silently: DEC-ORG-016** (the GIN indexes lead with the tenant, and `btree_gin` is what makes that expressible) · **DEC-ORG-012** (RLS needs `FORCE` and a non-superuser role) · **DEC-ORG-013** (the tenant is published per connection open and cleared when absent — pooled connections would otherwise hand one tenant's scope to the next borrower) · **DEC-ORG-014** (one `scope_id` column instead of a nullable branch column — a nullable member of a primary key is not expressible in PostgreSQL; and `ON CONFLICT` is `UPDATE … RETURNING` plus first-use creation, so a new branch needs no provisioning step) · **DEC-ORG-015** (the tenant time zone is cached for the process; changing it directly in the data needs a restart — accepted because no screen can change it) · plus **DEC-IDN-015/016** for the client session. **UI Kit: `vf-text-input` gained `password`/`tel`, `inputMode`, `autocomplete` and `digitsFirst`; `vf-button` gained `full` — all additive, recorded in `components.md`.** **NOT DONE, DELIBERATELY AND NAMED: attribution was implemented on the movement ledger only** — that is where BR-INV-066 rules it and where a recorded attribution concept exists; a `created_by` on all seventeen tables was **not** invented. **NEXT: the code is on the branch and the Pilot's two blockers are gone; what stands between here and a real Pilot is deployment — the Render service still runs the pre-Epic image, `Jwt__SigningKey` must be generated and set as a secret before it is redeployed, and the Neon role must be confirmed non-superuser (DEC-ORG-012) or the second isolation net is inert in production.**

**Earlier (eighteenth cycle):** 2026-08-02 · session closed (`/close-session`) · **MULTI-TENANCY IS RULED. THE «SAAS FOUNDATION» EPIC IS COMMISSIONED AND STOPPED AT ITS DEFINITION OF READY — PHASE 0 (DOCUMENTATION) IS DELIVERED AND AWAITS OWNER APPROVAL. NO CODE WAS WRITTEN, NOTHING COMMITTED.** **A complete Organization Architecture Design Review was commissioned and delivered** (twelve ruled sections, in-session, grounded in `file:line` evidence rather than in STATUS prose). **THE REVIEW INVERTED THE PREVIOUS RECOMMENDATION and retired two of this repository's own recorded assumptions on the record: (1)** «database-per-clinic → the retrofit is ZERO» is **false once branches exist** — a branch cannot be a database boundary (transfers · consolidated reporting · one login across branches), so a physical-scope column lands on inventory and documents **in every model**, which makes the marginal cost of a tenant discriminator one column and one predicate; **(2)** «shared DB breaks numbering because clinic B starts at `PUR-000002`» is an argument against **global sequences**, not against a shared database — and those must be replaced in either model. **A third correction: `database-platform-study.md:172` («per-tenant databases are free to multiply») is about LICENSING vs SQL Server, not about hosting floors or provisioning pipelines — I had misapplied it as evidence for database-per-clinic.** **OWNER RULING (2026-08-02): shared database with a tenant discriminator · Tenant → Branch → Membership → User · phone + password authentication before the Pilot starts · scope-owned numbering · «no temporary bypasses, no fixed TenantId in code, no future rework».** **THE EPIC WAS COMMISSIONED AND CORRECTLY STOPPED BEFORE ANY CODE — `ADR-0017 §5` (Definition of Ready) requires the module's six documents to exist AND be Approved, and says «anything missing → stop and ask the owner. Never fill the gap by inventing.» TWO MODULES' DOCUMENTATION DID NOT EXIST.** Five blockers were reported with citations: no ADR for the organization model · **BR-INV-066 (Approved) states «لا وحدة مستخدمين، ولا مصادقة… في الـ MVP» — a direct contradiction with the current task, which the contradiction policy says must STOP** · two new libraries need owner approval (verified: **zero** auth packages and **zero** auth wiring exist) · ADR-0010's Consequences left the token mechanism explicitly unruled · GLOSSARY had **zero** rows for منشأة/فرع/عضوية/مستخدم. **TWO OWNER QUESTIONS WERE PUT AND ANSWERED: (a) «Dashboard» in the ruled flow means LAND IN THE APP — the existing product list; NO dashboard is built (it is excluded by the inventory scope-lock and Reports is Post-Pilot) → DEC-IDN-007; (b) numbering becomes GAPLESS** — `nextval` does not roll back, so today a failed save **burns a number permanently**; a counter row inside the transaction does roll back, and it is achievable here because **no document has a delete or cancel path** (DEC-INV-037). **PHASE 0 DELIVERED — 11 files, documentation only, `git status` shows NOT ONE code file touched: `ADR-0022` «Organization model, multi-tenancy and identity foundation» (Proposed, indexed)** · **two new modules with the full 8/8 document set — `organization/` (REQ-ORG-001..008 · BR-ORG-001..008 · AC-ORG-001..011 · TS-ORG-001..013 · DEC-ORG-001..008) and `identity/` (REQ-IDN-001..009 · BR-IDN-001..010 · AC-IDN-001..013 · TS-IDN-001..016 · DEC-IDN-001..008)** · **three amendments made IN PLACE with every superseded wording preserved verbatim (the BR-CAT-020 / BR-INV-042 precedent): BR-INV-066** (attribution moves from optional free-text `ActorName` to an authenticated user; **the historical `ActorName` values stay readable and are neither deleted nor rewritten**), **ADR-0010** (token mechanism now specified — one JWT, no refresh; **the permission model stays unruled**), **ADR-0021** (Phase 2 now lands before the Pilot — which **strengthens** its boundary rather than relaxing it; **its topology is CONFIRMED, not amended**, because ADR-0022 ruled a shared database) · **BD-PRD-001 amended** (single-clinic product → multi-tenant SaaS) and `PROJECT_CONTEXT.md` with it · **GLOSSARY gained nine rows** and the «Veterinary clinic» row was corrected. **THE DECISIVE ARCHITECTURAL FINDINGS, all verified against code: `product_on_hands` has `HasKey("ProductId")` — a per-product singleton, and the single most expensive structure to change after real data** · `inventory_batches` and `inventory_movements` carry **no location** · **seven globally unique indexes, zero check constraints** · **`HasQueryFilter` appears ZERO times** — no tenant-scoping mechanism exists at all · **but the raw-SQL surface is only 6 sites and every one is `nextval` — there is NOT ONE raw read in the codebase**, so a global query filter genuinely covers the read surface (this is what makes a shared database defensible here) · the DbContext takes **one static connection string** and the bootstrap **migrates exactly one database**, so database-per-clinic could not even be expressed without rearchitecting it · **and a blind spot nobody had flagged: `ClinicClock` is a SINGLETON resolving ONE configured time zone at construction (`ClinicClock.cs:20-27`) — expiry safety decisions depend on it (BR-INV-059/060), it is correct for at most one tenant, and it contradicts ADR-0007's recorded «tenant-specific localization must remain possible». It becomes tenant-resolved in Phase 1; BR-INV-060 itself is UNCHANGED — only its source moves (DEC-ORG-007).** **THE ONE CONSTRAINT THAT MUST NOT BE LOST: the owner ruled TWO levels (no StockLocation). That stays free ONLY under ADR-0022 §11.1 — «a future warehouse is modelled as a Branch, not a new level below it». If anyone later adds a stock-location level, the primary-key change on a live `product_on_hands` is back. Recorded in ADR-0022 §11.1, DEC-ORG-002 and the GLOSSARY.** **⛔ THREE BLOCKING OWNER QUESTIONS REMAIN, DELIBERATELY NOT INVENTED (ADR-0017 §5): OQ-IDN-1 access-token lifetime** (proposed 12 h — covers a clinic working day without interrupting the cashier, and expires between days; there are no refresh tokens and no «remember me», so expiry means re-login) · **OQ-IDN-2 failed-attempt behaviour** (proposed **no lockout** — with one user and no reset path and no user administration, a lockout is a lock with no key) · **OQ-IDN-3 the nine new Arabic strings** for the login screen, proposed in `identity/ui.md`. **NEXT: owner approves ADR-0022 + the two modules + the three amendments and answers OQ-IDN-1..3 → then Phases 1–4 run continuously under Epic rules, stopping only at the seven completion conditions with an Epic Owner Report, and committing nothing until Epic Commit Approval.** **THE DEADLINE IS UNCHANGED AND IS THE WHOLE POINT: ADR-0020 defines the Pilot's start as the first real operational entry, which has NOT happened; the database is verified empty and all five sequences verified never-called. Every change above is near-free today. After the first real entry it is a primary-key rewrite, a back-fill of an append-only ledger, and a change to accounting series a bookkeeper can see — and the second customer cannot be onboarded until it lands, so it would land under commercial pressure on live data.**

**Earlier (seventeenth cycle):** 2026-08-02 · **THE «PILOT UX POLISH» EPIC AND THE PILOT BRANDING POLISH ARE COMPLETE, GATED GREEN, BROWSER-VERIFIED, OWNER-APPROVED, AND COMMITTED AND PUSHED** — single commit **`6c1ed0e`** «feat(ux,brand): Pilot UX Polish Epic + premium visual identity» (49 files, +2 485/−33) on `pilot/docs-fixes-and-cloud-deployment`; **local = remote**, the branch is **7 commits ahead of `origin/main`** (`6c1ed0e` plus `0e0f59d`, a STATUS correction — the file still claimed «NOTHING COMMITTED» after the push, and leaving the state file contradicting the repository is on the ai-governance Never list). **`main` was NOT fast-forwarded** — it would otherwise carry ADR-0021 while it is still `Proposed`. **The two units ride in ONE commit deliberately** — they interleave in `shell.component.ts`, `design-language.md` and `STATUS.md`, and splitting them meant hunk surgery on Arabic documentation (a recorded hazard); **the commit message says so rather than hiding it.** Full commit gate re-run green immediately before committing **and again after**. **SECOND OWNER REVIEW (2026-08-02): the owner approved the Epic but REFUSED the suspension of the S2 Inventory card** — «Product Details is the primary inspection screen… do not violate any approved business rule · do not aggregate in the browser · do not duplicate Inventory logic · do not bypass approved APIs · investigate the correct architectural solution; if a new read projection is correct, build it.» **BUILT: `GET /api/v1/inventory/{productId}/summary` — REQ-INV-012, a read projection OWNED BY INVENTORY.** The two rule-breaking candidates were rejected on the record in **DEC-INV-040**: a product filter on `GET /api/v1/inventory` **violates BR-INV-014** («حصرًا»), and summing batch pages in Angular **duplicates BR-INV-008 outside its owner**; putting on-hand on the Catalog product contract would make Catalog narrate an Inventory fact. **No business rule was amended, no ADR changed, no migration added, no existing contract altered.** On-hand is read **as stored** (BR-INV-008 — never summed); count and nearest expiry are scoped to **active batches** (BR-INV-009/010); «never received» is an explicit zero with a flag, **not a 404** (only a non-existent product 404s — the REQ-INV-003 precedent). **EF cannot translate a shared helper inside an expression tree, so the «active batch» predicate necessarily exists in two handlers — the guard is a TEST, not a comment: AC-INV-065 requires the summary to agree FIELD FOR FIELD with the inventory projection.** The card loads **independently** of the product, so a stock outage surfaces a retry in one card instead of blanking a page whose other five loaded. **Docs: REQ-INV-012 · AC-INV-061..065 · DEC-INV-040 · `catalog/ui.md` §4 card 7 un-suspended and marked implemented.** **The owner lifted Pilot Observation Mode for this Epic** and commissioned three phases. **A Product Improvement Tracker was opened first** (`docs/operations/uat/product-improvement-tracker.md`, Observation mode, append-only, stable IDs) and the owner filed **PIT-001..004**; the Epic message then approved all four. **Four owner rulings were obtained before any code**, because the Epic collided with Approved artifacts: **(1)** Phase 1 contradicted **design-language §5**, which rules «تنقّل سفلي» (bottom navigation) for الجوال while the Epic asked for a drawer on both tiers → owner **amended §5** so the collapsible sidebar is the pattern on اللوحي **and** الجوال (recorded in place, the superseded wording preserved); **(2)** PIT-002 asked for **centered** money while §6 already rules **الأرقام لليسار** — what the owner saw *was* the approved standard, partially applied → owner ruled **implement §6 as written**; **(3)** date/time → **two lines, keep ص/م**; **(4)** the S2 Inventory card → **approve the §4 amendment**. **THE LARGEST FINDING: most of the Epic already existed in Approved documentation and was simply unimplemented** — the **Product Details page (S2) already existed** at `/catalog/products/:id` with units, conversion factors and a price per sale unit; `catalog/ui.md` §3 **already required** «إجراءات مباشرة على الصف: تفاصيل» and «Enter يفتح التفاصيل»; a **centralized `FormatService` already existed** (STD-FE-042) with **zero** ad-hoc date formatting anywhere. So PIT-001/004 were **Bugs against approved docs**, needing no new REQ/BR/AC/ADR. **Built:** the shell sidebar becomes a **drawer at ≤768 px** (hamburger · slides from the right · backdrop/`Esc`/destination close it · focus in and back · `Tab` trapped · `inert`+`aria-hidden` when closed · 44×44 · `position: fixed` so nothing shifts) with **desktop unchanged**; **rows and mobile cards open S2** on click and `Enter` plus a visible «فتح التفاصيل» action; the numeric/money standard now has **one definition** (`web/src/app/shared/styles/_numeric.scss`) included by the three raw-`<table>` deviations under TD-007 (`<vf-table>` already complied); `FormatService.dateTimeParts()` renders **date over time** in the movement history. **⛔ NOT BUILT — the S2 Inventory card, stopped on a contradiction found at implementation time:** the owner approved it on my stated premise that existing endpoints suffice, and **that premise proved false** — `GET /api/v1/inventory` carries exactly the right fields but **cannot filter by product**, and **BR-INV-014 declares the filter list «حصرًا»**; the batch endpoint is paged with no aggregate, so summing in the browser would **re-implement BR-INV-008 outside its owner**; and the product read contracts **carry no stock at all** (though §3 lists «الرصيد» as a column — a second approved-but-unimplemented gap). `catalog/ui.md` §4 now records the card as **approved but suspended**, with three ways forward for the owner. **Gates at close: frontend 314/54 files (+15) · ESLint clean · Stylelint clean · `ng build` exit 0 (TD-107: 595.39 kB, +11.41 kB) · backend build 0/0 · `dotnet format` clean · Domain 163 · Architecture 134 (boundaries intact) · Integration 243 (+7). Live browser 40/40 at 1440×900 and 390×844 via headless Chrome/CDP, zero console errors** — the run confirms the Inventory card against the real database («الرصيد الحالي 248 علبة · عدد الدفعات 8») **and asserts from the network log that the number came from `/api/v1/inventory/{id}/summary`, not from anything Catalog-owned** — **it found one real defect the unit tests could not**: the drawer opened with focus left behind, because an element whose `visibility` is mid-transition cannot take focus and **jsdom implements neither `visibility` nor `inert` for focus**; fixed by deferring the visibility change to the close direction only, re-verified 33/33. **Mutation-checked** (disabling the `inert` guard fails its test). **Note: `:4200` had a STALE dev server — verification deliberately ran on a fresh `:4500`**, avoiding the trap that produced a bogus report in an earlier cycle. **ALSO DONE THIS CYCLE — PILOT BRANDING POLISH (owner-commissioned implementation task), COMPLETE, VERIFIED, APPROVED AND COMMITTED IN `6c1ed0e`.** A full visual identity was designed, generated and integrated. **The design was driven by the Approved design language, not invented:** §11 «لون أساسي واحد» → the mark uses **only `--vf-primary` #0f766e**, no second brand hue; §11/§2 «لا تدرجات لامعة» → **flat fills, zero gradients**; §12 «عائلة أيقونات واحدة… لا يُخلط بين عائلتين» → **the mark is IDENTITY, never a UI icon**, which is the documented reason empty states were deliberately NOT branded. **The mark:** a solid tapered **V** — broad arms converging to one point, right arm a unit higher (a deliberate forward lean = «Flow») — white on a rounded primary tile. **ONE silhouette at every size**; large sizes get optical correction only, never an added element. **Three earlier concepts were drawn, rendered at 16–180 px and rejected on sight, recorded so they are not retried:** a chevron in a rounded cradle (**read as «W» and acquired an unintended animal-face** — exactly what the brief forbids) · a diagonally sheared point (**read as damage**) · two blades with a channel (**lost the V, regained the «W» ambiguity**). **Package** `web/public/assets/branding/`: `logo.svg` · `logo-dark.svg` · `icon.svg` · `favicon.svg` · `favicon.ico` (**real 3-entry ICO, 16/32/48**) · `apple-touch-icon.png` (**180, full-bleed on purpose — iOS applies its own corner mask, so shipping rounded corners would round twice**). Rasters are **true Chrome renders of the same SVG**, and **the ICO was validated by decoding it back** — every directory offset lands on a PNG signature and each IHDR matches its declared size. **Integration: one source only** — a new **`<vf-logo>`** UI-Kit component (registered in `components.md`, STD-UX-127) replaces the text brand in **both** shell locations; `index.html` gained SVG-first favicon + `.ico` fallback + **`apple-touch-icon` link** (without it the file is never loaded) + `theme-color`; **the stale 15 KB Angular `favicon.ico` was deleted from the web root** so branding has exactly one home. **The accessible name is preserved** — the mark carries the product name as `alt`, and a test pins it (the existing app spec caught the regression when the text brand was removed). **Artwork is referenced, never inlined**, so it stays out of the JS bundle. **NOT DONE, deliberately, each with a reason:** login page (**does not exist — no auth by design, DEC-INV-030**) · splash screen (**not present; the brief said «if present», and adding one is redesign**) · empty states (**§12 forbids mixing icon families**). **Gates: frontend 315/54 (+1) · ESLint · Stylelint · `ng build` exit 0 (596.04 kB, +0.65 kB over the Epic — artwork is not bundled) · backend untouched. Browser verification 18/18** at 1440 and 390 (favicon links, apple-touch link, theme-color, logo loaded with `naturalWidth > 0`, accessible name, single brand instance, no icon-slot use, no collision with the hamburger or drawer close button, root `/favicon.ico` now 404, zero console errors) **plus the UX Polish suite re-run as regression: 40/40 still green.** **A design-review artifact is in the repo: `docs/ui/branding-contact-sheet.png`** (16→180 px, tab simulation, light/dark lockups, in-situ sidebar and mobile bar). Identity rules recorded as an **owner-approved §2 amendment** in `design-language.md`.

**✅ EPIC COMPLETE AND SHIPPED — «SAAS FOUNDATION» (approved 2026-08-02, four rulings + the Future SaaS Constraints section). PHASES 0–4 ALL LANDED AND GREEN; the seven completion conditions were met, the owner gave Epic Commit Approval, and the work is committed (`d187dfa`) and pushed. The «REMAINING» list at the end of this block is superseded by the nineteenth-cycle banner above: the clock, the login screen, the numbering and the verification are all done, and two gaps this block did not know about (row-level security, and attribution never being wired) were found and closed.** **Owner rulings recorded:** ADR-0022 **Accepted** · both modules **Approved** (14 status lines flipped) · amendments accepted · **OQ-IDN-1 = 12 h · OQ-IDN-2 = no lockout during the Pilot (Pilot-scoped, §12.15, expires on review) · OQ-IDN-3 = the nine Arabic strings approved · OQ-IDN-4 = phone numbers globally unique (§12.14)** · **ADR-0022 §12 «Future SaaS Constraints» added — seventeen invariants, each with its failure mode written down, because an invariant whose failure mode is not recorded is not enforceable.** **PHASE 1 DONE SO FAR: domain** `Tenant · Branch · Membership · MembershipRole · User` (Membership holds a plain `Guid UserId`, never an Identity type — the REQ-INV-008 precedent, so Organization and Identity stay isolated under STD-BE-005) · **abstractions** `ITenantContext` + `ICurrentUser` in Application, provider-neutral per ADR-0010 · **`TenantScope`** (shadow discriminators + `ScopedToTenant`/`ScopedToBranch`/`PlatformGlobal` + one filter pass applied after every configuration so none can forget it) · **`TenantStampInterceptor`** (writes stamped automatically; **an unresolved scope refuses the write — no default tenant, §12.6**) · **`SystemTenantScope`** (AsyncLocal, seeding/bootstrap/tests only) · **`RequestTenantContext`** (claims-only, **singleton by necessity**) · **all 17 existing configurations declare a scope** · **7 unique indexes rescoped** · **composite tenant FKs** on Product→Category/Manufacturer and all five parent→child aggregates · **`pk_product_on_hands` changed from `(product_id)` to `(tenant_id, branch_id, product_id)`** — the migration this whole Epic exists to do while it is free · **migration `20260802102605_OrganizationAndIdentityFoundation` generated and inspected.** **Gates so far: solution builds 0 warnings / 0 errors · architecture tests 139 (was 134) — five NEW tenant-isolation tests make ADR-0022 §12.1/§12.6/§12.7 build-enforced: every entity must declare a scope, only the six recorded tables may be global, every scoped entity must have a filter and both discriminator columns, and an unresolved scope must throw rather than default.** **TWO DESIGN TRAPS FOUND AND CLOSED, both of which would have shipped silently: (1) the query filter captures the tenant context ONCE because EF caches the model — a scoped registration would have pinned the FIRST request's tenant into the model and served it to every later request, a total cross-tenant leak no single-request test would reveal; the context is therefore a singleton that resolves per access, and the reasoning is recorded at both the filter and the registration. (2) `Tenant/Branch/Membership/User` MUST be platform-global: sign-in reads the membership to DISCOVER the tenant, so a tenant filter on it would ask the context for the very value that row exists to provide — the chicken-and-egg would have failed at runtime, not compile time.** **PHASE 2 BACKEND IS LANDED AND GREEN.** Packages added with owner approval: **`Microsoft.Extensions.Identity.Core`** (`PasswordHasher<T>` behind `IPasswordHasher`, ADR-0010 §1/§2, DEC-IDN-004) and **`Microsoft.AspNetCore.Authentication.JwtBearer`**. Built: `IPasswordHasher`/`PasswordHasherAdapter` · `SignInCommand`/`SignInResult`/`IAccessTokenIssuer` · **`SignInCommandHandler`** · `JwtOptions` (**12 h, DEC-IDN-009**; **`ClockSkew = TimeSpan.Zero`** so the framework's default five-minute leeway cannot silently extend the ruled lifetime) · `JwtAccessTokenIssuer` · **`POST /api/v1/auth/login`, the ONLY `AllowAnonymous` endpoint** · **`VTF-IDN-001/002` in the error catalog with Arabic + English messages** · **`OrganizationSeeder`** — Happy Pets Clinic · Main Branch · Clinic Owner `01001127204` (password hashed, **never stored plain**), idempotent by identity so an interrupted first boot leaves no half-built clinic · **`FallbackPolicy = RequireAuthenticatedUser`**, so a new endpoint is protected **by omission** rather than exposed by it · `Jwt:SigningKey` **required, validated at start-up, minimum 32 chars, never committed** (dev value in `appsettings.Development.json`, production supplies `Jwt__SigningKey`). **THREE SECURITY DECISIONS WORTH KEEPING: (1) sign-in verifies the password even when NO user was found, against a decoy hash — returning early on «no such user» leaks through timing exactly the fact the unified message exists to hide; (2) unknown number · wrong password · no membership are ONE code, ONE status, ONE message (BR-IDN-003), and TS-IDN-004 compares them literally; (3) the login response deliberately does NOT echo the tenant or branch — they exist only inside the token, so nothing the client holds can ever name its own tenant (§12.5).** **⭐ GATES NOW (all re-run at this checkpoint): build **0 warnings / 0 errors** · `dotnet format` **clean** (it caught the recorded CRLF hazard — files written with LF were normalized) · Domain **163** · Architecture **139** (+5) · INTEGRATION **243/243** GREEN — every one of them running through the REAL sign-in path with a real JWT, real claims, real query filters and real write-stamping.** The fixture authenticates like a user instead of forging a token, so if authentication or tenant resolution breaks, the whole suite fails loudly rather than quietly bypassing what it is meant to prove. **243/243 unchanged is AC-ORG-010 demonstrated: with one tenant and one branch, every existing business behaviour is byte-identical.** **Two write paths needed an explicit scope because they run before anyone signs in — the dev-data seeder and the organization seeder; both use `SystemTenantScope`, which is the only sanctioned use and is confined to Infrastructure and tests.** **REMAINING: Phase 1 — the clinic clock still reads one configured time zone and must become tenant-resolved (DEC-ORG-007/AC-ORG-009). Phase 2 frontend — login screen, auth guard, HTTP interceptor, logout in the shell, the nine approved Arabic strings, `session.expired` handling; NOT STARTED. Phase 3 numbering — NOT STARTED. Phase 4 verification, browser run and the Epic Owner Report — NOT STARTED. Nothing committed.**

**✅ BOTH PILOT BLOCKERS ARE CLEARED (nineteenth cycle). They were: (1) no login screen — now built, and the Pilot starts at one with no temporary bypass; (2) the five global sequences — now dropped, replaced by scope-owned gapless counters, so the first real document is numbered by the ADR-0022 §6 mechanism and not by the one it replaces. The work is committed and pushed (`d187dfa`); what remains before a real Pilot is deployment — the Render service still runs the pre-Epic image.** *(The original wording, for the record: «⛔ THE PILOT MUST NOT START YET… (1) THERE IS NO LOGIN SCREEN… (2) THE FIVE GLOBAL SEQUENCES ARE STILL IN PLACE…».)*

**FOUR IMPLEMENTATION-TIME DECISIONS WERE RECORDED THIS SESSION (they were discovered while building, not while designing, and a future contributor must not undo them): DEC-ORG-009** (the four organization tables are unfiltered, with the chicken-and-egg reason and an architecture test capping «global» at six named tables) · **DEC-ORG-010** (the tenant context is a **singleton** — a `Scoped` registration would pin the first request's tenant inside EF's cached model; **this one carried a real cross-tenant-leak risk**) · **DEC-ORG-011** (shadow properties, not domain properties) · **DEC-IDN-013/014** (decoy-hash timing defence · `ClockSkew = TimeSpan.Zero`, since the default five-minute leeway silently extends the ruled 12 hours).

**OPEN FOR THE OWNER — nineteenth cycle: (1) DONE — the Epic was approved, committed (`d187dfa`) and pushed; item (1) of the eighteenth-cycle list below is closed. The next decision is deployment: redeploy Render on this branch with a real `Jwt__SigningKey` secret, and confirm the Neon role is not a superuser (DEC-ORG-012) before treating row-level security as active in production.** **(1b) Two Pilot-scoped concessions come due before a second clinic: the seeded password equals the phone number (DEC-IDN-008) and there is no lockout (DEC-IDN-010) — ADR-0022 §12.15 requires revisiting both.** **(1c) A new operational constraint, DEC-ORG-012: the deployed database role must NOT be a superuser, or row-level security is installed and never enforced. Neon's default role qualifies; anything else must be checked before it is used. Recorded consequence: the local `docker-compose.yml` runs as `POSTGRES_USER`, a superuser, so RLS is inert in local development — the query filter alone protects there.** **(1d) The local dev database on `:5434` still holds pre-tenancy rows and the migration will refuse them; recreating it (drop the volume, let the seeders re-run) is expected, and it was left untouched this session because deleting the owner's data was not mine to decide.** **(2) Decide whether `ADR-0007` and `ADR-0010` should flip from `Proposed` to `Accepted`** — raised last turn and not ruled; Phases 1–2 build directly on ADR-0010's abstraction mandate, and this is the same condition that has twice kept the branch off `main`. **(3) `Jwt__SigningKey` must be generated and set as a secret env var before any cloud deploy** — the dev key in `appsettings.Development.json` is deliberately not production-usable, and the app refuses to boot without one. **(4) The Neon password rotation from the sixteenth cycle is still outstanding.** **(5) `backup-restore-runbook.md` now has a second reason to be rewritten** — ADR-0022 §Consequences records that single-tenant restore from a shared database is the one operation this decision makes harder. **ALL sixteenth- and seventeenth-cycle items below remain open and unchanged**, including the tablet breakpoint, the seventeen Arabic wordings, PIT-001..004 closure, AMD-1..6, DEC-INV-022, the STD-UX-065 reading, the ADR-0017 §11 vs Pilot Observation Mode contradiction, and GLOSSARY sync debt. **SERVERS (nineteenth cycle): browser verification deliberately used a THROWAWAY database — container `vetflow-verify` on `:55434` with an unprivileged `vetflow_app` role — plus an API on `:5085` and `ng serve` on `:4500`. The owner's dev database on `:5434` was NOT touched: it holds rows from earlier sessions that the tenancy migration would reject, and deleting it was not mine to decide. Recreating it (drop the volume, let the seeders re-run) is the normal way forward. Containers otherwise as before — pilot stack `:5435`/`:8080`, running the pre-Epic image. ⚠️ The stale `ng serve` on `:4200` (pid 27368, from 2026-08-01) may still be running and serves a build older than everything above.**

**✅ RULED 2026-08-02 (eighteenth cycle) — see the banner above. The recommendation recorded below was INVERTED by the commissioned architecture review: the owner ruled a SHARED DATABASE WITH A TENANT DISCRIMINATOR, not database-per-clinic, and ADR-0022 records why. The analysis below is preserved as the record of what was known when it was raised; its «Recommendation: database-per-clinic» is SUPERSEDED, and the reasoning that produced it is retired on the record in ADR-0022 §Context. Its deadline analysis remains correct and still binds.** *(Original entry, retained:)* **⛔ RAISED AND NOT RULED — MULTI-TENANCY. THE HIGHEST-PRIORITY OPEN ITEM, AND IT HAS A DEADLINE.** The owner asked whether pilot data will be linked to a tenant so a second clinic can later register and work separately. **Verified answer: NO — there is no `TenantId`/`ClinicId`/`OrganizationId` anywhere** (grep across every entity, EF configuration and migration returned zero), **and no tenancy ruling exists in any document** (`docs/business/`, `PROJECT_CONTEXT.md`, `ROADMAP.md`, `mvp.md` are all silent; «multi-branch» appears once as an out-of-scope future module). **Pilot data will therefore be recorded with no owner attribution.** **Whether that matters turns on one undecided choice: database-per-clinic vs shared database with a tenant column.** **Database-per-clinic → the retrofit is ZERO** (this database simply becomes clinic A's; attribution is implicit). **Shared database → attribution is one `UPDATE`, but three real costs, all verified in the code: (1) document numbers come from DB-level sequences, so clinic B's first invoice would be `PUR-000002`** — commercially unacceptable, and `PUR-`/`SAL-`/`PRT-`/`SRT-`/`PRD-` all share the mechanism; **(2) category name, manufacturer name and product `InternalCode` are GLOBALLY UNIQUE**, so clinic B could not reuse a name clinic A used — each becomes a composite index; **(3) every read and write needs a tenant filter, forever.** **ADR-0019's own platform study already scored per-tenant databases as free on PostgreSQL** («Multi-tenancy readiness — schemas, RLS, per-tenant DBs at no cost»), so the repository has already reasoned this way. **THE DEADLINE: the pilot database is verified EMPTY (16/16 tables at 0, all five sequences never called) and ADR-0020 defines the Pilot's start as the first real operational entry — which has NOT happened. So the destructive-migration bar has not engaged and this decision is FREE RIGHT NOW.** After the first real invoice, changing unique indexes or the numbering mechanism needs an owner-approved migration plan on live clinic data. **Verdict given to the owner: safe to start the pilot, PROVIDED the tenancy model is ruled BEFORE the first real entry.** Recommendation: **database-per-clinic**. **Consequence for ADR-0021: Phase 1 is currently ONE shared Render service with ONE Neon database — the deployment topology must match whichever model is chosen, so ADR-0021 needs amending too.** **Nothing was written or implemented — an ADR was offered and not drafted, because this is a review checkpoint and the owner's ruling comes first.**

**OPEN FOR THE OWNER: (A) DONE — the Epic and the branding were approved, committed (`6c1ed0e`) and pushed; (B) RESOLVED — the Inventory card is built (REQ-INV-012 / DEC-INV-040), nothing to rule; (C) rule the tablet breakpoint** — 768 today, so a modern tablet in portrait (~810–834 px) still gets the permanent sidebar, not the drawer; **(D) review SEVENTEEN new Arabic wordings** — the five navigation/row keys (`nav.primary`, `nav.menu.open/close`, `products.column.actions`, `products.row.open`) plus twelve `productDetails.inventory.*` for the new card; **(E) approve closure of PIT-001..004** (implemented, not closed). Report: `docs/ui/pilot-ux-polish-epic-report.md`. **All sixteenth-cycle items below remain open and unchanged.** **SERVERS AT CLOSE: the API `:5080` and `ng serve` `:4500` that this session started were STOPPED. ⚠️ THE STALE `ng serve` ON `:4200` (pid 27368, started 2026-08-01 03:48 from an earlier session) IS STILL RUNNING — it serves a build with NONE of this cycle's work, so `localhost:4200` looks like the Epic and the branding never happened.** The owner asked for it to be killed and then **interrupted the command; it was left alone deliberately and never re-attempted.** **Kill it before any manual testing, or start a fresh `ng serve` on another port.** Containers left running as before: dev db `:5434`, pilot stack `:5435`/`:8080`.

**Earlier (sixteenth cycle):** 2026-08-02 · session closed (`/close-session`) — **THE UNCOMMITTED BACKLOG IS COMMITTED AND PUSHED (FIRST TIME IN FOUR CYCLES), AND A CLOUD DEPLOYMENT IS IN FLIGHT AND NOT YET GREEN.** **Work permitted this session by explicit owner instruction, overriding Pilot Observation Mode each time:** produce two operating documents · «reconcile every documentation vs implementation mismatch» · «prepare the branch to deployment» · «push the changes». **Nothing was implemented that the owner did not name.**

**Delivered (1) — two Arabic documents, both also as PDF, both Draft awaiting owner review:** **`docs/operations/user-guide/`** (15 files, 73-page PDF) — one chapter per screen in the owner's twelve ruled sections, threaded by a single scenario (amoxicillin, two batches, receive → sell → sales return → write off → adjust → purchase return) with the stock ledger after every operation; numbers taken from **verified behaviour, not from the documentation** (FEFO splits one 60-tablet sale across two batches; a partial sales return resolves to the batches the goods actually left). **`docs/operations/uat/manual-test-script/`** (14 files, 50-page PDF) — **rewritten once on owner feedback** from six thematic scenarios into **one chapter per screen with numbered ☐ steps and a field-by-field explanation table (what it is · what to enter · its effect)**; relative dates (`[ت]`, `[ت−١]`, `[ت+١٥]`, `[ت+٤٠٠]`) keep it runnable on any day, and a per-chapter expected-balance table lets a tester catch divergence immediately. Both record the three behaviours a tester must **not** file as defects. **Two rendering defects were found by inspecting the PDF output and fixed at source: U+00B7 beside Arabic-Indic digits reads as ٠ at print size**, which garbled chapter-reference lists and **all 166 section headings**.

**Delivered (2) — documentation ↔ implementation reconciliation (owner-commissioned, seven ruled sections per issue). Six mismatches investigated; source of truth determined for each; TWO were implementation defects and are FIXED:** **(a) `editor.units.conversionHint` named the wrong direction** — it read «الكمية داخل الوحدة الأكبر التالية» while `QuantityInNextUnit` is how many of the **next (smaller)** row a unit contains (`ProductUnit`: "carton = 12 boxes"; rows submit largest-first via `position: index`). **BR-CAT-018 states the rule in exactly the opposite direction to the old hint** and BR-CAT-019 draws كرتونة ← علبة ← شريط ← قرص; the old wording appeared in **no approved document**. Source of truth = approved business rules; the copy was wrong. **(b) both return screens committed irreversibly with no confirmation** — `purchaseReturn.confirm`/`salesReturn.confirm` existed in `ar.ts` but were **rendered nowhere**, contradicting `purchasing/ui.md` and `sales/ui.md`, which both specify a confirmation for an irreversible stock movement; wired through `VfDialog` **reusing the existing approved strings (no new copy)**, running **after** the shared submit guidance so an invalid form still reports its fields. **Eight new tests pin the gate (four per screen), mutation-checked** — reverting `requestConfirm()` to `save()` fails all four purchase-return tests. **FOUR NOT ACTED ON, each with a stated reason:** the missing «حفظ كمسودة» affordance (**needs a route to reopen a draft return — none exists → new screen → owner approval**); movement-history references for returns (**BR-INV-043 is genuinely ambiguous — «حين تُنفَّذ قدرتاهما (C5/C6)» vs «لا يُعرَض رابط لمستند غير موجود»**; rendering it needs a new join under BR-INV-045 **and** a new `MovementReferenceTargetDto` state — an approved read-model contract change); the unit-cost snapshot unit basis (**not a mismatch at all — `write-kernel.md` §57, BR-PUR and `inventory/acceptance.md` all say «سعر وحدة بند الشراء» displayed «كما هي», and `test-scenarios.md` pins it with `UnitCostSnapshot = 100` (وحدة «علبة») beside a stock-unit quantity — settled approved behaviour, a **Usability** observation only**); and **AC-CAT-049 appears UNIMPLEMENTED — new finding**: nothing enforces «وحدة المخزون يجب أن تكون الأصغر» (`ProductWriteCommandValidator` only checks non-empty; the domain only enforces `VTF-CAT-020` *in-profile*), which is why `ProductUnitConversion` treats `isExact = false` as reachable. **Not built — adding it would reject existing product configurations, and `_INDEX.md` already records that seed data may violate the amended BR-CAT-020 with no migration path.**

**Delivered (3) — ADR-0021 «Deployment platform — managed cloud (Neon + Render)», status Proposed, indexed.** Records the owner's decision to move the real pilot to cloud **and to ship before authentication exists**. The ADR **does not relitigate it**; it phases the work so the accepted risk stays bounded: **Phase 1** stack live, **empty database, no clinic data** · **Phase 2** authentication (new capability — needs its own module docs) · **Phase 3** live data migrated under an ADR-0020 plan. **Phase 3 does not start before Phase 2.** Consequences recorded plainly, including two the owner should not discover later: **offline operation at the counter is lost** (today's on-premise stack survives an internet drop; a cloud one does not) and **clinic data leaves the premises**. `render.yaml` (Render Blueprint) pins three things that otherwise break or silently mis-run: `PORT`/`ASPNETCORE_HTTP_PORTS` **8080** (the image listens on 8080, Render routes `$PORT`, default 10000) · `numInstances: 1` (migrations apply at startup; two instances race the schema) · **Neon's DIRECT endpoint, not `-pooler`** (startup DDL through PgBouncer transaction mode). The deployment runbook gained a cloud section **and a banner making clear the on-premise deployment remains the live pilot until Phase 3**, plus a flag that `backup-restore-runbook.md` is now wrong for the cloud path and must be rewritten before any data moves.

**COMMITTED AND PUSHED — branch `pilot/docs-fixes-and-cloud-deployment`, four commits, local = remote.** **This is the first push of the Validation UX Adoption Epic**, which had been complete-but-uncommitted since the thirteenth cycle. **The owner's instruction was «push the changes»; that was read as the Epic Commit Approval that had been outstanding — if a narrower scope was meant, say so and it can be split.** The Epic (~60 files I did not author and have not reviewed line by line) rides in the **same commit** as the two reconciliation fixes because they interleave in `ar.ts` and both return components; separating them meant hunk surgery on Arabic source, which this repo's recorded hazard makes unsafe. **The commit message says exactly that.** **Branched rather than pushed to `main` deliberately: ADR-0021 is `Proposed`, not accepted, and four reconciliation issues are unruled — `main` would otherwise carry an unratified architecture decision. Fast-forward available on request.** **Full commit gate re-run green before committing:** build **0/0** · `dotnet format` **clean** · Domain **163** · Architecture **134** · Integration **236** · frontend **299/53 files** · ESLint **clean** · Stylelint **clean**.

**IN FLIGHT AND NOT GREEN — the Render deployment.** **What is proven:** the Docker image **builds on Render** (fully cached now), the app **starts**, serves **HTTPS**, routes `/api/v1/*`, and returns correct RFC 9457 ProblemDetails; `Database__ApplyMigrationsAtStartup=true` **takes effect** (the trace reaches `NpgsqlMigrator.MigrateAsync`). **What is not:** the app **crashes at startup (status 139)** because the Render env var `Database__ConnectionString` still contains **`Trust Server Certificate=true`**, **which Npgsql 10 removed** — `System.ArgumentException: Couldn't set trust server certificate`. **This was my error**: I supplied that template and wrote it into `render.yaml` and the runbook; **both are corrected and pushed** (commit 4 of 4) with the reasoning recorded — since Npgsql 8 the libpq semantics apply, so `SSL Mode=Require` already means *encrypt without verifying*, and `channel_binding` (which Neon puts in its URI) is likewise not an Npgsql keyword. **Verified by grep that the keyword exists nowhere in the codebase — it can only be the env var.** **The owner has been given the corrected, space-free string and has not yet applied it.** **Neon: project up in `eu-central-1`, database `neondb`, direct endpoint, TABLES STILL EMPTY — migrations have never run.** **The container image build was NEVER verified locally this session** — two attempts died on NuGet download timeouts inside the build container (different packages each run: environmental, not a Dockerfile defect); the Release publish step and the Angular bundle path were verified directly instead, and Render then built it successfully.

**OPEN FOR THE OWNER — deployment: (1) apply the corrected connection string** (`…;Password=…;SslMode=Require`, nothing after it) and redeploy; **(2) ROTATE THE NEON PASSWORD — `neondb_owner`'s live password was pasted into the session transcript** (database is empty and Phase 1, so not urgent, but treat it as burned); **(3) accept or amend ADR-0021** — it is the record that this ships without auth; **(4) merge the branch to `main`?** (fast-forward); **(5) `plan: starter` is paid — free tier is adequate for Phase 1 if preferred.** **OPEN — reconciliation: (6) the «حفظ كمسودة» affordance; (7) BR-INV-043's return-reference clause — does the C5/C6 condition now bind, and is a non-navigable label acceptable with no return-details screen?; (8) AC-CAT-049 unimplemented — enforce it and reject existing configurations, or record it as accepted?** **CARRIED FORWARD, unchanged: (9) AMD-1..6; (10) DEC-INV-022; (11) the STD-UX-065 reading; (12) the ADR-0017 §11 vs Pilot Observation Mode contradiction; (13) GLOSSARY sync debt; (14) the first backup of the clean pilot baseline; (15) whether the dev database should also be reset.** **Standing hazard unchanged: `localhost:4200` runs against the DIRTY DEV database; the clean pilot stack is `127.0.0.1:8080`.**

**Earlier (fifteenth cycle):** 2026-08-01 · session closed (`/close-session`) — **THE PILOT PHASE HAS BEGUN: THE PROJECT IS NO LONGER IN IMPLEMENTATION MODE, AND THE PILOT DATABASE IS CLEAN AND VERIFIED.** Two owner rulings this session, both recorded: **(1) Pilot Observation Mode** — the Pilot **suspends Continuous Capability Mode**; the AI contributor is a Pilot Observation & QA assistant doing exactly four things (reproduce · classify · explain expected behavior · prepare fixes only after approval), with implementation, refactoring, architecture work, performance work, unrequested documentation and enhancement suggestions **all prohibited until the owner says «Fix this issue»** → recorded in **`.claude/rules/workflow.md` §Pilot Observation Mode**; **(2) the finding report structure** (ten fields: ID · Category · Severity · Steps · Expected · Actual · Root cause *if known* · Suggested fix *one paragraph* · Affected modules · Regression risk, with **related findings grouped, never duplicated**) → recorded in **`docs/operations/pilot-findings.md`**. The three-category taxonomy (Bug · Usability · Enhancement) was **restated, not changed** — already ruled 2026-07-31 with the GO decision, so it got no new artifact (the restatement precedent). **Done this session:** the dev stack was started for owner testing (db 5434 · API 5080 · web 4200) · two expected-behavior questions answered from the repository, **both confirming approved design, neither a finding**: **no user accounts exist by design** (BR-INV-066 — «لا وحدة مستخدمين، ولا مصادقة… في الـ MVP»; the actor is the optional free-text `ActorName`, DEC-INV-030; ADR-0010 only rules *how* auth would be built if ever) and **no dashboard exists by design** (`/` redirects to `catalog/products`; «لوحات المعلومات (Dashboards)» is in the inventory scope-lock exclusion list and Reports is Post-Pilot scope) · **THE FINAL CLEAN RESET OF THE PILOT DATABASE WAS EXECUTED AND VERIFIED** — the Pilot Transition Checklist item (ADR-0020). **Method: the ruled procedure, not hand-written deletes** (`clean-database-verification.md`: `down -v` + `up -d`, migrations rebuild) so the result is a *newly created* database, not a cleaned one. **Before destroying anything the target was inspected and proven to hold no clinic data** (all documents first-of-series, all created inside a 102-second window on 2026-07-31 = the WS4 smoke run; the 28-char/53-byte category = the documented encoding round-trip record). **All seven verifications pass:** 16/16 business tables at 0 by exact count · all 5 document sequences **never-called** with `start_value = 1` (first documents will be `PUR-000001`/`SAL-000001`/`PRT-000001`/`SRT-000001`/`PRD-000001`) · inventory and movement totals 0 · movement history empty · **dynamic orphan scan across all 12 FKs = 0 orphans** · 12 FKs + 18 PKs with 0 unvalidated, 65 indexes with 0 invalid, 12/12 migrations applied, `has-pending-model-changes` **none**, STD-BE-051 destructive scan **PASS** · reference data intact from migrations alone (**5 natures · 13 units**, every row proven multi-byte UTF-8 — `octet_length > char_length` rules out the `????` fault) · pilot API independently returns `totalCount = 0` on every business endpoint. **Report: `docs/operations/pilot-database-verification-report.md` (Submitted, untracked).** **One deviation, stated in the report §3:** `up -d` instead of the runbook's `up -d --build` — source and database both carry 12 migrations so the DB outcome is identical, while `--build` would bake the uncommitted frontend into the pilot image; reversible with one command. **The dev database was deliberately NOT reset and was verified untouched afterwards** (3 products · 49 sales · 256 movements) — isolation was proven *before* the destructive step (`name: vetflow-pilot`, `config --volumes` resolving to one volume). **CONSEQUENCE THE NEXT SESSION MUST NOT MISS: `localhost:4200` runs against the DIRTY DEV DATABASE, not the clean pilot one — the clean pilot stack is `127.0.0.1:8080` (db loopback 5435).** **OPEN FOR THE OWNER: (1) Epic Commit Approval** — the Validation UX Adoption Epic is still complete, gated green and **entirely uncommitted** (see the fourteenth cycle below) **+ review of its report and flagged Arabic wordings; (2) the first backup of the clean pilot baseline** — the last Pilot Transition Checklist item, `scripts/backup-vetflow.ps1` validated in WS2, **not run** (it creates files, left as the owner's call); **(3) whether the pilot image should be rebuilt** from the current tree; **(4) whether the dev database should also be reset** (it is where `4200` testing happens); **(5) a governance contradiction, recorded not resolved — `ADR-0017 §11/§11a` still enumerates Continuous Capability Mode («continue automatically between capabilities»), which Pilot Observation Mode now suspends; `workflow.md` carries the new ruling, but an ADR's meaning is never amended silently (ai-governance «Never» list) and a new/amended ADR is an owner review checkpoint — does §11 want a cross-reference to the Pilot suspension?**; **(6) AMD-1..6; (7) DEC-INV-022; (8) the STD-UX-065 reading.** Both stacks left running. **Nothing committed.**

**Earlier the same day (fourteenth cycle):** **THE VALIDATION UX ADOPTION EPIC IS COMPLETE (C0–C12), GATED GREEN, BROWSER-VERIFIED, AND STOPPED AT ITS SEVEN COMPLETION CONDITIONS — AWAITING EPIC COMMIT APPROVAL. NOTHING COMMITTED, NOTHING PUSHED.** **Done this session (resumed exactly at the thirteenth-cycle plan):** **C9** — the product editor fully adopted (per-rule messages incl. mirrored server length ceilings · `vf-validation-summary` with the units section registered as a linked entry (STD-UX-023/129) · server field-error projection · shared submit guidance with first-invalid focus · full VTF classification with the generic-save-template override (`editor.error.save`) · unit-row validation through `vf-form-field` (required unit, positive conversion factor with its hint, barcode length) · the three cross-row unit rules split into one sentence each rendered at the rows (STD-UX-017; the old triple sentence stays as the rows' empty-state hint) · the **`vf-checkbox` repair under the freeze's accessibility exception** (CVA + explicit `id`/`for` + `aria-invalid`/`aria-describedby` channel; legacy `[checked]`/`(toggled)` contract preserved) · all four lookups surfaced with retry (STD-UX-041) · the **duplicate-check advisory through the shared `debouncedCheck`** (debounced/cancelling/cached, its **failure surfaced** as a focused warning that pauses once and never blocks — BR-CAT-042) · open-expiration rule moved to field-scoped conditional validators (BR-CAT-036); dead keys `editor.required`/`editor.error` removed. **C10** — announcer sweep: the polite live region now on **19/19 routed screens** (12 added: both details pages + product details · adjustment · write-off · both create pages · both return pages · batch viewer · expiry · history), announcing load outcomes, saved facts (status insertions are not reliably announced alone), and in-flight saves; rejections stay with their `role="alert"` surfaces (no double announcement). **C11** — docs sync per STD-UX-114: the six `writeOff.error.*` keys + `pickers.products/batchesError` landed in `inventory/ui.md`, `pickers.unitsError` + `purchaseReturn.error.draftState` in `purchasing/ui.md`, `salesReturn.error.draftState` in `sales/ui.md`, all C9 strings in `catalog/ui.md`; **`vf-banner` registered and the `vf-checkbox` repair recorded in `docs/ui/components.md` (STD-UX-127)**; GLOSSARY debt: **no new domain terms minted** (verified) — standing debt unchanged. **C12** — full gates + **live-browser verification via headless Chrome/CDP against the real stack: 16 screens × 1440×900 and 390×844, 201 recorded checks, all passing at close** (rtl/lang · zero overflow · zero console errors · region presence · moment-3 flows · dialog behaviors · per-line receive expiry focus · summary navigation · success-after-correction), which **found one real defect, fixed and re-verified: both return pages rendered the strictly-positive sentence for the non-negative rule (zero = "not returning this line") → new shared `validation.nonNegative` («القيمة يجب أن تكون صفرًا أو أكثر.»)**; seven first-pass failures were checker artifacts (asserted the canonical sentence where ruled contextual wordings correctly render). **The consolidated Owner Report with all nine ruled sections is Submitted: `docs/ui/validation-adoption-epic-report.md`** (coverage · before/after by module — 23/23 audited surfaces now comply, was 22 % · exceptions · debt · reuse · compliance % · a11y summary · browser results · final UX audit). **Gates at close: frontend 291/291 (was 283) · ESLint clean · Stylelint clean · `ng build` exit 0 (TD-107: 583.98 kB, +4.16 kB over the Epic) · backend untouched and green — build 0/0 · `dotnet format` clean · Domain 163 · Architecture 134 · Integration 236.** **Foundation freeze honored: only `vf-checkbox` touched, under the ruled accessibility exception.** **New Arabic wordings pending owner review (all flagged in `ar.ts` and landed in their `ui.md` homes):** `editor.error.save` · `editor.duplicateCheck.failed` · `editor.units.error.empty/noPurchase/noSale` · `pickers.categoriesError/manufacturersError/naturesError/unitsListError` · `validation.nonNegative` — joining the still-flagged writeOff/pickers/draftState groups and the Phase-1 `validation.*` set. **Dev-data side effect (pre-pilot):** draft purchase **PUR-000017 «مورد تحقق الإيبك»** (+1 line, created via API for the receive-dialog expiry check); no stock moved. **OPEN FOR THE OWNER: (1) Epic Commit Approval** (the whole uncommitted set: Phase 1 + Adoption Test + Epic — 67 modified + new files, +4 282/−1 811) **+ review of the Epic report and the flagged wordings; (2) the STD-UX-065 reading for plain sections (report §3.3); (3) AMD-1..6; (4) DEC-INV-022; (5) calendar vs UAT/Pilot.** Servers stopped; only the db container (:5434) runs.

**Earlier (thirteenth cycle):** **THE VALIDATION UX ADOPTION EPIC WAS COMMISSIONED AND RUNNING UNDER CONTINUOUS CAPABILITY MODE: 9 OF 13 PLANNED CAPABILITIES (C0–C8) IMPLEMENTED AND GATED GREEN.** The owner **approved the Adoption Test report**, **froze Validation Foundation v1** (no further Foundation change except **verified defects · accessibility fixes · security issues**), and commissioned **one Epic** covering **every remaining production form, dialog and workflow** per the approved standard — no stop between modules, no intermediate reviews unless a governance stop condition fires, **one consolidated Owner Report at completion** (ruling recorded in `validation-and-guidance.md` §0, **third addendum**; supersedes the Phase 2 → review → Phase 3 split). **Done this session (each verified by the full frontend suite before moving on):** **C0** — `vf-banner` built (§13 item 2; mandated by STD-UX-062/121 — read as *completing the approved architecture*, not modifying frozen pieces; flagged for the owner) and the three migrated screens retrofitted onto it · **C1** sale-create migrated (mirror of the Adoption-Test screen) · **C2** manufacturer dialog mirrored onto the POC pattern + the **silent activate/deactivate failures surfaced** on both list pages (STD-UX-004) · **C3** write-off rewritten as the adjustment mirror (reactive form + mapper store), **picker load failures surfaced with retry on both stock screens** (STD-UX-041), and the AP-16 **key-ownership cleanup** (write-off owns its six error keys) · **C4** purchase add-line dialog on the three moments + classification + units-load retry, **line removal un-silenced**, lines-store callbacks now carry `ClassifiedFailure` · **C5** receive dialog — the audit's largest UX risk — full per-code classification (STD-UX-037), per-line expiry through a `FormRecord` with **first-offending-line focus** (STD-UX-084), retry relabel (STD-UX-033) · **C6** sale add-line dialog (incl. profile-armed `wholeNumber` for non-splittable), commit dialog moved onto the mapper with banner focus (its ruled metadata-conditional wordings preserved), sale line removal un-silenced — **the last of the five copy-pasted `classify()` maps is gone (gap F-4 closed; STD-UX-123 holds on every mutation surface)** · **C7/C8** both return pages rebuilt as typed reactive forms: required date + per-line non-negative validation through `vf-form-field` (visually-hidden per-cell labels, STD-UX-093), **submit no longer disabled for invalidity** (STD-UX-016), classified failures with retry relabel, and the **STD-UX-042 sequence statement** — a failure after the draft was created now appends «أُنشئت مسودة المرتجع ولم تُثبَّت، ولم يتحرّك أيّ مخزون.». **Gates at close: ESLint clean · Stylelint clean · `ng build` exit 0 · frontend 283/283** (was 266 pre-Epic). **Browser verification of the Epic screens has NOT run yet — it is planned as C12's full sweep, per the Epic plan.** **New Arabic strings pending owner review + ui.md sync (C11, STD-UX-114):** the six `writeOff.error.*` keys (only «اختر سبب الإهلاك.» is a new wording — STD-UX-054 template substitution; the rest copy the adjustment sentences verbatim) · `pickers.productsError/batchesError/unitsError` · `purchaseReturn.error.draftState` · `salesReturn.error.draftState`; removed dead keys `purchaseCreate.required` / `saleCreate.required`. **Recorded debt:** a screen-local `nonNegative` validator is duplicated ×3 (add-purchase-line + both return pages) — belongs in `vfValidators` at the next Foundation window; the raw return-page `<table>` deviation stays under TD-007. **REMAINING IN THE EPIC — resume here: C9 product editor** (the long form: per-rule messages, `vf-validation-summary`, server projection, unit-row validation, `vf-checkbox` a11y repair under the freeze's accessibility exception, debounced duplicate check via the shared async-check utility) · **C10** announcer coverage sweep (STD-UX-092) · **C11** docs sync (module `ui.md` copy tables for the new strings, `components.md` registration of `vf-banner` — STD-UX-127, still unregistered) · **C12** final gates + live-browser verification of every touched screen at 1440/390 + the consolidated Owner Report (coverage · before/after compliance by module · exceptions · debt · reuse · compliance % · a11y summary · browser results · final UX audit). Nothing committed; only the db container (:5434) runs. AMD-1..6 · DEC-INV-022 · calendar-vs-UAT remain open as before.

**Earlier (twelfth cycle):** **PHASE 1 IS APPROVED AND THE OWNER-RULED ADOPTION TEST IS EXECUTED, GATED GREEN, BROWSER-VERIFIED, AND REPORTED — the owner has since approved the report (see the thirteenth cycle).** The owner approved Phase 1 with one additional gate before Phase 2: a single **Adoption Test** on one medium-complexity un-migrated production screen, Foundation **frozen** (no modification unless a real defect · approved infrastructure only · no new validation components); on success the Foundation freezes as **v1** and Phase 2 proceeds on it (ruling recorded in `validation-and-guidance.md` §0, **second addendum**). **Screen chosen: purchase create** (`/purchases/new`) — audit verdict Partial, a real medium form that is *not* a mirror of either POC (write-off and the manufacturer dialog would bias the estimate) and that exercises the `vf-date-input` CVA repair in a real blur-validated form plus a **live** `VTF-VAL-001` projection. **Executed:** the screen now runs entirely on the frozen Foundation — `vf-form-field` defaults (ui.md rules field-by-field errors with no contextual copy, so no overrides), shared submit guidance (moment 3 + first-invalid focus), date blur validation (the audit's named gap, closed), success-after-correction, `ApiErrorMapper` classification with the ruled contextual `system` override (`purchaseCreate.error`), server field errors projected inline and clearing on edit, a focusable banner clearing on the relevant edit; dead key `purchaseCreate.required` removed (byte-identical to `validation.required`). **Results (full detail in `docs/ui/validation-adoption-report.md`, Submitted):** production diff **−67/+141** (the screen gained six absent behaviors) · screen-local validation code **≈ 55 lines** vs **≈ 640 Foundation lines** exercised unmodified → **≈ 92 % reuse** · **9 pieces reused · 0 new components · 0 Foundation files touched · 0 new Arabic strings** · bundle −0.11 kB (579.71 kB, TD-107 unchanged) · **Foundation defect: none · insufficient API: none** · remaining-screens projection from the real data point: **Phase 2 ≈ 5–6.5 d, Phase 3 ≈ 1.5–2 d** (was ≈ 7–11 d). **Gates:** ESLint/Stylelint clean · frontend **268/268** (+2) · `ng build` exit 0 · **live-browser 16/16** at 1440 and 390 against the real stack (rtl/lang, zero overflow, all three moments, focus, aria chain, live VTF-VAL-001 inline with no banner, end-to-end save to the created invoice's details, zero console errors). Dev-data side effect: one draft purchase invoice («مورد تحقق الاعتماد»). Servers stopped; only the db container (:5434) runs. **Open for the owner: (1) the Adoption-Test report verdict — on success the Foundation is v1-frozen and Phase 2 may start; (2) commit approval** (Phase 1 + Adoption Test are one uncommitted change set); **(3) AMD-1..6 unruled; (4) DEC-INV-022 open; (5) calendar timing vs UAT/Pilot unchanged.** The eleventh-cycle flagged Arabic strings (`validation.maxLength` · summary · corrected · notFound) remain listed for review; the Adoption Test added none.

**Earlier (eleventh cycle, `/close-session`):** **VALIDATION FOUNDATION PHASE 1 IS IMPLEMENTED, GATED GREEN, AND BROWSER-VERIFIED — NOT COMMITTED, AWAITING PHASE-1 REVIEW (the owner's ruled order: Phase 1 → Review → Phase 2 «high-frequency workflows» → Review → Phase 3 «complete adoption», recorded in `validation-and-guidance.md` §0 addendum).** Open for the owner: **(1) Phase-1 review** of the foundation + POC (then commit approval — nothing is committed); **(2) the new Arabic strings** flagged in `ar.ts` (validation.maxLength · summary title · corrected · notFound micro-copy); **(3) AMD-1..6 remain unruled**; **(4) DEC-INV-022** still open; **(5) calendar timing vs UAT/Pilot** unchanged. The owner approved the standard's implementation with Phase 1 strictly scoped to the reusable foundation, one–two POC screens, no module-wide adoption, AMD-1..6 deferred. **Delivered exactly the nine ruled pieces:** `vf-form-field` (hint → error → success slot, three-moment timing, full aria wiring) · `vf-validation-message` · `vf-validation-summary` (linked navigation) · `ValidationFocusService` · `ValidationRegistry` (**all 33 backend codes**, completeness pinned by test) · `ApiErrorMapper` (classified failures + ruled contextual overrides) · `SubmitGuidanceDirective` (moment 3: mark-touched → validSubmit or first-invalid focus; dialogs via `trigger()`) · shared validation styling (existing tokens; single-source component styles) · shared utilities (`vfValidators`, `resolveValidationMessage`, `projectServerFieldErrors`, `debouncedCheck` 300 ms/cancel/cache). **UI Kit repairs:** `vf-select` and `vf-date-input` are now ControlValueAccessors (select marks touched on blur/panel-close and stamps `aria-invalid`/`aria-describedby` on its combobox), text/number/textarea cooperate with the wrapper, `vf-button` gained `type="submit"`. **POC (proof, not adoption): the inventory adjustment page** (typed reactive form, ruled per-field wordings as registry overrides, focusable rejection banner that clears on the relevant edit, retry-labelled action on concurrency) **and the category dialog** (three moments, distinct maxLength sentence, server duplicate projected inline per STD-UX-019/020). **Gates:** build exit 0 (only TD-107: initial bundle **579.82 kB**, +12.8 kB for the foundation — accepted-debt horizon unchanged) · ESLint clean · Stylelint clean · frontend tests **266/266** (+31, incl. registry-completeness and a regression spec) · **live-browser verification 30/30 checks** via headless Chrome/CDP against the real stack at 1440 and 390 (rtl/lang, zero overflow, all moments, focus, aria-describedby chain, live 409 `VTF-INV-061` rejection wording + banner focus + clear-on-edit, duplicate-name projection, zero console errors). **Verification found one real foundation defect, fixed + regression-pinned:** `errorText` was a computed depending only on the boolean `invalid()`, so the message froze on the first violated rule when the rule changed while the field stayed invalid (required → maxlength) — invisible on single-wording fields, caught only in the browser. Also fixed: a non-reactive `NgControl.control` init window for dialog-hosted content (bounded microtask retry). **Two new user-facing strings need owner eyes at review:** `validation.maxLength` («يجب ألّا يتجاوز هذا الحقل {max} حرفًا.») and the summary/`corrected`/`notFound` micro-copy — all style-guide-conformant, listed in `ar.ts` under the foundation section; the 33 `errors.<code>` defaults mirror the backend resx verbatim (no invention). **Docs sync:** the new pieces registered in `docs/ui/components.md` (STD-UX-127). **Deferred within Phase 1 by the owner's list:** `vf-banner` (screens keep local banner markup; the dialog's local `.dialog-error` is flagged) and the `vf-checkbox` error channel (first needed in Phase 2's product editor). **Known repo observation:** `prettier --check` fails on previously committed files too — it has never been a passing gate (frontend format gate is ESLint); new foundation files were written config-conformant. **Dev-data side effects of verification (pre-pilot):** a few «تصنيف تحقق NNNNNN» categories and one +1 adjustment movement on batch `ace4987c` (the −999999 rejection moved nothing, as asserted). Servers stopped; only the db container (:5434) runs. **NOTHING COMMITTED — next: owner Phase-1 review, then Phase 2 (high-frequency workflows: Product Editor · Purchase Invoice · Receive · Sale · Purchase Return · Sales Return).**

**Earlier (tenth cycle):** **THE VALIDATION & USER GUIDANCE UX STANDARD IS APPROVED WITH MODIFICATIONS AND BOTH DOCUMENTS ARE SYNCHRONIZED.** The owner approved the initiative's two documents and issued **twelve rulings**, all incorporated: Progressive User Guidance (hint → error → success) · the **three validation moments** (typing / blur / submit-for-business-rules) · field errors never banner-only · a **Validation Summary** component for long forms with clickable navigation · a **writing style guide** for all validation copy · server field errors mapped back to fields · errors disappear the instant a field is valid · **no toasts for validation** · auto-open + focus for invalid fields in tabs/accordions/dialogs · accessibility mandatory (aria-invalid, aria-describedby, alert semantics) · a new **Validation Performance** section (frequency, debounce ~300 ms, no per-keystroke API calls) · implementation order ruled **Foundation → Shared infrastructure → Module adoption**. **`docs/ui/validation-and-guidance.md` is now Approved** (rulings recorded + routed in its §0; `STD-UX-NNN` IDs renumbered once in this revision — before anything cited them — and frozen from now on), and **`docs/ui/validation-gap-analysis.md` is revised to match** (verdicts unchanged: **5 comply · 12 partial · 6 violate**; new cross-cutting gap **F-10** — the ruled Progressive-Guidance surface exists nowhere; effort re-phased per ruling 12, total **≈ 14–21 dev-days**). **Nothing is implemented yet — the owner's directive gates implementation on confirming this synchronization.** Still open, each needing its own ruling: the six backend amendments **AMD-1..6** (not covered by the approval) · **DEC-INV-022** (insufficient-stock wording) · **calendar scheduling of Phase 1 vs UAT/Pilot** (the during-Pilot no-new-features ruling stands) · the growing GLOSSARY debt (now incl. hint/success/summary vocabulary). Earlier this cycle (retained): the initiative was commissioned and the two Drafts produced from a three-front audit (docs conventions · per-screen frontend reality · backend error conventions); notable audit facts — zero focus/scroll management and zero `aria-describedby` app-wide · five copy-pasted `classify()` maps and no central VTF→message registry · four fully silent failure paths · the receive dialog unclassified despite being irreversible · 12 backend handlers with no registered validator (the C5 defect class, unguarded). Both files remain untracked; no code, no existing doc status, and no ADR/standard was changed.

*(Previous banner, retained:)* **Updated:** 2026-07-31 · session closed (`/close-session`) — **THE IMPLEMENTATION PHASE IS OVER AND THE PROJECT IS PILOT-READY, PUSHED, AND TAGGED.** One session, four owner-decided milestones: **Epic 2 committed** (`1876ed6`) · **Pilot Readiness specified (PRS Approved, six rulings), executed WS1–WS6 and committed** (`5f8e761`) · **GO decided and everything pushed** (GCM cleared; tag **`pilot-2026-07-31`**) · **`mvp.md` amended by ruling** into Pilot MVP Scope vs Post-Pilot Scope (`4768c6f`). **Nothing is in flight in the code. Next: UAT sessions (pack ready, participants ruled), then the Pilot Transition Checklist on the clinic machine (final clean reset · first backup · scan re-run — deployment runbook D1), then the Pilot begins with the first real operational entry (ADR-0020). During the Pilot: findings in exactly three categories — Bug · Usability · Enhancement (`docs/operations/pilot-findings.md`) — and no new features unless required to keep the system operational.** Open questions for the owner: **NONE** — only owner-scheduled actions (UAT dates; the clinic-machine deployment). Decision audit at close: mechanically verified — DEC-SAL-005 resolved in `sales/decisions.md` · TD-007 in the ledger · the six PRS-Q rulings in the PRS §9 · GO + taxonomy + during-Pilot rule in `go-no-go.md`/`pilot-findings.md`/`ROADMAP.md` · the mvp split in `mvp.md` with the original preserved verbatim. **Local = remote = `4768c6f`; working tree clean but for the two long-standing untracked files; only the dev db container runs.**

**Earlier the same day (seventh cycle):** **PILOT READINESS EXECUTED END-TO-END (WS1–WS6) AND THE GO/NO-GO REPORT WAS SUBMITTED.** The owner approved the PRS with all six rulings (PRS-Q-01..06 → recorded in the PRS §9), approved `docs/operations/`, and added the Lessons Learned deliverable. **DEC-SAL-005 is resolved by ruling** (a basic sales list is required for successful Pilot operation → `sales/decisions.md`), and the **Sales Invoice List was documented (REQ-SAL-005 · BR-SAL-019 · AC-SAL-021..022 · TS-SAL-024..029), built and gated green before WS1** so the validated deployment carries the final pilot scope. **All six workstreams PASSED with recorded evidence** — see `docs/operations/go-no-go.md` (D7) and the deliverables D1–D8 beside it. **Five real defects were found by execution, all fixed, none in the application:** the frontend had no deployment vehicle (WS1 fact, resolved by same-origin static hosting in the API image — no new tool) · PowerShell `>` corrupted the binary backup archive (caught by actually restoring; scripts now keep bytes out of the PS pipeline + validate with `pg_restore --list`) · `OWNER TO` broke cross-role restore (now `--no-owner`) · `$PSScriptRoot` empty on one invocation path · **the historical `????` Arabic fault is root-caused as a Windows-console client artifact — the deployed stack round-trips Arabic byte-identically.** Final sweep: build **0/0** · format **clean** · Domain **163** · Architecture **134** · Integration **236** (+13 list tests) · frontend **235** (+8) · ESLint/Stylelint **clean** · `ng build` **exit 0** (TD-107: 567.05 kB) · STD-BE-051 scan **live and PASS**. **NOTHING from this phase is committed — awaiting the owner (see Open questions).** Pilot stack validated then stopped; only the dev db container runs.

**Previous (sixth cycle):** **EPIC 2 IS APPROVED AND COMMITTED (`1876ed6`).** The owner re-confirmed **Epic Commit Approval** (the fifth-cycle grant had been interrupted before any git command ran), approved Epic 2 in full — including the post-C5-approval implementation corrections and C6's architectural decisions (the preserved Sales/Inventory boundary; batch destinations derived from the recorded inventory trace) — accepted the raw-table deviation (**no `<vf-table>` refactor before or during the Pilot** → **TD-007**), re-accepted the performance results and TD-107, and set the next objective: **Pilot Readiness, not another implementation Epic** (scope recorded in `ROADMAP.md` §Pre-Pilot direction under `BD-PRD-008`). The full commit gate was re-run green this cycle, and **Epic 2 (C1–C6 + documentation) was committed as a single commit. NOT pushed.**

**Where Epic 2 stands in one line (sixth cycle):** **COMPLETE, OWNER-APPROVED, COMMITTED — not pushed.** Commit gate re-run in this session before committing: build **0/0** · format **clean** · Domain **163** · Architecture **134** · Integration **223** · frontend **227** · ESLint/Stylelint **clean** · `ng build` **exit 0** (only the known TD-107 warning, 563.33 kB) · `has-pending-model-changes` **none**. **Push remains blocked twice over:** the push gate requires the Sprint 7 module docs to leave `Draft` (ADR-0017 §7), and **Git Credential Manager still blocks all pushes and only the owner can clear it** (`! git push origin main` — Epic 1's three commits are also still waiting on it).

## Owner rulings (2026-07-31, sixth cycle) — routing audit

| Owner ruling | Force | Recorded in |
|---|---|---|
| **Epic 2 approved** — including the post-C5 corrections (implementation defects, no approved rule changed) and C6's architecture (boundary preserved · trace-derived batch destinations, `IInventorySalesReturnWriter` not promoted to a DEC) | Status | **This file only** — the C1..C5 precedent: an approval is status, not a decision |
| **Epic Commit Approval — re-confirmed and EXECUTED** | Status | This file; the commit itself is the artifact |
| **Raw `<table>` accepted; no `<vf-table>` refactor required before or during the Pilot** | New force with a horizon | **`TECH_DEBT_LEDGER.md` → TD-007** (Accepted Debt — the TD-107-horizon precedent); closes the one open question from the fifth cycle |
| Performance results accepted · TD-107 remains accepted debt for the Pilot | **Restatements** of fourth-cycle rulings already recorded | **Nothing new written** (the C1-cycle precedent: a restatement creates no artifact) |
| **Next objective: Pilot Readiness, not implementation** — deployment validation · backup/restore verification · clean database setup · smoke testing · UAT preparation · final **Go/No-Go**; operational readiness, not functionality | **Sharpens `BD-PRD-008`** with an enumerated scope | **`ROADMAP.md` §Pre-Pilot direction** — the same home `BD-PRD-008` already indexes |
| «Do not expand the implementation further» | Restatement of `BD-PRD-008` | Nothing new written |

**Where work stood before this cycle (retained):** **Epic 1 «Sales MVP» is complete, fully verified and COMMITTED (3 commits) but NOT PUSHED — blocked on Git Credential Manager, which needs the owner.** **Epic 2 «Inventory Operations»: C1–C4 are implemented, green, browser- and performance-verified, and now ALL FOUR ARE OWNER-APPROVED (C1 on 2026-07-31, C2/C3/C4 in the fourth cycle) with the performance results ACCEPTED — still UNCOMMITTED per the owner's ruling that the Epic is the commit unit; C5/C6 «Returns» are NO LONGER BLOCKED — the owner ruled all four open return-document questions, the **Definition of Ready is COMPLETE for both**, and **C5 «Purchase Returns» is now IMPLEMENTED END-TO-END AND GREEN** (domain · migration · API · frontend · tests). **C6 «Sales Returns» has NOT started.**

**Just completed this session (fourth):** context recovered (`/recover-context`) · **five owner directives recorded, each routed to where it belongs** — C2/C3/C4 approved and the performance results accepted (status → this file) · TD-107 confirmed as accepted debt **through the Pilot** (→ `TECH_DEBT_LEDGER.md`) · "no speculative optimization" recognized as a **restatement** and deliberately given no new artifact · the **pre-Pilot scope freeze** written to `ROADMAP.md` §Pre-Pilot direction and indexed as **`BD-PRD-008`** *(not filed straight into `DECISION_LOG.md`, whose own header says it only **indexes** decisions living elsewhere)* · **the C5/C6 blocker re-verified from the repository, narrowed from six questions to four, put to the owner, and ALL FOUR RULED** → **`DEC-PUR-010`** + **`DEC-SAL-010`**, with `BD-PUR-002`/`BD-SAL-002` corrected because their «قواعده تُوثَّق لاحقًا» had just become false · **the full Definition of Ready written for both C5 and C6** on the next free IDs (all contiguous) plus five stale scope markers amended in place · **C5 «Purchase Returns» then implemented END-TO-END AND GREEN** — domain, additive migration, `PRT-` numbering, four commands, five endpoints, the screen, and tests at four layers · **three defects found by gates and tests rather than inspection**, including a **real business bug** (returns moved 4 stock units instead of 480 — no unit conversion). *(Previous session, retained: **owner approved C1** and accepted its destructive migration on the explicit pre-pilot ground · **ADR-0020 written, then rewritten to the owner's own wording and ACCEPTED** — the trigger is the **existence of real data**, with an owner-only escape hatch — plus the **Pilot definition** and the **Pilot Transition Checklist**, propagated to `STD-BE-051` · `overview.md` · the ADR index · `inventory/decisions.md` · **Continuous Capability Mode then ran C2 → C3 → C4 without stopping**, each with its own documentation on the next free IDs, implementation, tests and a full gate sweep · **live-browser verification** of the three new screens at two widths, which **found and fixed a real defect** (raw ISO timestamps in the history) · **performance capture** against ADR-0014 §11 with **20 004 seeded movements**, every budget passed · **C5/C6 stopped at the Definition of Ready** with a six-question decision request · three documentation/UI contradictions corrected (**BR-INV-004**, `_INDEX.md`, four non-existent theme tokens).)*

**GO DECIDED AND THE PHASE IS COMMITTED (eighth cycle, 2026-07-31):** The owner ruled **GO**, granted **Pilot Readiness Commit Approval** (executed — single commit), and set the Pilot-execution rules: **findings in exactly three categories — Bug · Usability · Enhancement** (→ `docs/operations/pilot-findings.md`, echoed in the UAT defect log) and **no new features during the Pilot unless required to keep the system operational** (→ `ROADMAP.md` §Pre-Pilot direction, dated addendum). The GO decision is recorded in `go-no-go.md` in the owner's words. **The next phase is Pilot execution:** UAT sessions (owner + cashier), then the Pilot Transition Checklist on the clinic machine, then real data. **The mvp.md observation is RESOLVED by owner ruling (2026-07-31, ninth cycle):** `releases/mvp.md` is **amended** — restructured into **§1 Pilot MVP Scope** (delivered & validated, per capability) and **§2 Post-Pilot Scope** (each deferral tied to its recorded owner ruling — explicitly postponed, not omitted), with **§3 the long-term vision unchanged** and the **original scope list preserved verbatim in an appendix** (no history rewritten, nothing dropped). `PROJECT_CONTEXT.md` §MVP scope now points at the split instead of duplicating the flat list. **Push + Pilot tag: DONE.** The push succeeded (`main → 5f8e761` — the Git Credential Manager block is CLEARED; the remote now carries the complete history: Epic 1's three held commits, Epic 2 `1876ed6`, and Pilot Readiness `5f8e761`), and the annotated tag **`pilot-2026-07-31`** is created and pushed — the Pilot Transition Checklist's «Current schema tagged» item is satisfied. **PRS-RSK-03 is closed.** What remains before real data: **UAT sessions**, then the remaining checklist items on the clinic machine (final clean reset · first backup · scan re-run), then **the Pilot begins with the first real operational entry** (ADR-0020).

*(Superseded seventh-cycle hand-off, retained:* **the phase was complete and the ball was with the owner:** (a) **Go/No-Go decision** on `docs/operations/go-no-go.md` — proposed verdict GO with three owner-side actions; (b) **commit approval for the Pilot Readiness work** (sales list + hosting + compose.pilot + scripts + docs/operations + PRS/status docs — all uncommitted); (c) **UAT sessions** (pack ready in `docs/operations/uat/`, participants ruled, schedule is the owner's); (d) **GCM push block** (`! git push origin main`) — after the commit, the schema tag completes the Pilot Transition Checklist's last item.*)* *(Superseded plan of this cycle, retained:* **The Pilot Readiness Specification (PRS) is DRAFTED and AWAITS OWNER REVIEW** — `docs/shared/roadmap/releases/pilot-readiness.md`, commissioned by the owner as the single source of truth for the phase, with the eight requested sections, the six workstreams (WS1 Deployment → WS6 Go/No-Go), nine risks, and **six open owner questions (PRS-Q-01..06) to be ruled in the review** — the load-bearing facts behind it: **the frontend has no deployment vehicle today** (compose = db+api only; the API serves no static files), and **ADR-0019's deferred backup/restore runbook comes due in exactly this phase**. **No implementation before the PRS is approved; after approval it executes sequentially WS1 → WS6.** Pilot Readiness introduces no new functionality (`BD-PRD-008`; scope in `ROADMAP.md` §Pre-Pilot direction). — all of which then happened.)* Also pending, independent of that work: **the push** (blocked on the Sprint 7 module docs leaving `Draft` per ADR-0017 §7, and on Git Credential Manager, which only the owner can clear — `! git push origin main`). Carried forward and still untouched: the four Sprint 7 leftovers and the **GLOSSARY sync debt** (~26 terms; «المبيعات» is still not a GLOSSARY module name — and «مرتجع مبيعات» now joins the same debt).

*(Superseded — the fourth cycle's plan, retained for the record: RESUME AT C6 «Sales Returns» — the last capability of Epic 2. Its Definition of Ready is already complete, so the next session writes code, not documentation.** **C1–C5 are all done, green and approved** (C5 approved by design ruling; **R2, R5 and R9 closed**). **Nothing is blocked and no question is open.** C6's one genuinely new piece of logic is **BR-SAL-017**: FEFO can split one sale line across several batches, so a partial sales return must go back to **the batches the goods actually left, in consumption order**, read from the sale-line-level trace (REQ-INV-008) — pinned by **AC-SAL-018 / TS-SAL-020** with a worked example. It reuses **`BatchOperationWriter.ApplyDocumentAsync`**, which C5 added and which already handles multi-line atomicity and reason-less document movements. **After C6, the Epic-level stop conditions:** live-browser verification of **every** new screen at 1440 and 390 (C2/C3/C4 are already done and pass; **C5's and C6's screens are not**) · performance capture · self review · the **Epic Owner Report** — and only then **Epic Commit Approval. Do not commit, do not push.** All of that is now done.)*

**Open questions for the owner (seventh cycle) — all decision-shaped, none technical:** **(1) Go/No-Go** (`docs/operations/go-no-go.md` — the decision is yours alone); **(2) commit approval** for the Pilot Readiness change set; **(3) UAT scheduling** (participants already ruled: you + the assistant/cashier); **(4) the GCM unblock** for pushing (standing since Epic 1). *(Sixth cycle, retained: the raw-`<table>` question was ruled — accepted, no `<vf-table>` refactor before or during the Pilot → **TD-007**.)*

**Previously open, still true:** **NONE blocking. The return-document decision request is ANSWERED and C5/C6 are UNBLOCKED** (owner, 2026-07-31, fourth cycle) — the four rulings are recorded in the section below and in `DEC-PUR-010` / `DEC-SAL-010`. Previously raised and now all ruled (2026-07-31): **(a) hold every Epic 2 commit until the Epic is complete** — the commit unit remains the Epic, not the capability; **(b) ADR-0020 accepted**, with the owner's own rule wording (destructive migrations barred **once real pilot or production data exists**, unless an **explicit owner-approved migration plan** exists); **(c) the Pilot start is now defined** — "the first real operational clinic data intentionally entered for business use" — plus a five-item **Pilot Transition Checklist**. Carried forward: four Sprint 7 leftovers (BR-INV-058 at receiving · promoting a Sprint 7 mechanism to DEC/ADR · the BR-CAT-020 product-config audit · DEC-SAL-005/009 scope) and the standing **GLOSSARY sync debt** (~26 terms; «المبيعات» is still not a GLOSSARY module name).

---

## Session close (2026-07-31, fifth) — decision audit and repository state

**Every ruling made this session is recorded, and nothing was invented.** Mechanically verified at close.

| Ruling / change | Recorded in |
|---|---|
| **C5 approved** | **This file only** — an approval is status, not a decision, and gets no `DEC` ID (the C1–C4 precedent, applied a fifth time) |
| «Continue through C6 and all Epic-level activities without stopping» | **No artifact** — a **restatement** of Continuous Capability Mode, already binding via `workflow.md` and ADR-0017 §11/§11a. The C1-cycle precedent: a restatement creates nothing. |
| **Epic Commit Approval granted** | **This file only**, in the banner at the top — **granted and NOT executed** (the turn was interrupted before any git command). Status, not a decision. |
| The two C6 derivations (receipt-derived conversion · resume-where-the-last-return-stopped · loud failure with no trace) | **`sales/business-rules.md` → notes on BR-SAL-016 and BR-SAL-017, with NO new IDs** — exactly the form C5 used for its conversion note on BR-PUR-016 |
| The C6 implementation mechanisms (`IInventorySalesReturnWriter`, the derived batch distribution, the commit-time ceiling re-check) | **Implementation detail under the Sprint 7 precedent** — documented where they live, **not promoted to a `DEC` unasked** |
| The four defects and their fixes | This file, §«Four real defects» — none changed a business rule |
| The raw-`<table>` standards deviation | This file, **§Open questions** — reported, not resolved unasked |
| Epic 2 completion | `docs/modules/_INDEX.md` updated in the same pass, not left to the end |

**Mechanically validated at close:** `REQ-SAL 001..004` · `BR-SAL 001..018` · `AC-SAL 001..020` · `TS-SAL 001..023` · `DEC-SAL 001..010` — **all contiguous, no gaps, no duplicates, no Approved ID renumbered, and no new ID minted this session** (C6's IDs were all written in the fourth cycle; implementation added only clarifying notes).

**No new ADR was created**, and the one candidate was considered and rejected on the record: `IInventorySalesReturnWriter` **applies** DEC-SAL-006's ruled boundary rather than changing it, and mirrors the existing `IInventoryConsumptionWriter` exactly. **Flagged in the Epic Owner Report so the owner can overrule.**

**Repository state: 147 changed paths, NOTHING COMMITTED, nothing pushed.** The tree carries Epic 2 in full (C1–C6: domain, application, infrastructure, API, two migrations, frontend, tests) plus the Epic 2 documentation. **The two long-standing untracked files** (`docs/releases/…`, `docs/ui/product-editor-ux-architecture.md`) are untouched as always. **All verification servers were shut down at close** — including the stale `:4200`/`:4300` dev servers inherited from earlier sessions, which had already produced one bogus routing report in the C2/C3/C4 pass. Only the database container (`:5434`) is left running.

## Epic 2 — **C6 «Sales Returns» DONE, and every Epic-level activity is complete** (2026-07-31, fifth cycle)

**Owner directive this cycle:** C5 approved · continue immediately with C6 · do not stop between C5 and C6 · then run browser verification, performance capture, self review, final gates and the Epic Owner Report in sequence without stopping. **All of it was done, and no defined stop condition was hit.**

### C6 — what was built

**Domain:** `SalesReturn` + `SalesReturnLine` + `SalesReturnStatus` (**two members only, no `Cancelled`** — DEC-INV-037, with a domain test that fails if one is added) · six new error codes **VTF-SAL-015..020** with ar/en resources · additive-only migration **`20260731042452_SalesReturns`** (ADR-0020 satisfied: `Up` is `CreateTable`/`CreateIndex` plus the `SRT-` sequence) · the `SRT-` number **through the same `nextval` mechanism as `SAL-`/`PRT-`, not a second one** · four commands + the `returnable-lines` read · five endpoints · the `/sales/:id/returns/new` screen, store, api service, ~30 `salesReturn.*` keys, and the **«إرجاع من العميل»** entry shown **only on a Committed invoice** (BR-SAL-015).

### The one structural difference from C5 — required by a rule, not chosen

**`SalesReturnLine` carries no `BatchId` at all**, where `PurchaseReturnLine` carries one. Two independent reasons, both owner-ruled: FEFO may split **one sale line across several batches** (BR-SAL-017), so a single destination could not express the truth; and **Sales may not hold a batch reference at all** (BR-SAL-013 / DEC-SAL-006 — «المبيعات لا تقرأ الدفعات، ولا تختارها، ولا تخزّن مرجعًا لها»). Purchasing has no equivalent rule.

**So C6 does not reach into Inventory the way C5 does.** A new public contract — **`IInventorySalesReturnWriter`**, the exact mirror of the existing `IInventoryConsumptionWriter` — lets Sales state intent («put sale line L's portion back») while **Inventory** reads the recorded consumption trace, derives the destination batches and applies the movement through the shared `BatchOperationWriter.ApplyDocumentAsync`. **An architecture test now fails if a batch reference ever appears on the sales-return aggregate, its line, or its command.** This is **implementation detail under the Sprint 7 precedent** — documented where it lives, **not promoted to a `DEC` unasked**.

### Two derivations, both from recorded data — never from today's configuration

Both follow **C5's receipt-derived-factor precedent** and are written into the docs as clarifying notes on **BR-SAL-016 / BR-SAL-017 with no new IDs** (the same choice C5 made for BR-PUR-016):

1. **The unit conversion.** A return line's quantity is in the original **sale unit**; batches hold the smallest **stock unit**. The factor used is the one that **actually applied** — total consumed stock ÷ the line's sold quantity — not the catalog's factor today, which may have been edited since and would move the wrong amount. Multiply-before-divide, and a quantity that cannot be expressed exactly at the ledger's precision is **rejected, never rounded** (BR-INV-058).
2. **The consumption order.** FEFO consumed in BR-INV-050's total order (expiry ↑ nulls last · receive date · batch id), and **every component of that key is immutable after receiving** — verified before relying on it — so re-deriving reproduces the order the goods actually left in. **The ledger's own `(OccurredAt, Id)` sort was deliberately not used:** a sale's movements share one timestamp and the tie-break id is random, so it would give a stable order that is *not* the consumption order.

**A user-visible consequence, stated rather than buried:** a second partial return **resumes where the first stopped** and does not refill a batch already made whole (6 from A + 4 from B; return 8 ⇒ A +6, B +2; a later 2 ⇒ **B only**). Recorded in BR-SAL-017.

### **Four real defects, each found by a gate or an Epic-level activity — none by casual inspection**

1. **C5's two return validators were never registered** (`Application/DependencyInjection.cs`). Validators resolve through `GetServices`, so an unregistered one is **not a weaker validator — it is no validation at all**: a missing `purchaseInvoiceId` fell through to the handler's `!` and would surface as a **500 instead of the documented per-field 400** (AC-PUR-019). Found while wiring C6's equivalents. **All four are now registered and an integration test pins the 400.**
2. **Both return screens were completely unstyled** — found by the live-browser pass, which C5 had never had. `.banner`, `.banner-success`, `.page`, `.vf-table` and the rest are defined **nowhere** in the codebase, and the UI Kit's `.vf-table` rules are `::ng-deep`-scoped inside `<vf-table>` so they never reach a raw table. The success banner computed to **`rgba(0,0,0,0)`** — no background at all. **This is exactly the C4 defect class** (tokens that do not exist fail silently while Stylelint passes). Both screens now carry a `styles` block using the **real** tokens; the success banner computes to `rgb(240,253,244)` on `rgb(21,128,61)` and the error banner to `rgb(254,242,242)` on `rgb(185,28,28)` — the same values the C4 pass confirmed.
3. **The two-draft race reported the wrong reason.** BR-SAL-016 *predicts* this outcome — drafts do not reserve, so two drafts can each pass the add-line ceiling and the second must fail at commit. It did fail, and nothing was saved, but it failed with **VTF-SAL-020 «تعذّر تحديد الدفعات»** instead of the over-return error: the commit-time walk came up short and the defensive check fired first, misdiagnosing *"someone returned it first"* as *"the ledger is unreadable"*. **The ceiling is now re-checked at commit in the line's own sale unit** and the case raises **VTF-SAL-016**, which the screen already has the right Arabic message for. **TS-SAL-019 covers only the add-time ceiling, so this path had no test at all** — one was added.
4. **A pre-existing integration test was order-dependent and would have failed in CI.** `TS_SAL_001` asserted that **zero `Consume` movements exist anywhere in the database** — true only while no test had ever committed a sale, which C6's tests must do. Rewritten to measure the *change* across the two draft creations, which is what the rule (a draft consumes nothing) actually says. **The rule is not weakened; the assertion now measures it instead of global state.**

### Live-browser verification — **both return screens, 1440×900 and 390×844**

Run against the full stack (db :5434 · **Release** API :5080 · a **fresh** `ng serve` on :4400 — the stale-server trap from the C2/C3/C4 pass was deliberately avoided) in headless Chrome over CDP. *(The Chrome extension is still not connected.)*

- **`dir=rtl`, `lang=ar`, horizontal overflow exactly 0 px** on both screens at both widths · **zero console errors** throughout, including through form interaction and submission.
- **The four documented columns in order** on each screen, and **none of the three forbidden controls**: no reason field, no batch picker, **no amount/total/currency anywhere** — verified by scanning the rendered text for «سبب», «دفعة» and any currency form.
- **A real return was driven through the C6 form:** quantity → «تثبيت المرتجع» → success banner reading **«تمّ تثبيت المرتجع · رقم المرتجع: SRT-000001»**, with **on-hand moving 244 → 249** and the screen's remaining returnable falling **12 → 7 → 5** across two returns.
- **The rejection was driven too:** returning 99 against a remainder of 7 produced the Arabic business message «الكمّية المرتجَعة تتجاوز المتبقّي القابل للإرجاع. لم يُحفظ أيّ تغيير.» **and nothing moved.**

### Performance capture — **every ADR-0014 §11 budget passes with wide margin**

Measured in **Release** against real PostgreSQL, 40 samples after warm-up, **with 5,002 committed returns seeded** so the numbers mean something (a p95 over three rows would be theatre).

| Endpoint | p50 | **p95** | max | vs budget |
|---|---|---|---|---|
| `GET /sales-invoices/{id}/returnable-lines` (C6) | 15.8 | **19.3** | 23.2 | **PASS** (6 % of 300 ms) |
| `GET /purchase-invoices/{id}/returnable-lines` (C5) | 15.8 | **21.1** | 21.9 | **PASS** |
| `POST /sales-returns` (create draft) | 16.3 | **24.0** | 35.8 | **PASS** |
| **`POST /sales-returns/{id}/commit`** (trace read + distribute + ledger) | 16.2 | **26.9** | 32.5 | **PASS** (9 %) |
| `GET /inventory` (projection, unchanged) | 15.8 | **20.4** | 20.6 | **PASS** |
| `GET /inventory/movements` (history, unchanged) | 15.9 | **19.1** | 25.3 | **PASS** |

**Page load on the PRODUCTION bundle** (served statically with the API proxied — measuring `ng serve` would be meaningless), 1440×900: **FCP 88 ms cold / 76 ms warm** for `/sales/:id/returns/new` and **76 ms** for `/purchases/:id/returns/new` — **≈ 4 % of the 2 s budget**, zero overflow. **Nothing was optimized** (principle 6, and the owner's «no speculative optimization»).

**TD-107 unchanged and not raised:** initial bundle **563.33 kB** against the 500 kB budget — a warning, up 6.42 kB for the C6 screen. Accepted debt through the Pilot by the owner's 2026-07-31 ruling.

### After the capture — the data was cleaned and the invariants re-checked in SQL

**The 5,000 synthetic return rows were deleted** (they were documents with no matching ledger rows, which is precisely the inconsistency C6 must never create). Then, after ~90 real return operations:

- **`BR-INV-005` holds: Σ `remaining_quantity` = Σ `on_hand_quantity` = 251.000.**
- **`BR-INV-062` holds: 47 committed returns → 47 ledger rows, and zero committed return lines without one.**
- **Drafts moved nothing: 46 draft returns → 0 ledger rows** — BR-SAL-016's «المسودة لا تحجز» confirmed at the data level, not just in a test.

**Dev-database side effects (pre-pilot, stated rather than buried):** this cycle created sales invoice `SAL-000003` and received purchase invoice `PUR-000015`, plus ~93 return documents (47 committed) from the browser and performance runs. All development data — **not** the "first real operational clinic data intentionally entered for business use" that ADR-0020 defines as the Pilot start. **Pre-existing and unrelated:** the dev seed products' Arabic names are still stored as `????` (an encoding fault from an earlier session); every string the new screens own renders correctly.

### One standards deviation, reported rather than hidden

Both return screens use a **raw `<table class="vf-table">`** instead of the UI Kit's `<vf-table>` component, which the Frontend Page mode's "UI Kit only" rule would prefer. **C5 introduced it and the owner approved C5; C6 mirrored it for consistency.** The UI Kit table is a scrollable PrimeNG datatable with state storage, built for data lists rather than for four rows each carrying an editable input — so converting is a real design change, not a cleanup. **It is left as it is, and flagged here for the owner's call rather than changed unasked.**

## Session close (2026-07-31, fourth) — decision audit and repository state

**Every ruling made this session is recorded in the repository, and nothing was invented.** Mechanically verified at close.

| Ruling / change | Recorded in |
|---|---|
| C2/C3/C4 approved · performance results accepted | **This file only** — status, not decisions (the C1 precedent: an approval gets no `DEC` ID) |
| **TD-107 accepted debt *through the Pilot*** | `TECH_DEBT_LEDGER.md` → TD-107, appended as a dated owner ruling; **not** a Pilot Readiness blocker |
| "No speculative optimization" | **No artifact** — a restatement of principle 6 · ADR-0014 §11 · TD-107's own 2026-07-17 ruling |
| **Pre-Pilot scope freeze / Pilot Readiness direction** | `docs/shared/roadmap/ROADMAP.md` §Pre-Pilot direction, indexed as **`BD-PRD-008`** |
| **The four return-document rulings** (pattern · `PRT-`/`SRT-` · partial returns · one invoice per return) | **`DEC-PUR-010`** and **`DEC-SAL-010`** — one per module, because each document is owned by exactly one |
| Returns' rules are no longer "documented later" | **`BD-PUR-002`** and **`BD-SAL-002`** updated — both said «قواعده تُوثَّق لاحقًا», which the rulings made false |
| C5/C6 requirements, rules, criteria, scenarios | `purchasing/` **REQ-PUR-006 · BR-PUR-014..018 · AC-PUR-019..025 · TS-PUR-034..041** · `sales/` **REQ-SAL-004 · BR-SAL-014..018 · AC-SAL-014..020 · TS-SAL-016..023** |
| Five stale scope markers that contradicted the code | Amended **in place with superseded text preserved** across `purchasing/` (4) and `sales/` (2) |

**Mechanically validated at close:** `REQ-PUR 001..006` · `BR-PUR 001..018` · `AC-PUR 001..025` · `TS-PUR 001..041` · `DEC-PUR 001..010` · `REQ-SAL 001..004` · `BR-SAL 001..018` · `AC-SAL 001..020` · `TS-SAL 001..023` · `DEC-SAL 001..010` — **all contiguous, no gaps, no duplicates, no Approved ID renumbered.**

**One ID withdrawn at close, deliberately:** a suffixed **`BR-PUR-016أ`** was written for the unit-conversion clarification and then **folded into BR-PUR-016 as a quoted note with no new ID** — `naming.md` does not sanction suffixed IDs, and this repository's own precedent avoided exactly that form (the C2 performance scenario became a constraint note rather than `TS-INV-036أ`).

**Three rules in the C5/C6 docs are DERIVED, not owner-ruled, and each says so in its own text** — the Received/Committed precondition (BR-PUR-015 / BR-SAL-015), drafts-do-not-reserve (BR-PUR-016), and the batch-distribution order for split sales returns (BR-SAL-017). Full reasoning in the section below.

**Implementation mechanisms introduced this session** — `BatchOperationWriter.ApplyDocumentAsync`, the derived (never stored) returnable ceiling, and the receipt-derived conversion factor — are **implementation detail under the Sprint 7 precedent**, documented where they live and **not promoted to a `DEC` unasked**. The conversion is the one exception: its effect is user-visible, so it is written into BR-PUR-016.

**Repository state: 116 changed paths, NOTHING COMMITTED, nothing pushed** — per the owner's ruling that the Epic is the commit unit. The tree carries C1–C5 in full plus the Epic 2 documentation. **The two long-standing untracked files** (`docs/releases/…`, `docs/ui/product-editor-ux-architecture.md`) are untouched as always.

## Owner rulings (2026-07-31, fourth cycle) — **C2/C3/C4 APPROVED · performance ACCEPTED · pre-Pilot scope frozen**

**Five directives. Three carried new force and were recorded; two were restatements and deliberately created no artifact** — the C1-cycle precedent («no new artifact created for a restatement»).

| Owner directive | Force | Recorded in |
|---|---|---|
| **C2, C3 and C4 are approved.** | Status, not a decision | **This file only** — the C1 precedent: an approval is status and gets no `DEC` ID. |
| **The performance results are accepted.** | Status | **This file only.** The measurements themselves already live in the performance-capture section below. |
| **TD-107 remains accepted technical debt for the Pilot.** | **Sharpens** the existing 2026-07-17 ruling by giving it a horizon | **`TECH_DEBT_LEDGER.md` → TD-107**, appended as a dated owner ruling. Explicitly **not** a Pilot Readiness blocker and **not** on ADR-0020's checklist. |
| **No speculative optimization.** | **Restatement** — already binding | **Nothing new written.** Principle 6 (measure before optimizing) · ADR-0014 §11 (budgets are tripwires) · TD-107's own 2026-07-17 ruling already say it. It is now also echoed inside the TD-107 entry where it bites. |
| **After Epic 2, prepare for Pilot Readiness, not scope expansion — no new capability before the Pilot unless required for successful operation.** | **New force** | **`docs/shared/roadmap/ROADMAP.md` → §Pre-Pilot direction** (the file whose stated job is «direction and sequence»), indexed as **`BD-PRD-008`** in `docs/business/DECISION_LOG.md`. |

**Why the scope ruling did not go straight into `DECISION_LOG.md`:** that file's own header says it **indexes** decisions that live elsewhere and that «the source remains the single place the decision lives». Writing the ruling there with this conversation as its only source would have contradicted the header **and** sourced a decision to conversation history, which non-negotiable 5 forbids. So the decision lives in the roadmap and the log points at it. **No ADR was created:** this is reversible product direction, not architecture, and an ADR would manufacture a review checkpoint the owner did not ask for.

**«Required for successful operation» is left to the owner, deliberately.** It is recorded as an owner-judged criterion, not an inferable test — an AI contributor may not decide that a capability qualifies. The ruling is also scoped so it cannot be misread as a freeze on *everything*: **bug fixes, recorded technical debt, and finishing Epic 2 itself (C5/C6) are not new capabilities.**

**Not started, and that is deliberate:** **Pilot Readiness work has not begun** — the owner gated it on «once Epic 2 is complete», and Epic 2 is not complete. **The four Sprint 7 leftovers and the GLOSSARY sync debt were also left untouched**, because «focus exclusively on C5 and C6» rules them out for now. Both remain listed above so neither is lost.

## Epic 2 — **the C5/C6 blocker is RESOLVED: re-verified, narrowed from six questions to four, and all four RULED** (2026-07-31, fourth cycle)

**The owner's «focus exclusively on completing C5 and C6» was read as a priority, not a design ruling** — it answered none of the open questions, so the Definition of Ready still blocked and **no code was written on the strength of it**. The four questions were put to the owner instead, **and all four were answered in the same cycle.**

### The owner's four rulings — C5/C6 are unblocked

| # | Question | **Owner ruling (2026-07-31)** |
|---|---|---|
| 1 | May the invoice pattern be implemented from, or will the owner design it? | **Implement the invoice pattern.** Header + lines + `Draft → Committed`, mirroring `SalesInvoice`/`PurchaseInvoice`. Commit moves stock through the existing `BatchOperationWriter`. |
| 2 | Number format | **`PRT-000001` (purchase return) · `SRT-000001` (sales return)** — the existing 3-letter + 6-digit shape, each visibly distinct from `PUR-`/`SAL-` so a return can never be misread as an invoice. |
| 3 | Partial returns | **Allowed.** A return line carries its own quantity, capped at what remains returnable on the original line. |
| 4 | One return document ↔ original invoices | **One original invoice per return.** Returning against three invoices means three return documents; the header therefore keeps a single counterparty and unambiguous provenance for BR-INV-069. |

**Recorded in `purchasing/decisions.md` → `DEC-PUR-010` and `sales/decisions.md` → `DEC-SAL-010`** — one entry per module, because each return document is owned by exactly one module («if it can live in one module, it must»). **`BD-PUR-002` / `BD-SAL-002` were updated in the same pass**: both said «قواعده تُوثَّق لاحقًا», which these rulings make false.

**Two questions were withdrawn before asking, as already ruled** — and C5/C6 implement the existing rules rather than re-asking: **Q5 (reason code) → BR-INV-067** («المرتجعات لا تحمل رمز سبب — مستندها هو سياقها») · **Q6 (cancellation) → DEC-INV-037** (correct by an opposing movement; no reversal path invented).

**One case needed no new rule and got none:** returning more than a batch holds is **already covered by BR-INV-061**, which rejects below zero and never clamps.

**The blocker was re-verified in the repository this cycle rather than taken from the handoff.** All **14** occurrences of «مرتجع» across `purchasing/` and `sales/` were read: every one is an explicit out-of-scope marker (`purchasing/` overview:36 · requirements:62 · ui:138 · workflow:34 — `sales/` overview:76,83 · requirements:55,70 · business-rules:51,156 · decisions:18,140,148 · workflow:58). **Zero `REQ-`/`BR-`/`AC-`/`TS-` IDs exist for either return document.**

**Two records the previous handoff missed, and they make the gap sharper, not smaller:** **`BD-PUR-002` «Purchase returns exist»** and **`BD-SAL-002` «Sale refunds exist»** are in the global decision log — but both say **«قواعده تُوثَّق لاحقًا»** (its rules are documented later). So the *existence* of both returns is ruled and their *rules are explicitly deferred and still unwritten*. That is the blocker stated in the owner's own registry.

**Everything else about C5/C6 was already ready.** The Inventory half is fully ruled (**BR-INV-069** provenance-bound and never FEFO · **DEC-INV-033/034/035**), the ledger already carries `PurchaseReturn`/`SalesReturn` in its closed type set, and **`BatchOperationWriter` (from C4) takes both capabilities almost unchanged.** Only the document design was missing, and it is now ruled.

### Definition of Ready — **COMPLETE for both C5 and C6** (documentation written this cycle)

**Every ID below is new, on the next free number, and mechanically validated as contiguous with no gaps and no renumbering** (`REQ-PUR` max 6 · `BR-PUR` 18 · `AC-PUR` 25 · `TS-PUR` 41 · `DEC-PUR` 10 · `REQ-SAL` 4 · `BR-SAL` 18 · `AC-SAL` 20 · `TS-SAL` 23 · `DEC-SAL` 10).

| Capability | Documentation written |
|---|---|
| **C5 Purchase Returns** | **DEC-PUR-010** · **REQ-PUR-006** · **BR-PUR-014..018** · **AC-PUR-019..025** · **TS-PUR-034..041** · a `workflow.md` flow · a `ui.md` screen section (`/purchases/:id/returns/new`) |
| **C6 Sales Returns** | **DEC-SAL-010** · **REQ-SAL-004** · **BR-SAL-014..018** · **AC-SAL-014..020** · **TS-SAL-016..023** · a `workflow.md` flow · a `ui.md` screen section (`/sales/:id/returns/new`) |

**Five stale scope markers were corrected in place, superseded text preserved** — the *documentation-contradicting-code* class that is on the Never list: `purchasing/requirements.md` («لا مرتجعات شراء») · `purchasing/overview.md` · `purchasing/workflow.md` · `purchasing/ui.md` · **`sales/overview.md` and `sales/decisions.md`, whose «الحركتان المسموحتان حصرًا: استلام واستهلاك — لا ثالثة» was already falsified by C1–C4** and is now annotated rather than left to mislead. **`BD-PUR-002` / `BD-SAL-002` were updated too** — both said «قواعده تُوثَّق لاحقًا», which these rulings made false. `docs/modules/_INDEX.md` was updated in the same pass, not left to the end.

**The C6 rule that is genuinely harder than C5, and is written down rather than left to implementation:** FEFO can split **one sale line across several batches**, so a partial sales return must go back to **the batches the goods actually left**, in **consumption order**, read from the sale-line-level trace (REQ-INV-008). **BR-SAL-017** states it and **AC-SAL-018 / TS-SAL-020** pin it with a worked example (6 from batch A + 4 from B, return 8 ⇒ A +6, B +2). C5 has no such case: one purchase line ⇒ exactly one batch (DEC-PUR-008).

### **Three rules in these docs are DERIVED, not owner-ruled — and each says so in its own text**

The owner's four rulings covered **the pattern, the numbering, partial returns, and one-invoice-per-return**. Writing the documentation required three further rules that the rulings did not reach. **None is presented as the owner's**; each carries an explicit provenance note where it lives, so no test later encodes a derivation as gospel:

1. **«Only against a Received / Committed invoice»** (**BR-PUR-015 · BR-SAL-015**) — **derived, not invented**: a return decreases stock, and an invoice that never entered stock would collide with **BR-INV-061** anyway. The rule only makes the rejection early and legible instead of late and cryptic.
2. **Draft returns do not reserve quantity** (**BR-PUR-016**, echoed in BR-SAL-016) — **a deliberate choice with a visible consequence, recorded rather than buried**: two drafts can both pass validation and the **second fails at commit**. Reservation is a whole mechanism nobody asked for, and reservations are themselves out of scope (DEC-SAL-001). **The alternative is the owner's call, not implementation's.**
3. **The batch-distribution order for split sales returns** (**BR-SAL-017**) — the owner allowed partial returns but did not rule **how they distribute** when FEFO split a line. Consumption order is **the only ordering the recorded data supports** (REQ-INV-008). **Its side effect is stated openly:** because FEFO ordered consumption by expiry, a partial return flows back **into the nearest-expiry batch first** — which is where the goods physically came from. **If the owner wants a different order, that single rule changes.**

### C5 «Purchase Returns» — **BACKEND COMPLETE AND GREEN** (2026-07-31)

**Gates after the C5 backend:** build **0/0** · format **clean** (the CRLF trap hit again on the new test file and was **fixed by running the formatter**, plus an import-ordering fix — whitespace only) · **Domain 148** (+12) · **Architecture 121** (+2) · **Integration 211** (+10) · `has-pending-model-changes` **none**.

**Built:** `PurchaseReturn` + `PurchaseReturnLine` + `PurchaseReturnStatus` (two members only — **no `Cancelled`**, and a domain test fails if one is added, because DEC-INV-037 gives no reversal path) · EF configurations · **additive-only migration `20260731032825_PurchaseReturns`** (ADR-0020 satisfied — `Up` contains only `CreateTable`/`CreateIndex`) · the `PRT-` sequence **through the same `nextval` mechanism as `PUR-`/`SAL-`, not a second one** · four commands + the `returnable-lines` read · five endpoints · five error codes with ar/en resources · four new validation keys with ar/en text.

**`BatchOperationWriter` gained `ApplyDocumentAsync`** — it could not be reused unchanged after all. Two things a document needs that an inventory-native operation does not: **all lines commit in one `SaveChanges`** (the existing method saves per call, so a three-line return would have been three transactions and a failure on the third would leave the first two applied — BR-PUR-018 forbids that), and **no reason with a document source** (BR-INV-067 gives returns no reason; the movement instead carries `Source=Purchasing` and a `ReferenceId` to the return line, BR-INV-057).

#### Three defects the tests caught — none by inspection

1. **The architecture gate caught the missing ar/en text** for the four new validation keys before any test of mine did. Fixed by adding the resources, never by touching the gate.
2. **A new return line was tracked as `Modified`, not `Added`**, so `SaveChanges` issued an UPDATE that matched no row → 500. The cause was a missing **`ValueGeneratedNever()`** on the line's id — and `SalesLineItemConfiguration` carries that exact line **with a comment describing this exact bug** («an insert, not a spurious update»). The precedent existed and I had not followed it. Found by capturing the tracked entity states, after two wrong guesses about LINQ translation.
3. **The unit-conversion defect, and it was a real business bug.** Return quantities are in the **original line's purchase unit**; batches hold the **smallest stock unit** (BR-INV-058). Returning 4 cartons of a 10-carton line was removing **4** stock units instead of **480**. **The conversion factor is derived from the receipt itself** (batch received quantity ÷ original line quantity) **rather than from today's catalog factor** — if a product's factor were edited after receiving, the catalog would give a factor that never applied to this stock. Multiply-before-divide so no factor is ever materialized alone (no rounding — BR-INV-058). **Documented as a clarifying note inside BR-PUR-016 — no new ID**, because its effect is user-visible but it is not a new rule. *(A suffixed `BR-PUR-016أ` was written first and then withdrawn at close: `naming.md` does not sanction suffixed IDs, and this repository's own precedent avoided exactly that form — the C2 performance scenario was written as a constraint note rather than as `TS-INV-036أ`.)*

### C5 «Purchase Returns» — **FRONTEND DONE. C5 IS COMPLETE AND GREEN.**

**`/purchases/:id/returns/new`** — page · store · api service · models · ~30 `purchaseReturn.*` i18n keys · **7 store specs** · and the **«إرجاع إلى المورد»** entry on the invoice screen, shown **only for a Received invoice** (BR-PUR-015) so the action does not exist where it would only fail.

**Three controls the rules forbid, and the screen does not have:** **no reason field** (BR-INV-067) · **no batch picker** (the destination is derived — BR-PUR-017 — so a picker would imply a choice nobody ruled) · **no amount or total anywhere** (DEC-INV-035 — a number with a currency beside it would suggest a credit that does not exist). An architecture test now **fails if a reason or money field appears** on any return type, and a second one fails if the add-line command ever accepts a batch.

**Gates after C5 — all green:** build **0/0** · format **clean** · **Domain 148** · **Architecture 121** · **Integration 211** · **frontend 218** (+7) · ESLint **clean** · Stylelint **clean** · `ng build` **exit 0** · `has-pending-model-changes` **none**. **TD-107 unchanged and not raised** — initial bundle **556.91 kB** against the 500 kB budget, a warning, up 13.68 kB with this screen. Per the owner's 2026-07-31 ruling it stays accepted debt through the Pilot and **was not optimized**.

**Still to do:** **C6 «Sales Returns»** (not started), then the Epic-level stop conditions — live-browser verification of every new screen at 1440/390 · performance capture · self review · the **Epic Owner Report** — then **Epic Commit Approval**. Live-browser and performance are Epic-level and are deliberately **not** run per capability.

### Resume here — C5/C6 implementation (no documentation work remains)

1. **C5 first, end-to-end and green before C6 is touched.** Domain `PurchaseReturn` + `PurchaseReturnLine` + status (mirror `PurchaseInvoice`) → EF configuration + **additive** migration (ADR-0020 satisfied; no destructive change) → `PRT-` numbering **through the existing `PUR-`/`SAL-` mechanism, not a second one** → application handlers (create draft · add/remove line · commit) → commit path onto the **existing `BatchOperationWriter`** → endpoints + error codes with ar/en resources → frontend screen/store/i18n/nav → tests at all four layers.
2. **Then C6**, same order, with the batch-split distribution of BR-SAL-017 as its one genuinely new piece of logic.
3. **Then the Epic-level stop conditions:** full gate sweep · live-browser verification of both new screens at 1440 and 390 · performance capture · self review · the **Epic Owner Report** — then **wait for Epic Commit Approval. Do not commit, do not push.**

**Known implementation decisions already taken, so they are not re-litigated:** returned-so-far is **derived**, not stored (below) · over-return is **BR-INV-061**, no new rule · no reason code (BR-INV-067) · no cancellation path (DEC-INV-037).

**One design point the rulings force but do not specify — decided deliberately, not discovered mid-handler:** partial returns (ruling 3) require knowing how much of an original line has already been returned. **Chosen: derive it** by summing committed return lines against that original line at validation time, rather than adding a stored `ReturnedQuantity` column. It cannot drift from the return documents that are its only source, and it needs no backfill. Recorded in the module docs; **implementation detail under the Sprint 7 precedent**, so it is not promoted to a `DEC` unasked. The over-return case needs no new rule: **BR-INV-061** already rejects below zero and never clamps.

(**Sprint 7 «Sales MVP» — THE FRONTEND SLICE IS FINISHED AND THE WHOLE SPRINT NOW PASSES EVERY GATE FOR REAL: build 0/0 · format clean · Domain 126 · Architecture 92 · Integration 182 · no pending model changes · ESLint + Stylelint clean · frontend 177 · `ng build` exit 0 · live-browser verification done at 1440 and 390 with zero overflow and zero console errors.** Two gates failed first and were **corrected, never bypassed**: `dotnet format` (LF endings across every Sprint 7 file) and three 30-day-boundary integration tests that still derived «today» from UTC while the handlers use `IClinicClock` — the tests were wrong, the production code was right, and the clinic time-zone id now lives in exactly one place. **Nothing committed, nothing pushed. CORRECTION to the previous handoff: the tree is TWO change sets, not three — Sprint 6 can no longer be committed alone because its handlers and tests now depend on `IClinicClock`, a Sprint 7 type, so a Sprint-6-only commit would not compile.** **The owner's five answers arrived unfilled, so no action was taken on any of them — all five remain open.** **Sprint 7 module docs are still `Draft`, which blocks the push gate (ADR-0017 §7), not the commit gate.** Prior line: **IMPLEMENTATION STARTED under owner approval and STOPPED MID-SLICE at the owner's `/close-session`. BACKEND COMPLETE AND FULLY GREEN; FRONTEND INCOMPLETE AND THE `ng build` IS RED.** Docker became available this session, so **every backend gate ran for real**: build 0/0 · Domain **126** · Architecture **92** · Integration **182** (including the **18 Sprint 6 tests that had never been executed**) · `has-pending-model-changes` none. **Nothing committed, nothing pushed.** The five slices exist end-to-end on the server side — Sales aggregate + draft lines + commit, the Inventory consumption contract, FEFO allocation with expired-batch exclusion, sale-line-level traceability, and per-batch concurrency detection. The **Angular sales feature is half-written**: models, API services, stores and the create page exist; **the details page and its three components do not**, no i18n keys, no nav entry, no route registration — the frontend therefore **does not compile**. See the session section immediately below for the exact remaining file list. Prior line: **ALL OWNER DECISIONS APPLIED; NO BLOCKING DECISION REMAINS. Documentation-only, NO CODE, owner STOP.** Final four closed: customer = optional free text (DEC-SAL-002) · money rounding Sales-scoped, quantities never rounded (DEC-SAL-004) · clinic local date from **one configured system-wide time zone**, UTC/server/device forbidden (**BR-INV-060**) · **`BR-CAT-020` AMENDED — stock unit must be the smallest measurable unit, reversing its previous "not required to be smallest" clause (DEC-CAT-033)**. Flagged: existing product configs may violate the amended rule and need correction; BR-INV-059 still invalidates UTC date logic in three implemented handlers. Decision-log audit at close found **one gap and closed it: `DEC-INV-026`** now records the R4 expiry-boundary/Clinic-Local-Date ruling, which had existed only as business rules. **Sprint 7 documentation is IMPLEMENTATION READY; nothing committed; owner STOP in force.** Prior line: architecture review rulings applied (11). New: **BR-INV-058** (stock unit = smallest measurable unit, exact conversion, **no quantity rounding**) · **BR-INV-059** (**Clinic Local Date** is the business date basis, UTC prohibited; `ExpiryDate` = last saleable day — also governs BR-INV-013/022/033/036) · traceability raised to **Sale Line level** · concurrency scoped **per Batch** · R5/R7/R9/R10/R11 accepted and recorded. **Flagged: BR-INV-059 invalidates the UTC date logic in the already-implemented Projection/Batch Viewer/Expiry Monitoring handlers, and no clinic-timezone source is documented.** Prior line: **Sprint 7 owner review applied, documentation-only.** 8 rulings applied across 17 docs: expired stock excluded from FEFO (DEC-INV-021) · concurrency-conflict detection required, mechanism left to implementation (DEC-INV-023) · DEC-INV-024 removed → **REQ-INV-008** traceability · isolation boundary approved (DEC-SAL-006/DEC-INV-019) · price snapshot only (DEC-SAL-003) · IsSplittable honored (DEC-SAL-007) · open package out (DEC-SAL-008) · Inventory History still deferred, roadmap note only (DEC-INV-025). New: REQ-INV-008 · BR-INV-056/057 · AC-INV-045/046/047 · TS-INV-050..053 · AC-SAL-012 · TS-SAL-014. **6 owner decisions still open (DEC-SAL-002/004/005/009 · DEC-INV-020/022).** Prior: DoR drafted, 8 Sales docs from placeholders. Prior line: **Sprint 6 — DoR APPROVED + Batch Viewer & Expiry Monitoring IMPLEMENTED (code-complete), Inventory History DEFERRED.** Non-Docker gates green (build 0/0 · arch 76 · domain 101 · frontend 156 · format/ESLint/Stylelint clean · no schema change). **Docker unavailable → integration tests (18, written+compiling, UNRUN), live-browser & performance capture NOT done → commit gate NOT yet satisfiable.** **NOTHING committed, NOT pushed.** Prior: Batch Viewer DoR approved 2026-07-30; Slice 1 Projection COMMITTED, not pushed.)

## Session close (2026-07-31, third) — decision audit and repository state

**Decision audit at close — every ruling made this session is recorded in the repository, and nothing was invented.**

| Ruling / change | Recorded in |
|---|---|
| Destructive-migration rule, **owner's wording** | **ADR-0020 `Accepted`** + `STD-BE-051` + `architecture/overview.md` + `decisions/_INDEX.md` |
| **Pilot start definition** + **Pilot Transition Checklist** | ADR-0020 §When the Pilot begins / §Pilot Transition Checklist |
| C1 approved · its destructive migration accepted pre-pilot | `inventory/decisions.md` — spent-exception entry, **not** a `DEC` |
| Epic 2 commits held to the Epic | Already governed by ADR-0017 §11/§11a + `workflow.md`; **no new artifact created for a restatement** |
| C2 reopened design (source → the ledger; types → BR-INV-065) | **BR-INV-039..045 amended in place**, supersession preserved · REQ-INV-005 · AC-INV-031..036 · TS-INV-031..036 |
| C3 / C4 criteria and scenarios | **AC-INV-051..060** · **TS-INV-057..066** · `workflow.md` flow · two `ui.md` screen sections |
| **BR-INV-004 scope lock corrected** | `write-kernel.md` — amended in place, superseded clause preserved, each dropped item's approving requirement named |

**Mechanically validated at close:** `AC-INV 001..060` · `TS-INV 001..066` · `BR-INV 001..069` · `REQ-INV 001..011` — **all contiguous, no gaps, no duplicates, no Approved ID renumbered**. **`DEC-INV-039` is still free** and is referenced only as such; **no new `DEC` and no new ADR beyond ADR-0020 was created**, because every Epic 2 business decision was already the owner's. The **implementation mechanisms** introduced this session — `BatchOperationWriter`, parsing body enums as string tokens, `FormatService.dateTime()`, and putting the two operations on their own screens — are **implementation detail under the Sprint 7 precedent**, documented where they live and **not promoted to a `DEC` unasked**.

**Repository state: 68 changed paths, NOTHING COMMITTED, nothing pushed** — per the owner's ruling that the Epic is the commit unit. The tree carries C1–C4 in full (domain, application, infrastructure, API, migration, frontend, tests) plus 12 documentation files and ADR-0020. **The two long-standing untracked files** (`docs/releases/…`, `docs/ui/product-editor-ux-architecture.md`) are untouched as always.

## Epic 2 — **PERFORMANCE CAPTURE DONE — every ADR-0014 §11 budget passes with wide margin** (2026-07-31)

**Measured against the documented budgets, not invented ones** (ADR-0014 §11: API p95 < 300 ms typical / < 500 ms worst case · first meaningful paint on desktop < 2 s). API in **Release**, real PostgreSQL.

**The measurement was made meaningful before it was taken.** The dev database held **4 movements** — a p95 over four rows would have been theatre — so **20,000 synthetic movements were seeded first** (clones of the real ones, timestamps spread, `ANALYZE` run), giving **20,004 rows** for the history projection to work against.

### API latency — 40 samples each after warm-up, sequential

| Endpoint | p50 | **p95** | max | vs budget |
|---|---|---|---|---|
| `GET /inventory/movements` (page 1, 25) | 16.0 | **22.4** | 24.8 | **PASS** (7 % of 300 ms) |
| `GET /inventory/movements` (page 1, 100) | 10.1 | **12.1** | 15.2 | **PASS** |
| `GET /inventory/movements` (**deep page 799**, 25) | 48.0 | **52.5** | 54.9 | **PASS** |
| `GET /inventory` (projection, unchanged) | 16.0 | **22.3** | 36.5 | **PASS** |
| `GET /inventory/expiry` (unchanged) | 16.0 | **19.5** | 40.4 | **PASS** |
| `POST /inventory/adjustments` | 16.8 | **23.5** | 34.6 | **PASS** |
| `POST /inventory/write-offs` | 16.3 | **21.8** | 24.6 | **PASS** |

**The one number worth remembering: the deep page costs ~2.3× page 1** (52.5 vs 22.4 ms). That is **offset pagination behaving exactly as expected** — the cost grows with the offset, not with the page size — and it is **inside budget by ~6×** at 20 k rows. **Recorded as the thing to watch, not optimized**: ADR-0014 §11 calls budgets tripwires, and principle 6 forbids speculative optimization. Keyset pagination is the known answer *if* a real breach ever appears.

Two structural facts behind the numbers: the migration already carries **`ix_inventory_movements_occurred_at_id`**, matching the BR-INV-044 sort, and the **constant-query-count** property is enforced by an integration test, so the shape cannot regress silently.

### Page load — the PRODUCTION bundle, not the dev server

Measured on the real optimized build (served statically with the API proxied), cold **and** warm cache, 1440×900:

| Screen | FCP cold | FCP warm | DCL | transferred |
|---|---|---|---|---|
| `/inventory/history` | **96 ms** | 68 ms | 70 ms | ~1.39 MB / 25 requests |
| `/inventory/adjustments/new` | **64 ms** | 64 ms | 45 ms | ~1.07 MB / 27 requests |
| `/inventory/write-offs/new` | **64 ms** | 64 ms | 45 ms | ~1.07 MB / 27 requests |

**All ≈ 3 % of the 2 s budget.** *(Measuring `ng serve` instead would have been meaningless — the dev build is unminified and unoptimized.)*

**One honesty note about the instrument:** the probe's `contentReadyAt` figure (~1 210 ms on every row) is **bounded below by its own fixed 1 200 ms wait** — it is an artifact, not a measurement, and is deliberately not quoted as a result. **FCP, LCP and DCL come from the browser's paint/navigation timing APIs and are real.**

### After the capture — the database was cleaned and the invariant re-checked

**19 901 synthetic rows were deleted** (159 movements remain — the real ones plus the balanced write traffic). The perf writes were designed to **net to zero** (alternating +1/−1), and they did.

**`BR-INV-005` re-verified directly in SQL after all of it: `on_hand_quantity` = Σ `remaining_quantity` = 206.000 — the invariant holds.** That is the constraint C3 exists to protect, checked after ~160 real write operations rather than assumed.

## Epic 2 — **LIVE-BROWSER VERIFICATION DONE for C2/C3/C4, and it caught a real defect** (2026-07-31)

**Run for real** against the full stack — db :5434 · API :5080 · a **fresh** `ng serve` — in headless Chrome over CDP, at **1440×900 and 390×844**. *(The Chrome extension was not connected, so the prior sessions' CDP method was used.)*

### The defect only this pass could find

**The history's date column rendered a raw machine timestamp** — `2026-07-31T02:31:28.451294+00:00` — in every row. `FormatService.date()` parses `yyyy-MM-dd` by splitting on `-`; handed a full ISO instant its third part is `NaN`, so it **silently returns the input unchanged**. Every gate passed: types, lint, 207 unit tests, `ng build`. **Fixed** with a new `FormatService.dateTime()` (its own parse, ar-EG, date + time — matching `ui.md`'s «تاريخ/وقت»), used by both history components, **plus four regression specs** including one that pins the boundary: `date()` still refuses a timestamp, which is *why* the two methods are separate. It now renders **«31‏/07‏/2026، 05:31 ص»**.

**A second, earlier trap worth recording:** the first run reported all three routes resolving to the *wrong* screens. That was **a stale `ng serve` already running on :4200 from an earlier session**, not a routing fault — proven by the new nav entries being absent from its DOM. **Re-run on a fresh server on :4300 and everything resolved correctly.** Had that been taken at face value it would have produced a bogus bug report; had it been dismissed, a real one might have been missed.

### What was verified, on the real stack

- **`dir=rtl`, `lang=ar`, and horizontal overflow exactly 0 px** on all three screens at both widths — **zero console errors anywhere**, including through form interaction and navigation.
- **History:** the **seven frozen columns in the documented order** (BR-INV-041) and **no action column** — the only buttons are pagination · **newest-first** · **signed quantities** (`+30 علبة`, `‎-2 علبة`) · `PUR-000014` rendered as a link that, **when clicked, navigates to `/purchases/:id` and lands on the invoice whose header reads `PUR-000014`** (TS-INV-033) · and **«—» with no link** on the adjustment and write-off rows (BR-INV-043). Mobile shows the same seven fields as cards.
- **Adjustments:** the reason picker offers **exactly the six adjustment reasons** — «منتهي الصلاحية» and «ملوَّث» are **absent from the UI**, so AC-INV-053 is verified at the surface, not only in the API. A **complete adjustment was driven through the form** (product → batch → direction → quantity → reason → save) and returned the success banner with its link to the history; **the new movement then appeared in the history**, taking it from 3 rows to 4.
- **Write-off:** **three** pickers, not four — **there is no direction control** — and **exactly the five write-off reasons**, «موجود» absent.
- **The theme-token fix is confirmed live:** the success banner computes to `rgb(240,253,244)` on `rgb(21,128,61)` — the real `--vf-success-soft`/`--vf-success`. Before the fix it would have been plain page background.
- **The write paths were also exercised directly against the running API**, including the rejection: a write-off beyond the batch returned **409 `VTF-INV-061`** with the Arabic message, and nothing moved.

**Dev-database side effects (pre-pilot, and stated rather than buried):** this pass created purchase invoice **`PUR-000014`** and received it (one new batch, +30), plus one **write-off (−2)** and **two adjustments (+4 and +5)**. All are development data — **not** the "first real operational clinic data intentionally entered for business use" that ADR-0020 defines as the Pilot start.

**Pre-existing and unrelated, unchanged:** the dev seed products' Arabic names are stored as `????` (an encoding fault at seed time from an earlier session). Every string the new screens own renders correctly.

**Frontend tests after the fix: 211** (was 207).

**The performance capture is now also done** — see the section above it. TD-107 unchanged: initial bundle over the 500 kB budget (a warning, not raised).

## Epic 2 — **three corrections made while blocked** (2026-07-31)

Found by review after C4, not by a gate — all three are the *documentation-contradicting-code* class, which is on the Never list and would have failed the commit gate later:

1. **`BR-INV-004` in `write-kernel.md` contradicted the code.** Its scope lock still excluded **التسويات · المرتجعات · تاريخ الحركة · FEFO · FIFO** by name — written when it was true, false as of Sprint 7 and C1–C4. **Amended in place with the superseded clause preserved and each item's approving requirement named** (BR-CAT-020 / BR-INV-042 precedent). **The rule's real intent is untouched and said so explicitly:** the receiving kernel still owns none of it — the new capabilities are separate Inventory paths over the same quantities, and **FIFO remains forbidden**. The same stale sentence in `InventoryBatch.cs`'s class comment was corrected too.
2. **`docs/modules/_INDEX.md` was stale** — it still described C3 as in progress and C4–C6 as not started.
3. **A real UI defect: four theme tokens that do not exist.** The banners and the ± quantity colouring used `--vf-danger-bg` / `--vf-success-bg` / `--vf-danger-text` / `--vf-success-text`, none of which are defined — the theme has `--vf-danger` / `--vf-danger-soft` / `--vf-success` / `--vf-success-soft`. **The fallbacks meant every error banner would have rendered as plain page background and the increase/decrease colour signal would have vanished, while Stylelint passed either way.** Corrected to the real tokens across all four components. **This is exactly the class of fault the live-browser pass exists to catch** — found here by inspection instead.

**Re-verified after all three:** build **0/0** · format **clean** · `ng build` **exit 0** · frontend **207** · Stylelint **clean**.

## Epic 2 — **C5/C6 BLOCKED at the Definition of Ready. This is a defined stop condition, not a pause.** (2026-07-31)

**Continuous Capability Mode ran C1 → C4 without stopping, exactly as directed. It stops at C5 because a gate says so, and CCM never overrides a gate:** *"the Definition of Ready still gates every capability · nothing is invented to keep moving"* (`workflow.md`, ADR-0017 §11a). The playbook's Step 0 is explicit: **if the slice cannot name its `REQ-`/`BR-`/`AC-` IDs, STOP and ask the owner — never fill a gap by inventing.**

### What is ruled, and what is missing

**The Inventory half of both returns is fully ruled and needs nothing:** **BR-INV-069** (provenance-bound, capped at the originating batch, never FEFO) · **DEC-INV-033/034** · **DEC-INV-035** (stock-only) · and the ledger already carries `PurchaseReturn`/`SalesReturn` in its closed type set. **If returns were inventory-native operations, C5 and C6 would already be built** — the `BatchOperationWriter` C4 introduced would take them almost unchanged.

**The blocker is the other half.** **DEC-INV-036 ruled that returns are *standalone documents*, and those documents belong to Purchasing and Sales — not Inventory.** In those two modules **no return documentation exists at all**: verified by search, **every** occurrence of «مرتجع» in `purchasing/` and `sales/` is an explicit **out-of-scope marker** (`purchasing/overview.md:36`, `purchasing/requirements.md:62`, `purchasing/workflow.md:34`, `sales/overview.md:76`, `sales/decisions.md:18/140/148`). **Zero `REQ-`/`BR-`/`AC-`/`TS-` IDs exist for either return**, and no `DEC` describes the document itself.

**Why this is different from C3 and C4, which I did document myself:** there, the requirement and its business rules were already owner-approved (REQ-INV-010/011, BR-INV-061..069) and I derived only the **acceptance criteria and test scenarios** — restatements of approved rules, which is documentation work. For C5/C6 **the requirement and the rules themselves are absent**. Writing them would mean deciding, unasked: the document's **number format and sequence** (a business-visible identifier, like `PUR-`/`SAL-`), its **lifecycle** (draft → committed, and whether a return can be cancelled), whether a return may be **partial** or must return a whole line, whether it carries a **date, supplier/customer, reason, or note**, whether **one return may span several original invoices**, and what happens to a return against an invoice that was **already fully returned**. **Those are business decisions, and inventing them is the first non-negotiable in `CLAUDE.md`.**

### The decision request — C5 and C6 need this before either can start

1. **Confirm the pattern.** DEC-INV-036's rationale says returns reuse the sales/purchase invoice pattern. Is that a ruling I may implement from — **header + lines + draft→committed, mirroring `SalesInvoice`** — or do you want to design it?
2. **Number format** for each document (the existing convention is `PUR-000001` / `SAL-000001`).
3. **Partial returns:** may a return line return *part* of an original line's quantity, or only the whole line?
4. **Scope of one return document:** one original invoice per return, or may a return span several?
5. **Reason:** do returns carry a reason code? **BR-INV-067 says explicitly they do not** («المرتجعات لا تحمل رمز سبب — مستندها هو سياقها») — confirm that still holds now that the document exists.
6. **Cancellation:** does a committed return have a reversal path, or is DEC-INV-037's "correct by an opposing movement" the only route?

**Nothing else in Epic 2 is waiting on this.** C1–C4 are complete, green, and independent of the answer.

## Epic 2 progress — **C4 «Write-Off» DONE and green — R9 CLOSED** (2026-07-31)

**R9 is discharged, and there is a test that proves it:** expired stock has been visible, unsaleable (DEC-INV-021) and **stranded inside `OnHandQuantity` with no exit** since Sprint 7. `Expired_stock_can_finally_leave_inventory_TS_INV_066_R9` writes off an expired batch to zero and asserts the on-hand follows. **An expired batch is deliberately *not* excluded** — DEC-INV-021 keeps expired stock out of *selling*, not out of disposal, and refusing it here would have preserved the very debt R9 named.

**Docs:** **AC-INV-057..060** · **TS-INV-063..066** · a `ui.md` screen section.

**The duplication C3 would otherwise have caused was removed, not repeated.** `BatchOperationWriter` now owns everything the batch-moving operations share — the three things that move together (BR-INV-003/005/062), the reason-list check (BR-INV-067), and the concurrency outcome (BR-INV-068). **C3 was refactored onto it** and both handlers are now ~15 lines. Without it, BR-INV-005 and BR-INV-068 would have had four copies by C6, drifting one paste at a time.

**Only two things differ from an adjustment, and the code says so:** a write-off has **no direction** (it only removes — offering one would invent a capability nobody ruled) and **its own reason list**. «تصحيح جرد» · «رصيد افتتاحيّ» · «موجود» are **not members of the write-off contract enum at all** — «موجود» on a write-off is a contradiction in terms — and **a second architecture test fails if the two lists ever drift**.

**Frontend:** `/inventory/write-offs/new` — page · store (**reusing the adjustment screen's product and batch reads rather than copying them**) · ~15 `writeOff.*` keys · an «إهلاك مخزون» nav entry. The batch picker **shows expiry dates**, because that is usually *why* a batch is being written off, and expired batches stay selectable.

**Tests added (+9):** 4 integration (both quantities moving together, the five-vs-three reason split, the whole-rejection with **no ledger row written**, and the R9 proof) · 1 architecture · 4 frontend store.

**Gates after C4 — all green:** build **0/0** · format **clean** · Domain **136** · Architecture **105** · Integration **201** · frontend **207** · ESLint **clean** · Stylelint **clean** · `ng build` **exit 0** · `has-pending-model-changes` **none**. **No migration** — C4 writes only to existing tables.

## Epic 2 progress — **C3 «Inventory Adjustments» DONE and green** (2026-07-31)

**Closes R5** — «لا آلية مطابقة لثابت BR-INV-005» — by *being* the correction mechanism the debt named. **No drift detector and no reconciliation job was built**: that would be scope invention, and the accepted on-hand race stays accepted.

**Documentation first, on the next free IDs:** **AC-INV-051..056** · **TS-INV-057..062** · a new **workflow.md** flow · a new **ui.md** screen section. `workflow.md` had said *«لا حركة ثالثة … ولا سجلّ حركات»* — true when written, false now; **corrected in place with the superseded text preserved**, since documentation contradicting code is on the Never list.

**Two design points worth the owner's eye:**
- **It is its own screen (`/inventory/adjustments/new`), not a button in the batch viewer.** **AC-INV-021 and BR-INV-018 forbid any quantity-editing action inside that viewer** — both approved and implemented. Putting the adjustment there would have broken an approved rule; the placement is an implementation choice that keeps it intact.
- **The optional `ActorName` field is shown.** BR-INV-066/DEC-INV-030 allow it and C3 is the first screen where it becomes visible — **hiding it would have made the rule dead**. Free text, never validated, never required, with the owner's own examples as the placeholder.

**Domain:** `InventoryBatch.ApplyDelta` — **the floor rule (BR-INV-061) lives on the aggregate, once**, so C4/C5/C6 inherit it instead of re-deriving it four times. It **rejects** as a `BusinessRuleException` (a real business outcome, not a programmer error) and **never clamps** (DEC-INV-032); `Quantity` — the historical received amount — never moves. `ProductOnHand.ApplyDelta` mirrors it so **BR-INV-005 holds through every Epic 2 operation**.

**Three new error codes, each mapping to exactly one rule** (STD-BE-033): **`VTF-INV-061`** below zero · **`VTF-INV-067`** reason not in this operation's list · **`VTF-INV-068`** batch changed while saving. `VTF-INV-068` is deliberately **not** a reuse of `VTF-INV-056`, which BR-INV-056 scopes to sale consumption. All three have ar/en resources and catalog entries.

**The two reason lists stay separate** (DEC-INV-031): the contract enum cannot even express «منتهي الصلاحية» or «ملوَّث», the domain subset checks it again, and **an architecture test fails if the two ever drift**.

**A real defect was found and fixed by a test, not by inspection:** an unknown reason token on the wire produced a **500**, because binding a body enum makes an unknown value a deserialization failure. The request record now parses direction and reason **explicitly** into the canonical validation shape → **400** (STD-API-010/014/023, the `QueryStringParser` philosophy applied to a body). This is exactly the value a real user will get wrong.

**Concurrency, stricter than ruled and said so:** the batch's existing `xmin` token means a **positive** adjustment also gets conflict detection, while BR-INV-068 only requires it for decreasing paths. Kept — it cannot cause a false failure the way a `ProductOnHand` token would — and recorded here rather than discovered later.

**Frontend:** `/inventory/adjustments/new` — form page · store · api service (**reusing the batch-viewer read for the batch picker; no endpoint was added for the form**) · ~40 `adjustment.*` i18n keys · a «تسوية مخزون» nav entry. Depleted batches stay selectable on purpose: an adjustment can add back to a batch that reached zero.

**Tests added (+19):** 7 domain (the floor rule, exact-to-zero, the rejection changing nothing, and the two reason lists) · 1 architecture (contract reasons ≡ the adjustment subset) · 6 integration (both directions with the invariant, the whole-rejection with no row written, the reason split, the optional actor, the single ledger row appearing in the history with «—», 404/400) · 6 frontend store.

**Gates after C3 — all green:** build **0/0** · format **clean** · Domain **136** · Architecture **104** · Integration **197** · frontend **203** · ESLint **clean** · Stylelint **clean** · `ng build` **exit 0** · `has-pending-model-changes` **none**. **C3 adds no migration** — it writes only to existing tables.

## Epic 2 progress — **C2 «Inventory Movement History» DONE and green** (2026-07-31)

**Discharges R2.** Built on the **preserved IDs only** — REQ-INV-005 · BR-INV-039..045 · AC-INV-031..036 · TS-INV-031..036 — with **no new ID allocated** (DEC-INV-038 forbids it; a needed performance scenario was written as a constraint note under the existing IDs rather than as `TS-INV-036أ`).

**The documentation redesign R2 required, and its exact limit.** Only two things changed in the preserved design: **the source** (a projection over the **ledger**, not over `InventoryBatch` — the design flaw R2 named: consumption mutates `RemainingQuantity` without creating a row) and **the type vocabulary** (BR-INV-065's closed set). **BR-INV-042 was amended in place with its superseded clause preserved** (the BR-CAT-020 precedent), never renumbered. Everything else is untouched: read-only · immutable · **the seven frozen fields** · newest-first with a stable tie-break · one projection query.

**A limit worth seeing:** the ledger carries **reason, reason note and `ActorName`** (BR-INV-066/067) and **the history screen does not show them**. BR-INV-041 locks the field list with «حصرًا», and DEC-INV-038 reopened the design for *movement types*, not for new columns — so **no column was added and none was invented**. Surfacing them is the owner's call and has not been made. Recorded in the DTO, the rule and `ui.md` so it cannot be mistaken for an oversight.

**Backend:** `InventoryHistoryQuery` + validator + `InventoryHistoryItemDto` (+ three contract enums mirroring the domain ones) · `InventoryHistoryQueryHandler` — **one projection SELECT plus the pagination COUNT**, with the purchase and sales documents resolved by **left** joins so an inventory-native movement with no document still appears · `GET /api/v1/inventory/movements`, **read-only: no POST/PUT/DELETE exists**.

**Frontend:** `/inventory/history` — page · store · api service · type badge · table · mobile cards · skeleton · ~30 `history.*` i18n keys (the type and source terms are **the owner's own words**) · a «تاريخ الحركة» nav entry. The quantity is rendered **with its sign** (`+` / `−`), never as a bare magnitude.

**Tests added (+20):** 7 integration (rows produced by the **real** receiving and sale-commit paths, not by inserting ledger rows), 1 architecture (**the contract enums must mirror the domain enums** — the handler casts between them, so the agreement is asserted rather than assumed), 12 frontend (6 store · 6 table, covering the seven-column lock, both reference links, and «—» with no link for a write-off).

**Gates after C2 — all green:** build **0/0** · format **clean** (the LF-vs-CRLF trap caught again on the new files and **fixed by running the formatter**, whitespace only) · Domain **129** · Architecture **93** · Integration **191** · frontend **197** · ESLint **clean** · Stylelint **clean** · `ng build` **exit 0** · `has-pending-model-changes` **none**. C2 adds **no migration** — it is a pure read path.

**Not done yet, deliberately:** **live-browser and performance verification are Epic-level stop conditions** and are run once over every new screen at the end of Epic 2, not per capability. TD-107 unchanged: initial bundle **543.23 kB** against the 500 kB budget — a **warning**, and it grew by 3.46 kB with this screen.

## Owner rulings (2026-07-31, third cycle) — **ADR-0020 ACCEPTED · Pilot start defined · Epic 2 commits held to the Epic**

Three rulings, all applied. **The session did not stop here** — the owner directed C2–C6 to continue immediately under Continuous Capability Mode.

1. **Hold every Epic 2 commit until the Epic is complete.** The commit unit remains the **Epic**, not the capability. This confirms the reading taken at the previous close: C1 stays uncommitted, and **Epic Commit Approval remains at the end of Epic 2**.
2. **ADR-0020 accepted — and its rule rewritten by the owner.** The draft was an **absolute** prohibition; the owner replaced it with: *"No destructive migrations are permitted once real pilot or production data exists, unless an explicit owner-approved migration plan has been approved."* **Two substantive changes, not wording:** the trigger is now **the existence of real data**, not a phase boundary; and there is now an **escape hatch that belongs solely to the owner** — approved before the migration ships, never inferred, and no AI contributor may approve or waive one. Propagated to `STD-BE-051`, `architecture/overview.md`, `decisions/_INDEX.md` (now `Accepted`) and the Inventory exception entry.
3. **The Pilot start is defined:** *"The Pilot officially begins when the first real operational clinic data is intentionally entered for business use."* — the transition point between development and operational data. It is **an act, not a date**, and it **excludes seed and verification data** however realistic. A five-item **Pilot Transition Checklist** (all migrations applied · backup completed · seed data finalized · no destructive migrations pending · schema tagged) is recorded in ADR-0020, and executing it is what flips `STD-BE-051` from Manual to Semi-Automatic.

**ADR-0020's two open items are closed by this cycle.** Nothing in it now rests on AI interpretation: the rule text, the Pilot definition and the checklist are the owner's own wording.

## Session close (2026-07-31, second) — **C1 APPROVED · a new standing migration rule recorded · resume at C2**

**No code was written this session.** Context was recovered, the owner ruled, and the rulings were recorded. **Nothing was committed and nothing was pushed.**

### The owner's rulings, and where each one now lives

| Ruling (owner, 2026-07-31) | Recorded in |
|---|---|
| **C1 «Movement Ledger» is approved.** | This section + the Epic 2 section below. Status, not a decision — no `DEC` ID. |
| **C1's destructive migration is accepted, on the ground that we are still pre-pilot.** | `docs/modules/inventory/decisions.md` → **«استثناء مقبول لمرّة واحدة — ترحيل C1 الهدّام»**, written on the accepted-risk pattern, **not** as a `DEC`: it is a **spent** exception with no ongoing force. **`DEC-INV-039` was deliberately left free, not skipped.** |
| **From the pilot onward, no destructive migration is permitted.** | **[ADR-0020](docs/architecture/decisions/ADR-0020-schema-evolution-safety.md)** — new, `Proposed`, indexed — plus **`STD-BE-051`** in `standards/backend-standards.md`, plus a **Schema-evolution-safety row in `architecture/overview.md`** (the "map of what is decided where" named by `PROJECT_CONTEXT.md`, which enumerates every ADR — leaving 0020 out would have made it wrong). Global engineering policy, so it does **not** live in a module. |
| **Continuous Capability Mode continues; no approvals between C2–C6 unless a stop condition fires.** | Already binding — `.claude/rules/workflow.md` + ADR-0017 §11/§11a. Reaffirmed, nothing re-recorded. |
| **Session capacity is an acceptable checkpoint; resume at C2.** | This section. |

### Why the migration rule became an ADR rather than a `DEC` or a decision-log row

`docs/business/DECISION_LOG.md` is a **business** log and this is engineering policy. ADR-0019's own Consequences delegate "migration specifics" to `standards/backend-standards.md`, so the **standards row is the required artifact** — but every row there is sourced to an ADR or a principle, and **no existing ADR owns this subject**: `STD-BE-041`/`042` govern *how* a migration ships and *where* it lives, and **neither constrains what a migration may destroy**. ADR-0019 was not amended, because its subject is the platform choice and provider independence, not data safety. Hence a new ADR — left **`Proposed`**, since a new ADR is a review checkpoint that needs the owner's explicit acceptance.

### Two things recorded honestly rather than smoothed over

1. **`STD-BE-051` is `Manual`, not automated — deliberately.** The rule is **dormant until the pilot begins**, so an architecture test or CI scan today would sit permanently inert or falsely red in the gate sweep. The obligation to automate it (a `DropTable`/`DropColumn`/narrowing-`AlterColumn` scan) is recorded in **ADR-0020 §Consequences** and in a note under the standards table, so it cannot be lost — **and so that nobody later "fixes" the Manual row by adding a gate that cannot pass.**
2. **"The pilot" is a named phase but its start moment is undefined.** `inventory/decisions.md` records **pilot readiness (جاهزية التجربة الأولى)** as Epic 2's governing goal, and `docs/releases/pilot-p1-money-fix-report.md` refers to a "Pilot P1" — but **nowhere does the repository say when the pilot begins**. The trigger was **not invented**; it is an open question, and ADR-0020 applies the conservative reading in the meantime. **This does not block C2–C6: every one of them is additive** (C2 is a projection; C3–C6 add tables and columns), so no destructive migration is currently foreseen in the Epic.

### C1 is approved but **NOT committed** — read this before assuming otherwise

**"C1 is approved" was not read as commit authority, and nothing was committed.** Under Continuous Capability Mode the commit unit is the **Epic**, and **Epic Commit Approval** is a named artifact (ADR-0017 §11a) that the owner did not invoke. Committing on an inferred approval would be a governance breach; leaving the work on disk with this handoff describing it is fully recoverable. **The question is open at the top of this file and nothing waits on it — C2 proceeds either way.**

**Uncommitted at close:** C1's code (`InventoryMovement` + 3 enums, its EF configuration, both writers, the `InventoryConsumption` deletions, migration `20260731003637_InventoryMovementLedger`, `InventoryMovementTests`, and the re-pointed Sprint 7 integration tests), the Epic 2 documentation, and this session's four doc changes (ADR-0020, the ADR index row, `STD-BE-051` + its note, the inventory exception entry).

### Untracked files — checked, and they are the two long-standing intentional ones

`docs/releases/pilot-p1-money-fix-report.md` and `docs/ui/product-editor-ux-architecture.md` are **untracked by intention, not by oversight** — this file has recorded them as "pre-existing untracked, intentionally not committed" in every handoff since Sprint 3 (e.g. lines ~439, ~984, ~1044). The editor doc is a forward-looking design reference **approved in principle but deliberately left `Status: Draft` and uncommitted** pending an owner ruling (recorded under the Edit Product slice); the money-fix report documents work whose code was committed while the report was not. **Neither was touched, deleted, or committed this session.** Nothing new here — noted only because a reader of `git status` will see them beside C1's genuinely new files and should not confuse the two.

### Gates

**No gate was run this session and none was owed** — no code changed. C1's gates stand as recorded below: build **0/0** · format **clean** · Domain **129** · Architecture **92** · Integration **184** · `has-pending-model-changes` **none**.

### Resume here

1. **C2 «Inventory Movement History»** — Definition of Ready first (ADR-0017), then implement as a **projection over the ledger**, reusing the **preserved IDs** (REQ-INV-005 · BR-INV-039..045 · AC-INV-031..036 · TS-INV-031..036 — DEC-INV-038 forbids allocating new ones) and **discharging R2**.
2. Then **C3 → C4 → C5 → C6 without stopping for the owner**, verifying after each capability and fixing immediately. **A failing gate still stops everything.**
3. Stop at the Epic's **seven conditions** and produce the **Epic Owner Report**. **Do not commit, do not push, until Epic Commit Approval.**

## Epic 2 «Inventory Operations» — **ALL DECISIONS RULED · C1 APPROVED (owner, 2026-07-31), GREEN · C2–C6 NOT STARTED**

**Owner ruled every decision on 2026-07-31 in one cycle** (AD-1..3, BD-1..9, plus a new rule BD-10). All rulings are recorded — **nothing invented**: `inventory/decisions.md` **DEC-INV-027..038**, `business-rules.md` **BR-INV-061..069**, `requirements.md` **REQ-INV-009/010/011** and **REQ-INV-005 reopened on its preserved ID** (DEC-INV-038).

### C1 — Movement Ledger: **DONE, all backend gates green, APPROVED by the owner (2026-07-31) — but not committed**

- **`InventoryMovement`** (+ `InventoryMovementType` / `Source` / `Reason`): append-only by construction — **no mutator exists**, which a domain test asserts by reflection. Signed quantities: `Increase`/`Decrease` factories take a magnitude so the sign convention lives in exactly one place (BR-INV-064).
- **BR-INV-063 honoured literally:** the ledger **records history and never calculates inventory**. `InventoryBatch.RemainingQuantity` and `ProductOnHand.OnHandQuantity` remain authoritative; nothing derives a quantity from the ledger; no event sourcing.
- **Both existing write paths now emit movements** in the same unit of work as the quantity change (BR-INV-062): `InventoryReceiptWriter` → Receive/Purchasing/+qty/ref=purchase line · `InventoryConsumptionWriter` → Consume/Sales/−qty/ref=**sale line**.
- **`InventoryConsumption` absorbed** (DEC-INV-027): entity, EF configuration and table removed. **REQ-INV-008 sale-line traceability is unchanged** — it now rides on the Consume movement's reference. The Sprint 7 tests were re-pointed at the ledger through a small `Trace` projection, so the assertions still read in business terms.
- **Migration `20260731003637_InventoryMovementLedger`**: creates `inventory_movements`, drops `inventory_consumptions`.
- **Reason vocabulary is the owner's list verbatim**, with a test that fails if a term is added or dropped.

**Gates after C1 — all green:** build **0/0** · format **clean** · **Domain 129** · **Architecture 92** · **Integration 184** · `has-pending-model-changes` **none**. (Domain moved 126 → 129: the 8 absorbed-entity tests were replaced by 11 ledger tests.)

**Data note:** the migration **drops `inventory_consumptions`**, discarding its rows. It is a **destructive migration** and was called out rather than buried — **and the owner then accepted it explicitly on pre-pilot grounds (2026-07-31)**, while creating the standing rule that **no destructive migration is permitted once the pilot begins** (**ADR-0020** · **`STD-BE-051`**). The acceptance is recorded as a **spent, one-off exception** in `docs/modules/inventory/decisions.md`.

### C2–C6 — not started

C2 Inventory History · C3 Adjustments · C4 Write-Off · C5 Purchase Returns · C6 Sales Returns. **No code, no docs beyond the rules already recorded above.** The ledger they all depend on now exists, so each is additive from here.

### Discovery record (the review the owner approved)

**Scope:** Inventory Adjustments · Purchase Returns · Sales Returns · Write-Off · Inventory Movement History.

**State: no approved documentation exists for any of the five.** Zero `REQ-`/`BR-`/`AC-`/`TS-`/`DEC-` IDs. Every occurrence in the repository is an explicit out-of-scope marker (`write-kernel.md:20` BR-INV-004 excludes تسويات/مرتجعات/تاريخ حركة by name; `purchasing/overview.md:36` defers returns and inventory adjustment to "شرائحها"). **No business rule was invented in this discovery.** What follows is a decision request.

### Reused, not re-asked (the discovery's main output)

| Existing decision | How Epic 2 reuses it |
|---|---|
| **DEC-INV-019 / DEC-SAL-006** | The boundary generalizes verbatim: **Purchasing/Sales express return intent · Inventory executes the stock movement · Inventory owns batch selection.** No new architectural boundary is needed for returns. |
| **DEC-INV-001 / DEC-PUR-008** | Return/adjust/write-off writers mirror `IInventoryReceiptWriter` and `IInventoryConsumptionWriter`. No new pattern. |
| **`InventoryConsumption(saleLineId, batchId, quantity)`** | **The single most valuable asset.** Already written at **sale-line** level *explicitly so "future Returns can identify which line consumed which batch"* (REQ-INV-008). **Sales Returns can restore stock to its originating batch without inventing anything.** |
| **`InventoryBatch.PurchaseLineId`** | The identical provenance path for **Purchase Returns**. |
| **DEC-INV-011/012** | Batch status stays derived Active/Depleted; **Epic 2 adds no batch state**, and «returned»/«written-off» must not become states. |
| **DEC-INV-021 + R9** | Write-Off is the capability R9 named as the resolution for stranded expired stock. |
| **DEC-INV-023** | Concurrency-conflict detection, already ruled mandatory for consumption. |
| **DEC-INV-026 / BR-INV-059/060** | Clinic Local Date governs every Epic 2 date. Settled. |
| **BR-INV-058** | Exact conversion, no quantity rounding — applies to a return entered in a sale/purchase unit. Settled. |
| **BR-INV-005 + R5** | R5 deferred reconciliation **explicitly to "future Inventory Adjustments"**. That is this Epic. |
| **BR-PUR-011 / BR-SAL-011** | Received and committed invoices are **immutable**, which forces returns to be **new documents** rather than mutations of the original. |
| **DEC-INV-015/016 · REQ-INV-005 · BR-INV-039..045 · AC-INV-031..036 · TS-INV-031..036** | The **preserved, deferred History design**. DEC-INV-025 ruled that reopening happens **on these same IDs**, and R2 ruled the design **must be redesigned first** because a projection over `InventoryBatch` cannot represent Consume. |

### The decisions genuinely missing — 9, grouped, 7 with a recommendation

**Architectural (ADR-level):**
- **AD-1 — Unified Inventory Movement Ledger.** *Recommended: adopt.* Append-only row per stock change (product · batch · type · signed quantity in stock unit · reference · source module · occurred-at). Consequences: `InventoryConsumption` is **absorbed** (it is already a single-purpose partial ledger); **History becomes a projection over the ledger**, which *is* the R2-mandated redesign; BR-INV-005 becomes auditable. **Without it, adjustments and write-offs have nowhere to be recorded and History cannot be rebuilt.** Everything else in the Epic depends on this.
- **AD-2 — Ledger's relationship to the quantities.** *Recommended: record alongside*, leaving `RemainingQuantity`/`OnHandQuantity` authoritative (BR-INV-001/002 unchanged). The alternative — deriving quantities from the ledger (event sourcing) — contradicts DEC-INV-002 and BR-INV-002 and is a far larger change.
- **AD-3 — Concurrency scope.** DEC-INV-023 mandated conflict detection for **consumption only**; DEC-INV-002 deliberately left receiving optimistic. Epic 2 adds four writers. *Recommended: extend DEC-INV-023 to every stock-**decreasing** path (write-off, purchase return, negative adjustment); leave receiving unchanged.* This widens a ruled scope, so it needs the owner.

**Business:**
- **BD-1 — Actor attribution. BLOCKING, no recommendation possible without your call.** **There is no authentication, user, or actor concept anywhere in the codebase**, and Audit Log is `Not documented` (DEC-CAT-028). Adjustments and write-offs conventionally record *who*. *Recommended: record no actor in Epic 2 and document it as a known, named gap* — the free-text-supplier precedent (BR-PUR-001): do not invent a module. Alternatives: free-text "performed by" (invented data), or block Epic 2 behind an Auth epic.
- **BD-2 — Reason vocabulary for Adjustments **and** Write-Off (one decision, both capabilities). NO RECOMMENDATION — this is business vocabulary I must not invent.** Fixed enumerated Arabic list, or free text? *Recommended shape only:* a short fixed list **plus** an optional free-text note. **You must supply the terms.**
- **BD-3 — Adjustment semantics.** *Recommended: batch-level, both directions, never below zero, rejected rather than clamped.* Batch-level is near-forced: BR-INV-005 requires batch and on-hand to move together, and a product-level adjustment cannot say which batch moved. Rejection over clamping mirrors BR-INV-052 and BR-INV-058.
- **BD-4 — Return targeting.** *Recommended: both returns are provenance-bound, never FEFO.* Purchase Return decrements **the originating batch** (`PurchaseLineId`), capped at its remaining quantity. Sales Return restores to **the originating batch** (`InventoryConsumption`), capped at what that sale line consumed. FEFO is a *sales allocation* rule and must not leak into returns.
- **BD-5 — Sales Return condition handling.** *Recommended: always restock to origin; the existing expiry rules then apply naturally* (a returned-into-expired batch simply stays unsaleable via DEC-INV-021, with no new concept). Alternative: return-and-write-off in one step, which couples two capabilities.
- **BD-6 — Financial scope. The biggest scope-limiter.** Suppliers, Customers, Cash Management and Expenses are **all `Not documented`**. *Recommended: Epic 2 is **stock-only*** — no credit notes, no refunds, no supplier/customer balances, no cost re-recognition — deferred explicitly in the same shape as DEC-SAL-003 deferred price override.
- **BD-7 — Document model.** *Recommended: split.* Purchase/Sales Returns are **standalone documents** with their own numbers and a draft→committed lifecycle (reusing the entire Sales invoice pattern, forced by BR-PUR-011/BR-SAL-011 immutability); Adjustments and Write-Off are **inventory-native operations with no counterparty document**. DEC-SAL-009 («ملغاة») stays closed and out.
- **BD-8 — Reversal policy.** *Recommended: append-only.* No movement is edited or deleted; a mistake is corrected by a new opposing movement. Natural consequence of AD-1.
- **BD-9 — History screen scope.** *Recommended: reopen REQ-INV-005 on the preserved IDs*, reusing the BR-INV-039/041/044/045 shape (read-only · immutable · single projection query · date-desc with a stable tiebreak), **updated for real movement types** — which discharges R2.

### Dependencies · Risks

**Dependencies:** no auth/actor (BD-1) · Audit Log undocumented · Suppliers/Customers/Cash/Expenses undocumented (BD-6) · GLOSSARY sync still deferred and «المبيعات» still absent, with Epic 2 adding ~15 more terms.

**Risks:** (1) absorbing `InventoryConsumption` into the ledger is a **data migration over committed production-shaped data**; (2) implementing BR-INV-005 reconciliation may **surface pre-existing drift**, including the accepted R5 on-hand race; (3) Epic 2 spans **three modules and five capabilities — the largest epic so far**, exceeding the Medium context budget, so it must be split per capability; (4) returns reopen concurrency (AD-3); (5) Write-Off finally clears R9.

### Proposed Epic structure (6 capabilities, dependency-ordered)

**C1 Movement Ledger foundation** (AD-1/AD-2; absorbs `InventoryConsumption`) → **C2 Inventory Movement History** (projection over C1; discharges R2 on preserved IDs) → **C3 Inventory Adjustments** (+ BR-INV-005 reconciliation, closing R5) → **C4 Write-Off** (closes R9) → **C5 Purchase Returns** → **C6 Sales Returns**. Rationale: the ledger must exist before anything can be recorded; adjustments are the simplest write path and validate the ledger; returns are cross-module and go last.

**Status: awaiting owner ruling on AD-1..3 and BD-1..9. No documentation written, no IDs allocated, no code.** Next free IDs when approved: **REQ-INV-009** · **BR-INV-061** · **AC-INV-051** · **TS-INV-057** · **DEC-INV-027** (REQ-INV-005 / BR-INV-039..045 / AC-INV-031..036 / TS-INV-031..036 are **reserved for History** and must be reused, not reallocated).

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
