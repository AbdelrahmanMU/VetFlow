# Epic Owner Report — Pilot UX Polish

> Status: **Submitted** · 2026-08-02 · awaiting **Epic Commit Approval**.
> Nothing is committed and nothing is pushed (ADR-0017 §11a).
> Definition of Ready and the four rulings: [`pilot-ux-polish-epic.md`](pilot-ux-polish-epic.md).

## 1. Verdict in one paragraph

**The Epic is delivered in full.** The responsive navigation drawer, the
list→details route, the money-alignment standard, the two-line date/time **and
the Inventory card on Product Details** are implemented, gated green, and
verified in a real browser at both widths. **The single largest finding was
that most of what was asked for already existed in Approved documentation and
simply was not implemented**, so it needed no new requirement, rule, ID or ADR
— the one genuine exception is the Inventory card, which needed a new
Inventory-owned read projection and is documented as such.

> **Revision 2 (2026-08-02).** The owner reviewed revision 1, approved the Epic,
> and **refused the suspension of the Inventory card** — ruling that Product
> Details is the primary inspection screen and must expose inventory, but
> without violating a business rule, aggregating in the browser, duplicating
> Inventory logic, or bypassing approved APIs. That work is §4a below.

## 2. Tracker items

| Item | Module | Was | Now |
|---|---|---|---|
| **PIT-001** — multi-unit prices not visible; no route to details | Catalog | Approved | **Implemented** |
| **PIT-002** — money not visually consistent in tables | Shared UI | Approved | **Implemented** |
| **PIT-003** — date/time inconsistent and hard to scan | Shared UI | Approved | **Implemented** |
| **PIT-004** — product rows behave like static text | Catalog | Approved | **Implemented** |

**Remaining tracker items: none.** All four are implemented; their status is
updated in the tracker and awaits the owner's closure approval (the tracker's
rule 4 — nothing closes without it).

## 3. What was built

### Phase 1 — responsive navigation (`shell.component.ts`)

The permanent sidebar becomes a drawer at ≤768 px — **one component, not a
second navigation**. Hamburger in a compact top bar · slides from the **right**
(RTL) · closes on the backdrop, on `Esc`, and on choosing a destination ·
focus moves in on open and returns to the hamburger on close · `Tab` **wraps
around inside the drawer** · closed drawer is `inert` **and** `aria-hidden` ·
44×44 touch target · `position: fixed`, so **nothing behind it shifts**.

*Stated precisely, because it is an accessibility claim:* the wrap is a cycle
through **the drawer's own focusables** — last back to first, and first back to
last under `Shift+Tab`. It is not a whole-page trap; the hamburger and the
backdrop button remain focusable outside it, and `Esc` and the drawer's close
button are the deliberate ways out.

**Desktop is byte-for-byte unchanged above the breakpoint** — verified in the
browser, not merely intended.

### Phase 2 — the approved observations

- **PIT-001 / PIT-004** — product rows and mobile cards are focusable, labelled,
  and open **S2 Product Details** on click and on `Enter`; a visible «فتح
  التفاصيل» action makes the affordance discoverable, revealed on hover and
  always present where hover does not exist. This closes a gap against
  `catalog/ui.md` §3, which already required «إجراءات مباشرة على الصف: تفاصيل»
  and «Enter يفتح التفاصيل».
- **PIT-002** — the numeric/money standard now has **one definition**:
  `web/src/app/shared/styles/_numeric.scss`. `<vf-table>` already complied;
  the three raw-`<table>` deviations that TD-007 accepted (purchase lines, sale
  lines, product-details units) now include that one mixin instead of each
  choosing an alignment. Headers were marked so a column and its heading agree.

  **The audit you asked for — «every table displaying prices, costs, totals or
  financial values» — was run across all 14 components that render money:**

  | Surface | Verdict |
  |---|---|
  | Products · purchases · sales list tables (`<vf-table>`) | **Already compliant** — money columns declared `numeric: true` → `vf-td--numeric` → `text-align: end` |
  | Inventory · batch viewer · expiry · movement-history tables (`<vf-table>`) | **Already compliant** — same route, all numeric columns declared |
  | Purchase lines · sale lines · product-details units (raw `<table>`, TD-007) | **Were wrong; fixed** via the shared mixin |
  | Purchase-details and sale-details invoice totals | **Not a column** — a `<dl>` fact; carries `.vf-num` for tabular figures. No alignment rule applies. |
  | Add-purchase-line and add-sale-line dialog previews | **Not a column** — inline preview values |
  | Unit-profile editor · product cards (mobile) | **Not a column** — card/field layout |

  **Nothing was left unexamined**: 6 surfaces already complied, 3 were fixed,
  and 5 are not tabular so the column rule does not reach them.
- **PIT-003** — `FormatService.dateTimeParts()` returns the date and time as two
  lines from a single parse; the movement history table and cards render date
  over time. The Arabic meridiem (ص/م) is kept per your ruling.

### Phase 3 — Product Details

**The page already existed** (route `/catalog/products/:id`, screen S2) and
already showed identity, capabilities, the full unit profile with conversion
factors and roles, a selling price per sale unit, per-unit barcode,
splittability and notes — read-only, with editing only behind its own Edit
action. What was missing was the **way in**, which is what this Epic added.
It now answers *"how much is a box?"* without entering the editor.

## 4. The Inventory card — why it stopped in revision 1

*(Kept as the record of the finding. It is **resolved** — see §4a.)*

**The Inventory card (current stock · batch count · nearest expiry).**

You approved adding it on the stated understanding — mine, in the question I
put to you — that existing endpoints could serve it with no new contract.
**Implementation proved that wrong, so I stopped rather than proceed on a false
premise.** The facts:

| Source | Why it does not serve the card |
|---|---|
| `GET /api/v1/inventory` | Carries **exactly** the needed fields (`OnHandQuantity`, `StockUnitName`, `BatchCount`, `NearestExpiry`) but **cannot filter by product** — and **BR-INV-014 declares the filter list «حصرًا»** (exclusive). Adding a product filter changes an approved business rule. |
| `GET /api/v1/inventory/{productId}/batches` | Paged, with no aggregate. Summing remaining quantities in the browser would **re-implement BR-INV-008 outside the module that owns it** — which the governance rules forbid outright. |
| `GET /api/v1/products` and `/{id}` | **Do not carry stock at all** — even though `catalog/ui.md` §3 lists «الرصيد» as a column. A second approved-but-unimplemented gap, found in passing. |

## 4a. How it was resolved — a new Inventory-owned read (revision 2)

You rejected all three of the options above as framed, and instead ruled: build
the correct architecture. **That is what was done — none of your four
prohibitions was touched.**

### The decision

Of the three candidates, the two that would have violated something were
rejected on the record in **DEC-INV-040**:

| Candidate | Rejected because |
|---|---|
| Add a product filter to `GET /api/v1/inventory` | **Violates BR-INV-014**, whose filter list is «حصرًا». Amending an approved rule for another screen's convenience is an unjustified price. |
| Sum batch pages in Angular | **Duplicates BR-INV-008 outside its owner** and breaks past the first page. Exactly what you forbade. |
| Put on-hand on the Catalog product contract | Makes **Catalog** the narrator of an **Inventory** fact — blurs the module boundary. |

**Chosen: a new read projection owned by Inventory** —
`GET /api/v1/inventory/{productId}/summary` (**REQ-INV-012**). The owning module
reports its own facts; Catalog renders them verbatim. **This is not a new
pattern**: REQ-INV-003 already did exactly this for one product's batches.

### What it returns, and what it refuses to do

On-hand is read **as stored** from `ProductOnHand.OnHandQuantity` (BR-INV-008) —
never summed. Batch count and nearest expiry are scoped to **active batches
only** (BR-INV-009/010). A product that was **never received** returns an
explicit zero with a flag, so the screen says «لم يُستلَم هذا المنتج بعد» rather
than printing an unexplained 0; **only a non-existent product is a 404**
(the REQ-INV-003 precedent, AC-INV-022). No write surface exists (BR-INV-006).

### How the constraints were kept

| Your constraint | How it holds |
|---|---|
| Preserve module boundaries | Inventory owns the read; the Catalog↔Inventory join happens **inside the query handler**, the sanctioned place (ADR-0014 §2). **Architecture tests: 134, all passing.** |
| Preserve CQRS | A query + handler + DTO through the existing `IQueryHandler` pipeline, with its logging and validation decorators. Same shape as every other read. |
| Preserve approved BRs | **No rule amended.** BR-INV-014 is untouched — which is the entire reason this endpoint exists rather than a filter. |
| Preserve ADRs | No ADR changed. ADR-0014 §2 and ADR-0015 followed as written. |
| Avoid duplicated calculations | See below — guarded by a **test**, not a comment. |
| No business logic in Angular | The screen renders four supplied values. It sums nothing, converts nothing, and derives nothing. |
| No hacks, no bypassed APIs | One documented public endpoint, consumed over HTTP like every other read. |

### The duplication guard — stated honestly

EF Core **cannot translate a shared helper inside an expression tree**, so the
"active batch" predicate is necessarily written in two handlers. Rather than
leave that to a comment, **AC-INV-065 pins it with a test**: for the same
product, the summary must agree **field for field** with what the inventory
projection reports. If anyone later changes what "active" means in one path and
not the other, that test fails.

### Documentation

**REQ-INV-012** (requirements) · **AC-INV-061…065** (acceptance) ·
**DEC-INV-040** (the decision and the three rejected alternatives) ·
`catalog/ui.md` §4 card 7 **un-suspended and marked implemented**, with the
data source and the reasoning recorded. **No ID renumbered, none annulled, and
no business rule amended.**

## 5. Verification

| Gate | Result |
|---|---|
| Frontend tests | **314 passed / 54 files** (was 299/53 — **+15**) |
| ESLint · Stylelint | **clean** |
| `ng build` | **exit 0** — only the known TD-107 budget warning |
| Backend build | **0 warnings / 0 errors** |
| `dotnet format` | **clean** |
| Domain · Architecture · **Integration** | **163 · 134 · 243** — all pass (**+7** integration, the new read) |
| **Live browser** | **40/40 checks**, headless Chrome/CDP against the real stack at **1440×900** and **390×844** |
| Console errors | **zero** across the whole run |

The browser run confirms the Inventory card end to end against the real
database — «الرصيد الحالي 248 علبة · عدد الدفعات 8 · أقرب صلاحية: لا توجد
صلاحية · عرض الدفعات» — and asserts from the network log that the number came
from **`/api/v1/inventory/{id}/summary`**, Inventory's own endpoint, rather
than from anything Catalog-owned.

The **never-received** branch was rendered too, on a real product the inventory
projection excludes: it prints «لم يُستلَم هذا المنتج بعد، فلا يوجد له مخزون.»
and **no balance line at all** — the ambiguous bare `0` the §4 amendment
forbids never appears. The endpoint was also checked directly: that product
returns `hasInventoryRecord: false` with zeros, while a non-existent id returns
**404**.

**Stated so it is not overclaimed:** the card's **error/retry** branch is
covered by **unit tests only** — it was not induced in the browser, because
doing so means making a live endpoint fail mid-run.

**Browser verification found one real defect that the unit tests could not.**
Opening the drawer left focus behind on the page underneath: the drawer is
`visibility: hidden` while closed, and an element whose visibility is
mid-transition **cannot take focus**, so `focus()` was silently ignored. jsdom
does not implement `visibility` or `inert` for focus purposes, so the unit test
passed either way. Fixed by deferring the visibility change to the **close**
direction only, so opening flips it immediately. Re-verified 33/33.

**Mutation-checked:** disabling the `inert`/`aria-hidden` guard fails its test,
so the accessibility contract is genuinely pinned rather than incidentally true.

**One regression has no durable pin, and the next session must know it.** The
`visibility`-transition fix is guarded **only** by the browser harness, which
lives in a scratchpad and is **not in the repository**. jsdom implements neither
`visibility` nor `inert` for focus purposes, so **reverting that CSS leaves
310/310 green** while the drawer silently opens without focus. If this matters
beyond the Epic, the harness should be committed as a checked-in verification
script — deliberately not done unasked.

**Also verified, because a fresh browser profile could not exercise it:** an
**existing** user whose browser already holds the pre-Epic table state
(`vetflow.catalog.products.table.v1` with eight columns' widths) and hidden-column
preferences still sees the new actions column and its button, with hidden columns
respected and header/cell counts matching. The new column is additive because
`ProductColumnPreferences` persists **hidden** ids, not visible ones.

**Accessibility:** `aria-expanded`/`aria-controls` on the hamburger, an
accessible name on the drawer, `inert` + `aria-hidden` while closed, focus in
and out, a `Tab` trap, `Esc`, 44×44 targets, and per-row/per-card labels naming
the product. Two ESLint accessibility errors were fixed properly rather than
suppressed (the backdrop became a real `<button>`; the delegated `keydown` moved
to a host binding).

**Performance:** initial bundle **593.78 kB**, up **+9.80 kB** over the Epic —
inside the accepted TD-107 horizon, no new budget breached.

## 6. Screens and APIs changed

**Screens:** application shell (all screens) · products list (table + mobile
cards) · product details · movement history (table + cards) · purchase details
line items · sale details line items.

**APIs — one added, none changed:**

| Change | Detail |
|---|---|
| **Added** | `GET /api/v1/inventory/{productId}/summary` (REQ-INV-012) — read-only, Inventory-owned |
| Changed | **none** — no existing endpoint, DTO or contract altered |
| **Migrations** | **none** — the read uses existing tables only; no schema change, so ADR-0020 is not engaged |

## 7. Documentation changed

- `docs/ui/design-language.md` §5 — the owner's mobile-navigation amendment,
  with the superseded «تنقّل سفلي» preserved, not erased.
- `docs/modules/catalog/ui.md` §4 — the seventh card, recorded as approved and
  **suspended**, with the blocking contradiction stated.
- `docs/operations/uat/product-improvement-tracker.md` — statuses and summaries.
- `docs/ui/pilot-ux-polish-epic.md` — Definition of Ready and the four rulings.

## 8. New Arabic wordings — pending your review

Flagged in `ar.ts`. All are accessible names or a column heading; none is body
copy.

| Key | Wording |
|---|---|
| `nav.primary` | التنقّل الرئيسي |
| `nav.menu.open` | فتح قائمة التنقّل |
| `nav.menu.close` | إغلاق قائمة التنقّل |
| `products.column.actions` | إجراءات |
| `products.row.open` | عرض تفاصيل المنتج {name} |
| `productDetails.section.inventory` | المخزون |
| `productDetails.inventory.onHand` | الرصيد الحالي |
| `productDetails.inventory.batchCount` | عدد الدفعات |
| `productDetails.inventory.nearestExpiry` | أقرب صلاحية |
| `productDetails.inventory.noExpiry` | لا توجد صلاحية |
| `productDetails.inventory.never` | لم يُستلَم هذا المنتج بعد، فلا يوجد له مخزون. |
| `productDetails.inventory.outOfStock` | نفد المخزون من هذا المنتج. |
| `productDetails.inventory.viewBatches` | عرض الدفعات |
| `productDetails.inventory.loading` | جارٍ تحميل المخزون… |
| `productDetails.inventory.error` | تعذّر تحميل بيانات المخزون. بقيّة بيانات المنتج صحيحة. |
| `productDetails.inventory.retry` | إعادة المحاولة |

`products.details.open` («فتح التفاصيل») already existed and was **dead** — it is
now used for the row action, as it was evidently written for.

## 9. Technical debt and risks

- **TD-107** unchanged in kind: **+11.41 kB** initial bundle (595.39 kB).
- **One extra HTTP read per product-details visit.** Deliberate: it is scoped,
  indexed by product, and fails independently — a stock outage degrades one card
  instead of blanking the page. The alternative (widening the product contract)
  was rejected on boundary grounds in DEC-INV-040.
- **The "active batch" predicate now exists in two handlers**, because EF cannot
  translate a shared helper inside an expression tree. Held together by
  AC-INV-065's field-for-field agreement test, not by convention.
- **TD-007** unchanged: the three raw `<table>` screens stay raw; they now share
  the numeric standard through one mixin rather than being migrated to
  `<vf-table>`, which you ruled out before and during the Pilot.
- **The compact breakpoint is 768 px**, the app's existing single breakpoint.
  §5 names **اللوحي** as its own tier, and a modern tablet in portrait
  (~810–834 px) therefore still gets the **permanent sidebar**, not the drawer.
  **Flagged, not decided:** moving the shell's breakpoint to ~1024 px would put
  the drawer on those devices, at the cost of a boundary the repository has
  never defined. **Your call.**
- **Not built, listed for completeness** — approved in `catalog/ui.md` §4 and
  still unimplemented, outside this Epic's scope because you did not name them:
  the product image (REQ-CAT-004), the activity card (REQ-CAT-045), and the
  header actions الأسعار · ملف الوحدات · تعطيل/تفعيل · حذف. S4 and S5 **do not
  exist as routes at all**, which is why no price→S5 affordance was added.
- **Branch:** this lands on `pilot/docs-fixes-and-cloud-deployment`, where
  ADR-0021 is still `Proposed` and the Render deployment is not green.

## 10. Recommendations

1. **Grant Epic Commit Approval** — every stop condition is met and nothing is
   outstanding that blocks the commit.
2. **Rule the tablet breakpoint** — 768 today, or ~1024. Not blocking; the drawer
   works, the question is only which devices get it.
3. **Review the seventeen Arabic wordings** in §8.
4. **Approve closure of PIT-001…004** in the tracker.
5. Consider the two documented-but-missing gaps as a small follow-up — the
   «الرصيد» column in §3 (now trivially servable by REQ-INV-012) and the
   activity card in §4. Both are already approved, so neither needs new
   documentation.

## 11. Epic stop conditions (ADR-0017 §11a)

| # | Condition | State |
|---|---|---|
| 1 | The complete Epic is implemented | **Yes** — including the Inventory card |
| 2 | All tests pass | **Yes** — 314 frontend · 163 + 134 + 243 backend |
| 3 | Architecture tests pass | **Yes** — 134, boundaries intact |
| 4 | Browser verification passes | **Yes** — 40/40, two widths, zero console errors |
| 5 | Performance verification passes | **Yes** — +11.4 kB bundle, one extra scoped read per S2 visit |
| 6 | Self review complete | **Yes** — §9 records the debt and the open questions |
| 7 | Epic Owner Report ready | **This document** |

**Nothing is committed and nothing is pushed. Awaiting Epic Commit Approval.**
