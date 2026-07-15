# STATUS — Current State of Work

> The single mutable state file. Update it before ending any significant
> session. Stable knowledge does NOT belong here — it goes in `docs/`.

**Updated:** 2026-07-15

## Current sprint

**Sprint 3 — Implementation.** The first product code of VetFlow.

Implementation outranks governance. If implementation exposes a weakness in the
foundation: **record it under "Foundation friction" below, keep working if it is
safe, and evaluate the change only after the feature is complete.** Governance
changes require evidence (Governance Change Policy — `docs/architecture/principles.md`).

**Every implementation session starts at `.claude/playbooks/implementation.md`.**

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

## In flight / next

1. **Owner review of Slice 1** (this stop). Review complete — see findings above.
   Recommendation: fix **R1** (docs-vs-code, arguably required by the
   docs-synchronized gate), **R2**, and **R3** before the first commit; file
   R4/R5 as follow-ups. On approval: commit (gates are green), then choose the
   next slice — natural candidates: S3 product editor (unblocks the primary
   action + creation CTAs) or the auth slice (rule the ADR-0010 open items first).
2. Environment note: Node was upgraded 20 → 24 LTS via nvm (Angular tooling
   requirement); host port 5434 chosen for the dev DB (5432/5433 occupied on
   the dev machine).

## Open items for the owner

1. **Review Slice 1 and rule on the friction proposals below** (especially F1).
2. Internal-code (BR-CAT-006) **format** is undefined in the docs — needed
   before the Create Product slice.
3. CI platform is undecided (no remote, no pipeline). ADR-0016's CI enforcement
   currently runs locally only — pick a platform so the gates become CI.
4. Approve the Sprint 1 shared docs and the `BD-*` registry (carried over).
5. Answer `domain-overview.md` TODOs 2–6 (carried over).
6. Flip ADR-0003…0019 Proposed → Accepted when ready (carried over).
7. Confirm the CI performance budget numbers (ADR-0016 §5) (carried over).
8. Catalog `overview.md` purchase-cost negative-boundary statements
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
