# Product Improvement Tracker

> Status: **Live** — Manual UAT / Pilot Review phase (opened 2026-08-02).
> Mode: **Implementation** since 2026-08-02 — the owner's «Pilot UX Polish»
> Epic message approved PIT-001 … PIT-004 and commissioned their
> implementation. Plan and outstanding rulings:
> [`../../ui/pilot-ux-polish-epic.md`](../../ui/pilot-ux-polish-epic.md).
> New observations recorded here still default to **Observation** handling:
> they are appended and waited on, never acted upon unbidden.
>
> This document is the **single source of truth** for every UX, usability,
> workflow, discoverability, pricing, navigation, wording, and convenience
> improvement observed during manual testing.
>
> See also: [`pilot-findings.md`](../pilot-findings.md) (Bug · Usability ·
> Enhancement, owner-ruled structure) and [`defect-log.md`](defect-log.md)
> (UAT session defects).

## Working rules (binding on this document)

1. **No implementation.** Observations only, until the owner explicitly changes
   the mode from **Observation** to **Implementation**.
2. **Append-only.** Every new observation is appended; **chronological order is
   preserved**.
3. **Stable IDs.** `PIT-NNN` is assigned once and never reused, renumbered, or
   recycled — not even for rejected or withdrawn items.
4. **Nothing is removed.** Items are never deleted; an item leaves the active
   set only by moving to `Rejected` or `Implemented`, and **only with the
   owner's explicit approval of its closure**.
5. **No invention.** Fields are filled from what the owner reported and what
   the repository states — never guessed.
6. **Summaries stay current.** The summary section at the end is recomputed on
   every update.

---

## Status Legend

| Status | Meaning |
|---|---|
| **New** | Recorded from the owner's observation; not yet reviewed. |
| **Reviewed** | Read and understood; awaiting the owner's ruling. |
| **Approved** | The owner agrees the change should happen; not yet scheduled. |
| **Planned** | Approved and placed in a wave / epic / release scope. |
| **Implemented** | Shipped and verified. Closure approved by the owner. |
| **Rejected** | The owner ruled it will not be done. Kept on record permanently. |

## Categories

Discoverability · Navigation · Pricing · Product Details · Validation UX ·
Workflow · Performance · Wording · Visual Design · Accessibility · Reporting ·
Inventory · Purchasing · Sales · General UX

## Priority

Critical · High · Medium · Low

---

## Item Template

> Reference only — **not an observation**. Copy this block for each new item and
> replace `PIT-NNN` with the next unused number.

### PIT-NNN

Module:
Screen:
Priority:
Category:
Status:

Observation

Expected user experience

Current behavior

Why this matters

Recommended solution

Notes

---

## Observations

### PIT-001

Module: Catalog
Screen: Products List
Priority: High
Category: Discoverability
Status: Implemented
Recorded: 2026-08-02
Approved: 2026-08-02 — owner, in the "Pilot UX Polish" Epic message
Implemented: 2026-08-02 — Pilot UX Polish Epic; gated green and browser-verified.
**Closure not yet approved by the owner** (working rule 4).

Observation

When a user browses the Products list, only the **stock-unit selling price** is
visible. Products that have multiple selling units (tablet, strip, box,
carton…) do not expose those prices anywhere from the list. There is also **no
navigation from the product card / list item to a Product Details page** where
all selling units, prices, conversion factors, and product information can be
viewed.

Expected user experience

A user asking *"How much is a box?"* should be able to answer immediately,
without opening the edit screen. The Products list should provide an obvious
way to open **Product Details**, and that page should display:

- Product basic information
- Stock unit
- Purchase unit
- All selling units
- Selling price for every selling unit
- Conversion factors
- Splittability
- Barcode(s)
- Product nature
- Manufacturer
- Category
- Current stock
- Expiry summary (if applicable)

Current behavior

The Products list shows the stock-unit selling price only. Prices for the other
selling units are not surfaced from the list, and there is no route from a list
item to a read-only Product Details view.

Why this matters

Answering a routine price question requires opening the **edit** screen — a
write-mode screen used for a read-only lookup. That is slower at the counter and
puts the user in an editing context they did not intend to enter.

Recommended solution

Owner-stated direction: add a Product Details (read-only) page covering the
fields listed above, and an obvious way to open it from the Products list.

Notes

Owner framing: *"This is a Product Details / Discoverability enhancement
only."* Observation only — not implemented, and no requirement, business rule,
acceptance criterion, test scenario, decision, or ADR was created or changed.
Related: PIT-004 (the same list offers no route into a details view).

---

### PIT-002

Title: Monetary values are not visually centered inside their table column.
Module: Shared UI
Screen: All tables displaying monetary values
Priority: Medium
Category: Visual Design / Consistency
Status: Implemented
Recorded: 2026-08-02
Approved: 2026-08-02 — owner, in the "Pilot UX Polish" Epic message
Implemented: 2026-08-02 — Pilot UX Polish Epic; gated green and browser-verified.
**Closure not yet approved by the owner** (working rule 4).

Observation

Across multiple screens, monetary values appear visually shifted toward the
left side of the column instead of appearing properly centered. This is
noticeable in the Products list and **should be audited across every table
displaying prices, costs, totals or financial values**.

Expected user experience

Financial values should have one consistent presentation across the entire
application.

Current behavior

Monetary values read as left-shifted within their column rather than balanced,
and presentation is not uniform across tables.

Why this matters

Owner-stated business impact: financial data is scanned continuously by clinic
staff. Small visual inconsistencies reduce readability and make tables feel
less polished.

Recommended solution

Owner-stated direction:

- Create one shared styling standard for monetary values.
- Consistent typography.
- Consistent spacing.
- Consistent alignment.
- Same currency formatting everywhere.
- The entire column should look visually balanced.

Notes

Scope is application-wide, not a single screen — the owner asked for an audit
across every table showing financial values. Observation only.

---

### PIT-003

Title: Date and time formatting is inconsistent and difficult to read quickly.
Module: Shared UI
Screen: All screens displaying dates or timestamps
Priority: High
Category: Consistency / Readability
Status: Implemented
Recorded: 2026-08-02
Approved: 2026-08-02 — owner, in the "Pilot UX Polish" Epic message
Implemented: 2026-08-02 — Pilot UX Polish Epic; gated green and browser-verified.
**Closure not yet approved by the owner** (working rule 4).

Observation

Dates and times should follow one visual standard throughout the application.
Current formatting is not always easy to scan, especially inside tables.

Expected user experience

A single formatting standard adopted for the whole application. Owner's
examples:

| Case | Example |
|---|---|
| Date only | `31/07/2026` |
| Date & time | `31/07/2026` / `02:35 PM` (two lines) |

Owner's note: *"(or another owner-approved standard) — the important point is
complete consistency."*

Current behavior

Date and time presentation is not uniform across screens, and is hard to scan
inside tables.

Why this matters

Owner-stated business impact: users constantly compare dates in purchases,
sales, batches, expiry monitoring and inventory history. Consistent formatting
significantly improves readability.

Recommended solution

Owner-stated direction: create one centralized Date Formatting
service/component. Individual screens should never choose their own date
formatting.

Notes

The exact format is **not settled** — the owner explicitly left the standard
open and named consistency as the requirement. Observation only.

---

### PIT-004

Title: Product rows behave like static text instead of interactive records.
Module: Catalog
Screen: Products List
Priority: Medium
Category: Navigation / Discoverability
Status: Implemented
Recorded: 2026-08-02
Approved: 2026-08-02 — owner, in the "Pilot UX Polish" Epic message
Implemented: 2026-08-02 — Pilot UX Polish Epic; gated green and browser-verified.
**Closure not yet approved by the owner** (working rule 4).

Observation

The product row cannot be clicked. There is no obvious way to navigate from the
Products list to a Product Details page. The row behaves like a dead end.

Expected user experience

The employee should immediately understand that product details can be
inspected.

Current behavior

Rows are non-interactive and expose no navigation affordance.

Why this matters

Owner-stated business impact: employees frequently need to inspect a product
without editing it. Making navigation obvious reduces clicks and improves
workflow.

Recommended solution

**Not to be designed now.** The owner recorded possible future UX directions
for later evaluation only: clickable row · a "View Details" action · a
dedicated Product Details page.

Notes

Owner instruction verbatim: *"Do not design or implement now. Only record the
observation."* Related to PIT-001, which describes what such a details view
would need to show; kept as a separate item because this one is about the
missing affordance, not the page contents.

---

## Summary

_Recomputed on every update._

### Total observations

**4** (PIT-001 … PIT-004)

### By module

| Module | Count | Items |
|---|---|---|
| Catalog | 2 | PIT-001, PIT-004 |
| Shared UI | 2 | PIT-002, PIT-003 |

### By priority

| Priority | Count | Items |
|---|---|---|
| Critical | 0 | — |
| High | 2 | PIT-001, PIT-003 |
| Medium | 2 | PIT-002, PIT-004 |
| Low | 0 | — |

### By status

| Status | Count | Items |
|---|---|---|
| New | 0 | — |
| Reviewed | 0 | — |
| Approved | 0 | — |
| Planned | 0 | — |
| Implemented | 4 | PIT-001 … PIT-004 |
| Rejected | 0 | — |

All four are implemented and verified; **none is closed** — closure needs the
owner's explicit approval. Report:
[`../../ui/pilot-ux-polish-epic-report.md`](../../ui/pilot-ux-polish-epic-report.md).

### Top recurring themes

| Theme | Items | Count |
|---|---|---|
| No read-only way to inspect a product — the Products list is a dead end | PIT-001, PIT-004 | 2 |
| No single application-wide presentation standard for a data type (money, dates) | PIT-002, PIT-003 | 2 |
| Information exists but is reachable only through the edit screen | PIT-001, PIT-004 | 2 |

### Category labels used but not yet in the Categories list

The owner used composite labels on PIT-002 and PIT-003. `Visual Design`,
`Navigation` and `Discoverability` are already in the list; **`Consistency`**
and **`Readability`** are not. Recorded verbatim as written — awaiting the
owner's word on whether to add them to the Categories section or map them onto
existing ones.
