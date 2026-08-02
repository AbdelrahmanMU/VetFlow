# Epic — Pilot UX Polish · Definition of Ready

> Status: **Draft — three owner rulings outstanding before Phase 1 and one
> Phase 3 item can start.** Commissioned by the owner on 2026-08-02.
> Gate: ADR-0017 §4 (Definition of Ready) · stop conditions: ADR-0017 §11a.

## 0. Mode

The owner's Epic message of 2026-08-02 **lifts Pilot Observation Mode**
(`.claude/rules/workflow.md`) for this Epic and returns work to **Continuous
Capability Mode** (ADR-0017 §11/§11a): continue automatically between
capabilities, verify after each, fix immediately, stop at the seven Epic
conditions and wait for Epic Commit Approval. **A failing gate still stops
everything.**

The same message is read as **approving PIT-001 … PIT-004** in
[`../operations/uat/product-improvement-tracker.md`](../operations/uat/product-improvement-tracker.md);
their status is moved `New → Approved`, since Phase 2 says "implement every
Approved observation" and nothing else would qualify.

## 1. What the repository already contains

This was verified before planning, and it changes the Epic's shape
substantially. **Most of what the Epic asks for is already specified in
Approved documentation and simply not implemented** — which means it needs no
new documentation, no new IDs, and no ADR.

| Epic asks for | Repository reality |
|---|---|
| Phase 3: "Create a dedicated Product Details page" | **It already exists.** `web/src/app/features/catalog/product-details/` — route `/catalog/products/:id`, screen **S2**, specified in `docs/modules/catalog/ui.md` §4. Read-only, edit only via the Edit action. It already renders identity, capabilities, the full unit profile with conversion factors and roles, **a selling price per sale unit**, per-unit barcode, splittability and internal notes. |
| Phase 3: navigation from the list | **Already an approved requirement, unimplemented.** `catalog/ui.md` §3 rules row actions «تفاصيل · تعديل · الأسعار · تعطيل/تفعيل» and «Enter يفتح التفاصيل». The list navigates only to `/products/new`. |
| Global: "shared date formatting service" | **Already exists.** `FormatService` (`core/i18n/format.service.ts`, STD-FE-042) centralizes `date()`, `dateTime()`, `money()`, `integer()`, `decimal()`. A sweep found **zero** ad-hoc date formatting anywhere in the frontend. What is open is the *presentation*, not the architecture. |
| Global: "consistent money alignment" | **An approved rule already exists and is not implemented.** design-language §6: «محاذاة: النص لليمين والأرقام لليسار بأرقام ثابتة العرض حتى تصطف الخانات». `.vf-num` (styles.scss:83) sets `font-variant-numeric: tabular-nums` **only** — no alignment. |
| Phase 1: responsive navigation | **Real and severe.** `core/layout/shell.component.ts` sets `.sidebar { display: none }` at ≤768 px with **no replacement affordance** — navigation is entirely unreachable on mobile. |

**Not new observations.** The gaps below are discoveries made while planning,
not owner observations, so they are deliberately **not** filed as PIT items.

## 2. The S2 delta against its own approved spec

`catalog/ui.md` §4 specifies six cards. The screen implements four.

| Approved in §4 | Implemented |
|---|---|
| Header actions: تعديل · الأسعار · ملف الوحدات · تعطيل/تفعيل · حذف | Edit + Back only |
| Product image, shown **here only** (REQ-CAT-004) | absent |
| Card 4 «الأسعار» with a direct button to S5 when unpriced (REQ-CAT-025) | price column only; the warning has no route out |
| Card 6 «النشاط» — recent audit activity (REQ-CAT-045) | absent |
| Cards 1, 2, 3, 5 (identity · capabilities · units · notes) | present |
| **Inventory (current stock · batch count · expiry summary)** — **not in §4 at all** | absent |

## 3. Scope, split by what actually blocks

### Buildable now — approved documentation already exists

| # | Work | Approved authority |
|---|---|---|
| A1 | Products list → Product Details navigation (row action + `Enter`) | `catalog/ui.md` §3 · design-language §6 «Enter يفتح» |
| A2 | Price card → S5 affordance when the product is unpriced | `catalog/ui.md` §4 · REQ-CAT-025 |
| A3 | Numeric/money column alignment made consistent app-wide | design-language §6 — **pending ruling Q2** |
| A4 | Date/time presentation unified through `FormatService` | design-language §6 · STD-FE-042 — **pending ruling Q3** |

### Blocked on an owner ruling

| # | Work | Why blocked |
|---|---|---|
| B1 | **Phase 1 — responsive navigation** | Contradicts Approved design-language §5 on the mobile tier. **Ruling Q1.** |
| B2 | **Phase 3 — Inventory section on S2** | Not in the Approved §4 spec. Documentation-First applies. **Ruling Q4.** |

### Deliberately out of scope unless the owner adds them

The product image (REQ-CAT-004), the activity card (REQ-CAT-045), and the
missing header actions (الأسعار · ملف الوحدات · تعطيل/تفعيل · حذف) are approved
and unimplemented, but the owner did not name them and the Epic says "do not
invent any other improvements." **Listed, not built.**

## 4. The rulings needed

### Q1 — the mobile navigation pattern *(blocks Phase 1)*

`docs/ui/design-language.md` is **Approved** and states it is
«مرجع ملزم للتنفيذ». Its §5 adaptive table rules **two different patterns**:

| Tier | Approved pattern | The Epic asks for |
|---|---|---|
| **اللوحي** (tablet) | «شريط جانبي قابل للطي» — collapsible sidebar | hamburger + right drawer — **this is the approved pattern**, no conflict |
| **الجوال** (mobile) | «تنقّل سفلي» — **bottom navigation** | hamburger + right drawer — **conflicts** |

The Epic's Phase 1 spec is the approved *tablet* row applied to the mobile tier
as well. Two options, and only the owner can choose:

- **(a)** Amend design-language §5 so the drawer is the pattern on both tiers.
  This **amends an Approved document** and must be recorded as an owner ruling.
- **(b)** Build the drawer for tablet and **bottom navigation** for mobile, as
  currently approved.

*Note, in the owner's favor:* §5's other mobile rule — «لا جداول: بطاقات» — **is
already honored**; the product list renders cards below 768 px. Navigation is
the only mobile violation, so this ruling touches exactly one cell of §5.

### Q2 — money alignment *(shapes A3)*

PIT-002 reports monetary values as "shifted toward the left" and asks for them
to be **centered**. Approved design-language §6 rules numbers **left-aligned**
(«الأرقام لليسار») with tabular figures so digits line up.

**What was observed is the approved standard, partially applied.** Centering
money would deviate from §6 and would also break digit alignment down a column,
which is the reason §6 rules a fixed edge. Options:

- **(a)** Implement §6 as written — numbers left, tabular, consistently applied.
  No doc change. *(Recommended: it is already approved, and it delivers the
  "visually balanced column" PIT-002 asks for.)*
- **(b)** Rule centering instead — **amends design-language §6.**

### Q3 — the date/time format *(shapes A4)*

`FormatService` currently produces `31/07/2026` for dates and
`31/07/2026, 02:35 م` on one line for timestamps. PIT-003's example is two
lines, `31/07/2026` above `02:35 PM`. Confirm:

- Two-line date-over-time in tables — yes or no.
- The meridiem in Arabic UI: keep **ص/م**, or use **AM/PM** as the example shows.
  *(Recommended: keep ص/م — the app is Arabic-first and `ar-EG` renders it
  natively; the example is read as illustrating layout, not language.)*

### Q4 — the Inventory section on S2 *(blocks B2)*

> **RESOLVED 2026-08-02, and the premise below turned out to be wrong.** The
> owner chose (a). Implementation then proved the claim in this section — that
> existing endpoints suffice — **false**: `GET /api/v1/inventory` cannot filter
> by product and BR-INV-014 declares its filters exclusive. The owner reviewed
> that finding and ruled the backend be extended properly, which produced
> **REQ-INV-012** (an Inventory-owned read projection) and **DEC-INV-040**.
> The text below is kept as the record of what was believed at the time.
> Outcome: [`pilot-ux-polish-epic-report.md`](pilot-ux-polish-epic-report.md) §4a.

Not in the Approved §4 card list. The data is reachable from **existing**
endpoints — `GET /api/v1/inventory/{productId}/batches` (batch rows, remaining
quantity, `TotalCount` = batch count) and `GET /api/v1/inventory` (current
stock per product) — so **no new endpoint is required**, but the screen would
issue an extra call and §4 would need a seventh card.

- **(a)** Add the card and record the §4 amendment (a one-paragraph doc change,
  owner-approved) — then it is buildable with existing APIs.
- **(b)** Drop it from this Epic and keep S2 exactly as approved.

## 5. Definition of Ready — status

| ADR-0017 §4 element | State |
|---|---|
| Business rules identified | **Yes** — no rule is changed; all work closes gaps against approved docs |
| Acceptance criteria exist | **Yes for A1–A4** (existing REQ/AC cited above); **absent for B1/B2** pending rulings |
| Context budget | Bounded: catalog docs · design-language · shell + catalog frontend |
| No speculation | Holds — every item traces to an owner observation or an approved doc |
| Open questions | **Q1 · Q2 · Q3 · Q4 above** |

**Verdict: Ready for A1–A4 once Q2 and Q3 are answered; NOT ready for B1 and B2.**

## 6. Branch context

This Epic accumulates on `pilot/docs-fixes-and-cloud-deployment`, where
**ADR-0021 is still `Proposed`** and the Render deployment is **in flight and
not green**. Stated so the owner is not surprised about where the work lands.
