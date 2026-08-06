# Dashboard — Owner Report

> Status: **Report (2026-08-02)** — deliverable of the commissioned Discovery
> (Phase 0) and PRS (Phase 1). **No code was written. Nothing was committed.**
> Written in English per ADR-0002 (engineering/communication docs); the module's
> business documents are Arabic.
> **Read this first, then `decisions.md` — it carries the reasoning this report
> summarises.**

---

## 1 · What was delivered

Nine documents under `docs/modules/dashboard/`, all **Draft**, all awaiting your
approval:

`overview.md` · `requirements.md` · `business-rules.md` · `workflow.md` ·
`ui.md` · **`api.md`** · `acceptance.md` · `test-scenarios.md` · `decisions.md`

**Two notes on the file list, both deliberate:**

- Your list named `workflows.md` and `testing.md`. The repository's naming rule
  fixes these as **`workflow.md`** and **`test-scenarios.md`**, and every other
  module uses those names. **Nothing you asked for is missing** — only the
  filenames follow the existing convention. `api.md` is a **ninth** document,
  on the `inventory/write-kernel.md` precedent.
- **`acceptance.md` was written even though your list omitted it.** ADR-0017 §5
  makes acceptance criteria part of the Definition of Ready. A gate does not
  lapse because a file list didn't mention it.

Identifiers minted: **REQ-DSH-001..010 · BR-DSH-001..020 · AC-DSH-001..026 ·
TS-DSH-001..030 · DEC-DSH-001..012**.

---

## 2 · The one thing you asked for that cannot be built

**«Low stock» is blocked — not rejected, and not quietly dropped.**

It fails on two independent counts, either of which is sufficient:

1. **The number has no definition.** `BR-INV-012` is deferred, and
   **`DEC-INV-004` states literally: «لا يُخترع حدّ كمّي عام… لا منطق نائب ولا
   معيار مؤقت»** — no general quantitative threshold is to be invented, and no
   placeholder logic. It waits on a **per-product Reorder Level owned by
   Catalog**, which does not exist anywhere in the system.
2. **The click-through is also closed.** `BR-INV-014` declares the inventory
   screen's filter list **«حصرًا»** — exhaustive — and low stock is not in it.
   This is the same rule that, one month ago, forced `DEC-INV-040` rather than
   letting the Catalog stock card add a product filter.

**No threshold was invented. No «less than 10». No placeholder.**

**What I need from you — OQ-DSH-1:** either rule a **Reorder Level** as a
Catalog capability (its REQ/BR belong in `catalog/`, not here), and low stock
returns to the board; or it stays a **Future Enhancement**. **This blocks that
one item and nothing else** — the other seven are entirely independent of it.

---

## 3 · Why every element on the board exists

Each was put to your three questions. All three had to be answered.

| Element | Business question | Decision it drives | Destination |
|---|---|---|---|
| **Expired batches** | Is there medicine in my stock I may not sell today? | **Write it off** (`REQ-INV-011`) or isolate it | `/inventory/expiry` · «expired» |
| **Out of stock** | What can I not sell today that I normally sell? | Open a purchase invoice / call the supplier | `/inventory` · «out of stock» |
| **Expiring soon (30d)** | What will I lose within a month if I don't move? | Sell it first; stop buying more of it | `/inventory/expiry` · «expiring soon» |
| **Draft purchases** | Did goods arrive that were never recorded? | **Receive them** — until then every other number is wrong | `/purchases` · «draft» |
| **Draft sales** | Is there a sale that was never committed? | Commit it, or leave it deliberately | `/sales` · «draft» |
| **Today's sales** | How much did we sell today? Is the cashier recording at all? | Tell a slow day from a recording failure | `/sales` · today's date range |
| **Recent movements (5)** | What happened since I last looked? | Catch a wrong adjustment or write-off **early** | `/inventory/history` |
| **Quick actions** | *(not an information element — see below)* | — | `/sales/new` · `/purchases/new` · `/catalog/products/new` |

**Three of these deserve a word beyond the table:**

- **Draft purchases is the strongest item on the board, and the least obvious.**
  A draft purchase usually means **goods physically in the clinic that the
  system does not know about**. While it stays a draft there is no batch and no
  on-hand quantity (`BR-PUR-009/010`). So this item **corrects the dashboard
  itself** — every other number on the board is incomplete until it is cleared.
- **Recent movements is the only item that is awareness rather than alarm**, and
  it earns its place on a specific ground: the movement ledger is
  **append-only, correctable only by a reverse movement** (`DEC-INV-037`). An
  unexpected write-off noticed the next morning is cheap; noticed in a month it
  is permanent history. **Early detection is the only inexpensive correction
  path that exists.**
- **Quick actions is exempt from the three questions, by written reason:** it
  displays no information, so it cannot mislead. But it is **not** three equal
  buttons — §15.1 of the design language requires **one primary action per
  screen**, and §4 says two equally prominent buttons mean the design hasn't
  decided. So **«بيع جديد» is primary and the other two are quieter**
  (`DEC-DSH-008`, marked in its own text as derived from the standard rather
  than ruled by you).

---

## 4 · Why twelve candidates were rejected

Each rejection cites a recorded ruling, not taste. Full table in
`decisions.md` §ب.

| Rejected | Which question it failed |
|---|---|
| Inventory total value | **(2) and (3)** — no morning decision follows, no screen to go to. `DEC-INV-035` also keeps Epic 2's scope inventory-only, with no financial effect |
| Today's profit / margins | **(1)** — there is no approved definition of cost of goods sold. Inventing one is inventing accounting |
| Weekly sales chart | **(2)** — a week's trend does not change what you do this morning |
| Top-selling products | **(2) and (3)** — restocking decisions come from out-of-stock and expiry, not from a ranking |
| Catalog counters (products, categories) | **(2)** entirely — a pride counter, not a decision |
| Welcome card | all three — decoration |
| **Notifications centre** | **(3)**, and it annexes another module: **`BR-INV-032` textually forbids notifications, alerts, background jobs and escalations in read screens**, and `DEC-INV-017` keeps Monitoring deferred |
| Branch comparison | **(1)** — there is no second branch and no branch management screen |
| Depleted batches | **(2)** — nothing is done about a depleted batch |
| Separate adjustment / write-off counters | **(3)** — duplicates the recent-movements destination. Your instruction: group related findings, never duplicate |
| Never-received products | **(2)** — data hygiene, not daily operation |

**This rejection list is the actual output of Phase 0.** A dashboard that
displays everything countable is a template. This one had to argue for each of
eight survivors and record why twelve others died.

---

## 5 · How this improves the daily operation

- **One screen replaces a tour of four modules.** Today, knowing the clinic's
  state means opening inventory, then expiry, then purchases, then sales.
- **One click from noticing to acting.** Every element lands on the destination
  **already filtered on that same condition** — you never re-apply the filter by
  hand. That is what «reduce navigation» has to mean; otherwise navigation is
  merely moved.
- **Zero is the most valuable thing the board can say.** When nothing needs
  attention, you get **one explicit all-clear** — not five zero tiles, not «no
  data» (`BR-DSH-013`). Zero expired batches is **good news, not absent data**,
  and showing it in the language of emptiness inverts its meaning. This is
  precisely what makes thirty seconds possible: **the eye learns that a quiet
  band means «go».**
- **Fixed order, so reading becomes muscle memory.** Items never reorder by
  count (`BR-DSH-012`). A board that rearranges itself has to be re-read every
  morning.
- **A failure never masquerades as calm.** A section that could not load shows a
  retry **inside its own card** and **is never rendered as zero**
  (`BR-DSH-014`). «Zero expired batches» and «could not determine expired
  batches» are contradictory statements, and conflating them is **false
  reassurance inside a safety decision** (`DEC-INV-021`).

---

## 6 · Why this beats a traditional statistics dashboard

| Statistics dashboard | This one |
|---|---|
| Answers **«how are things going?»** | Answers **«what needs my attention right now?»** |
| Shows what is **countable** | Shows what is **actionable** — an element that leads nowhere is deleted (`BR-DSH-002`) |
| Numbers you look at | Numbers you **click through into work** |
| A busy day looks impressive | A **clean day looks empty — and that is the point** |
| Charts communicate trends to an audience | **No charts.** You are not an audience; you run the clinic (`DEC-DSH-004`) |
| Grows by accretion — every new metric gets added | **Grows only through the three questions.** `BR-DSH-020` makes passing them a **condition of existing**, not a later review |
| Money everywhere | **Exactly one money figure** — today's sales total — and `DEC-DSH-012` records why the rest fail question (2) |

The distinction is not stylistic. A statistics dashboard is optimised for
**being informed**; this one is optimised for **being finished by 9:15**.

---

## 7 · Two findings you should see before approving implementation

### 7.1 · The Dashboard is **not** a self-contained module

`DEC-DSH-001` rules that the board **composes reads its modules own** and
computes nothing itself. That is not a preference — it is `DEC-INV-040`
(**your ruling, 2026-08-02**) applied to six sources instead of one: you refused
to let Catalog sum inventory batches in the browser or let the inventory
projection gain a filter that violates «حصرًا», and ordered the **owning module
to serve its own read**.

**The consequence, stated now rather than discovered later:** implementing this
board requires **new read requirements amended into `inventory/`, `sales/` and
`purchasing/`** — with REQ/BR/AC identifiers **in those modules, not in this
one**, and **each needing your approval**. The filters and screens all exist;
what does not exist is a **count** where today only a **page** is returned
(`api.md` §4 has the exact table).

**I am flagging this before approval on purpose.** In the Pilot UX Polish epic,
the S2 stock card was approved on **my stated premise that existing endpoints
sufficed — and that premise proved false at implementation time.** That will
not happen twice.

### 7.2 · The finding that makes the module buildable at all

**Every filter the navigation contract needs is already inside an approved
whitelist:** «out of stock» in `BR-INV-014` · «expired»/«expiring soon» in
`BR-INV-035` · status in `BR-PUR-004` · status and sale-date range in
`BR-SAL-019`.

**⇒ No business rule is amended, and nothing is added to any «حصرًا» list.**

What is genuinely missing is smaller and additive: **not one feature list reads
URL query parameters today** (verified in code — only the login screen does), so
filtered deep-linking needs wiring in each destination screen. That is
`DEC-DSH-006` — an **implementation dependency with no business rule involved**.

---

## 8 · The three collisions with Approved artifacts, and how each was handled

Your re-issued commission is the ruling. Each collision was recorded **in
place, with the superseded wording preserved verbatim** — the BR-CAT-020 /
BR-INV-066 precedent. **No identifier was deleted, renumbered or reused.**

1. **`DEC-IDN-007`** — «the first screen after sign-in is the product list, and
   **no dashboard is built**». **Superseded by `DEC-DSH-011`.** Its own stated
   basis was the inventory scope-lock, and your commission lifted it. The
   supersession was **propagated to the four other identity documents that state
   the landing screen** — leaving them silent would have left the module's docs
   contradicting each other. **Each is marked forward-looking, because the
   Dashboard is Draft and unimplemented, so `catalog/products` is still the
   truth today.** `TS-IDN-001/016` were **not rewritten** — they are Approved,
   pinned to real tests, and accurate until the board ships.
2. **`mvp.md`** — gained **§2a**, recording the commission and its exact extent:
   it lifts the dashboard exclusion **and nothing else** from the `DEC-SAL-001`
   scope lock, exactly as `DEC-SAL-010` lifted returns alone.
3. **Pilot Observation Mode** — prohibits new documentation «unless explicitly
   requested». Your commission **is** that request. Recorded rather than passed
   over.

---

## 9 · Four questions, none of them invented

ADR-0017 §5: what is missing is asked, not filled in.

| # | Question | What it blocks |
|---|---|---|
| **OQ-DSH-1** | **Reorder Level** — build it as a Catalog capability, or keep low stock as Future? | **That one item.** Nothing else |
| **OQ-DSH-2** | Should the «clinic-local date» definition move out of `inventory/business-rules.md` into a cross-cutting home, now that a Sales figure uses it? | **Nothing.** Recommendation: raise it later — the source is already single in code; this is a documentation-location question, not a behaviour one |
| **OQ-DSH-3** | The Arabic name — **«لوحة التشغيل»** proposed, deliberately **not** «لوحة المعلومات» (which names the very thing you excluded) — and the **nineteen** interface strings in `ui.md` §7 | **Implementation, not design.** No unapproved Arabic string gets written — the OQ-IDN-3 precedent |
| **OQ-DSH-4** | Does **draft sales** stay an attention item? | **Nothing.** Recommendation: keep it — it passes all three questions, and `BR-DSH-013` makes it cost nothing on a clean day, since a zero item is not displayed at all |

---

## 10 · One extension request, raised rather than invented

The UI Kit has **no card or tile component** (`badge · banner · button ·
checkbox · chip · dialog · drawer · empty-state · form-field · input · logo ·
pagination · popover · select · skeleton · table · theme`), and the board needs
both.

**§17 of the design language forbids a module patching that gap itself:**
«الانحراف البصريّ يبدأ دائمًا باستثناء صغير محلّيّ لمرّة واحدة» — visual drift
always begins with one small local exception. So **`vf-card` and
`vf-stat-tile`** are raised for `components.md`, to be registered **before** use
so they belong to the whole product rather than to this screen.

The mandatory §16 design checklist is **answered inside `ui.md` §10**, because
§16 makes answering it a condition of approving any UI document.

---

## 11 · Repository state

**Documentation only. Not one code file was touched.**

- **New:** `docs/modules/dashboard/` — **nine documents plus this report** (ten
  files). The nine are the module doc set; this report is a delivery artifact,
  not a tenth module document.
- **Amended in place:** `identity/decisions.md` · `identity/overview.md` ·
  `identity/ui.md` · `identity/workflow.md` · `identity/test-scenarios.md` ·
  `mvp.md` · `docs/modules/_INDEX.md` · `GLOSSARY.md` · `STATUS.md`.
- **Nothing committed. Nothing pushed. No implementation of any kind.**

**Still in flight from the previous cycle and untouched by this work:** the
cloud deploy has never been observed green. Its five-step resume list is at the
top of `STATUS.md` and remains the first task of any implementation session.

**Next step is yours:** approve or amend the module, and answer OQ-DSH-1..4.
Implementation begins only after that — and its first act will be to seek your
approval for the new read requirements in the three owning modules.
