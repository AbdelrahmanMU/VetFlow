# Dashboard — Epic Owner Report (implementation)

> Status: **Report (2026-08-03)** — the implementation the owner authorised on 2026-08-03.
> **All gates green. Nothing committed, nothing pushed — awaiting Epic Commit Approval.**
> The design-phase report is [`owner-report.md`](owner-report.md); this one covers what was
> built.

---

## 1 · Your four rulings, and where each one landed

| Ruling | What was done |
|---|---|
| **OQ-DSH-1** — low stock stays blocked; invent no threshold; build the rest | **No threshold exists anywhere in the code.** An integration test asserts the dashboard payload contains neither `lowStock` nor `reorder` — a placeholder would show up as a zero, so the guard is the *absence* of the concept, not a comment. The other seven capabilities shipped |
| **OQ-DSH-2** — move «today» to a shared cross-cutting home | **`docs/architecture/cross-cutting/clinic-date.md` created** and is now the definition's home. **BR-INV-059/060 keep their identifiers and their text**, amended in place to point there. **No code behaviour changed** — `IClinicClock` was already product-wide infrastructure; the documentation now matches what the code already did |
| **OQ-DSH-3** — approved; the name is «لوحة التشغيل» | Used verbatim in the UI, the glossary and every document. **Two strings the approved nineteen did not cover** are flagged in `ar.ts` rather than passed off as approved — see §6 |
| **OQ-DSH-4** — keep draft sales | Kept. REQ-DSH-006 / BR-DSH-008 unchanged |

Plus the two rulings in the same message: **`vf-card` and `vf-stat-tile` are in the UI Kit**
and registered in `components.md` before use; and **the read-composition rule is preserved** —
enforced now by a build-breaking architecture test rather than by a comment (§3).

## 2 · What was built

**Backend** — `GET /api/v1/dashboard`, one request, seven sections, each with its own status.
Over **three module-owned reads**, not one dashboard-owned query:

| Read | Owner | Facts |
|---|---|---|
| `REQ-INV-013` | Inventory | expired · expiring-soon · out-of-stock · last five movements |
| `REQ-SAL-006` | Sales | draft count · today's count · today's total |
| `REQ-PUR-007` | Purchasing | draft count |

**Frontend** — `/dashboard`, now the landing route; the attention band, today's sales, recent
movements, and the quick actions with **one** primary. Plus the piece that was genuinely
missing: **the four destination screens now read their filter from the URL**, so a tile lands
on exactly the rows it counted.

## 3 · The architectural rule you asked me to preserve — now enforced, not promised

You ruled: «the Dashboard is a read-composition module only and must never duplicate business
logic or compute domain rules.»

**That is now structural.** `VetFlow.Application.Dashboard` contains a parameterless query and
a DTO of primitives — nothing else — and the composition lives in Infrastructure, which is the
sanctioned cross-module read path (ADR-0014 §2). **«Dashboard» was added to the module
isolation test**, so the first person who imports an Inventory type "just to reuse the enum"
**breaks the build**. There is not one `Where`, `Count` or comparison in the composer.

A consequence worth naming: the movement type crosses to the dashboard **as a string**.
Re-declaring Inventory's closed vocabulary (BR-INV-065) in the Dashboard would have put a
second copy of an Inventory rule where it does not belong, and the two would have drifted the
first time a type was added.

## 4 · Three findings from implementation, none of them silent

**⛔ (1) The landing route was hardcoded in two places outside the route table — a real
defect, found by the browser and by nothing else.** After the route table was changed to
`/dashboard`, signing in still landed on the product list: the destination was also written by
hand in `login-page.component.ts` and in `anonymousOnlyGuard`. **The two unit tests covering
those paths were green the whole time — because they asserted the old destination.** Fixed in
all four places (two sources, two tests). Recorded as **DEC-DSH-016**.

**⛔ (2) The primary action was 39 px tall on the phone.** `vf-button` enforced the 44 px floor
on the full-width variant only. **Fixed in the UI Kit, not on the dashboard** — every button
now clears 44 px at ≤768 px, desktop untouched. Handling it locally would have been exactly
the "one small local exception" §17 warns about, and would have left the same gap on every
other screen. Recorded as **DEC-DSH-017**. *(The nineteenth cycle found this identical defect,
also in the browser — which is the argument for running it.)*

**(3) Three reads, not seven — with the trade-off written down.** One read per owning module
rather than one per tile. The cost: **the four inventory sections share a source, so they fail
together.** The contract still holds — each section reports its own status and none is ever
rendered as zero — but you should know the failure granularity is three, not seven. Recorded
as **DEC-DSH-014**.

**⛔ (4) Found in self-review: an Approved rule promised something the screen did not have.**
`BR-DSH-015` and `workflow.md` §4 rule that the board refreshes **«بطلب صريح من المستخدم»** —
an explicit user act — and `dashboard.refresh` («تحديث») was among your approved nineteen
strings. **But no refresh control existed.** The only routes to a reload were the *retry*
buttons, which appear only on failure, so on a healthy board there was **no way to refresh at
all** — a documentation-versus-code contradiction, which the governance rules list under
*Never*. **A quiet «تحديث» action now sits in the header** (quiet, because §15.1 allows one
primary and it is «بيع جديد»), the approved string is finally used, and `ui.md` §2 records it.

**(5) The failure path had never executed anywhere.** Every integration test ran against a
healthy stack, so all seven sections came back `ok` and the `failed` branch was never reached
in any test or in the browser. Given that the nineteenth cycle's `401` defect survived a full
browser pass for exactly this reason — a code path that only runs under conditions the
verification never created — **a dedicated test now forces a module to fail**
(`DashboardSectionFailureTests`): one owning read is replaced with a throwing stub, and the
test asserts the board still returns `200`, that the broken section reports `failed`, that the
other six stay `ok`, and that **the failed section carries no `count` key at all**.
**Mutation-checked:** making the failed factory emit `Count = 0` turns the test red.

## 5 · Gates — all green

| Gate | Result |
|---|---|
| Backend build | **0 warnings / 0 errors** |
| `dotnet format` | **clean** (it caught the recorded CRLF hazard again; files were normalised) |
| Domain tests | **163** |
| Architecture tests | **140** — including the new Dashboard isolation assertion |
| Integration tests | **292** (+18) — including three that force a section to fail |
| ESLint / Stylelint | **clean** |
| Frontend unit tests | **345** (+13) |
| `ng build` | **exit 0** — 603.43 kB (+2.79 kB; the budget warning is the pre-existing TD-107) |
| **Live browser** | **22/22** at 1440×900 and 390×844, **zero console errors** |

> **The browser suite was re-run after the «تحديث» control was added**, and grew by two
> checks for it: the control exists on a **healthy** board (not only on failure), and pressing
> it **re-reads the board** — asserted from the network log as a **second**
> `/api/v1/dashboard` request, so it is a real refresh and not a page reload.
>
> **The touch-target check was widened at the same time, and this is worth stating plainly:**
> it had been asserting a *chosen few* elements, which would have let the new quiet header
> actions through unmeasured — exactly the class of gap that produced the 39 px defect. It now
> measures **every interactive element on the board** (6 at 390 px) and reports any that fall
> short by name.

**The browser run went against the real stack** — the dev PostgreSQL on 5434, the API on 5080,
and a **fresh** dev server on 4500 because **:4200 held a stale one** (the trap that produced a
bogus report in an earlier cycle). It verified, among others: a signed-out visit cannot reach
the board; the landing route; **exactly one `/api/v1/dashboard` request** in the network log;
zero-count tiles absent; the draft-purchase tile reading **3** from the real database; the tile
linking to `/purchases?status=draft`; **the destination showing exactly 3 rows with the filter
chip visible**; no charts; exactly one money figure; the empty-ledger empty state; and the
44 px targets.

**Population parity was also checked against the live database**, not only in tests: the
dashboard's counts equal each destination screen's `totalCount` for all six filtered links.

## 6 · Two strings I did not invent — raised as OQ-DSH-5, and now ruled

The original nineteen (OQ-DSH-3) covered the board's content. Two **structural** labels were
not in that set: §12 requires an action label beside an icon (a tile without one is an
icon-only affordance), and §6 makes an empty state mandatory for any data view.

Rather than invent them or leave blanks, both shipped **marked in `ar.ts` as unapproved** and
were raised as **OQ-DSH-5**. **You ruled them on 2026-08-06, and in both cases chose different
wording than I proposed:**

| Key | I proposed | **You ruled** |
|---|---|---|
| `dashboard.tile.open` | ~~«عرض»~~ | **«فتح»** |
| `dashboard.recentMovements.empty` | ~~«لا توجد حركات بعد»~~ | **«لا توجد حركات مسجلة حتى الآن.»** |

Applied verbatim. **The module now has twenty-one approved strings and no open question.**
That both proposals were overridden is the argument for flagging rather than assuming: a
proposal does not become approved by being shown.

## 7 · What is deliberately not built

- **Low stock** — blocked by your ruling. No threshold, no placeholder, no filter added to
  BR-INV-014's «حصرًا» list.
- **No charts, no trends, no comparisons** — BR-DSH-017; `vf-stat-tile` has no trend affordance
  at all, and a test asserts it.
- **No auto-refresh or polling** — BR-DSH-015; an integration test holds the board open and
  asserts no further request.
- **No business action on the board** — a POST to `/api/v1/dashboard` returns 405, and every
  interactive element navigates.

## 8 · Repository state

**Nothing committed. Nothing pushed.** The working tree carries the nine new backend/frontend
source areas, the four destination-screen changes, the two UI Kit components, the documentation
updates for your rulings, and this report.

**Still outstanding from the nineteenth cycle and untouched:** the cloud deploy has never been
observed green. Its five-step resume list is at the top of `STATUS.md`.

**Awaiting your Epic Commit Approval.**
