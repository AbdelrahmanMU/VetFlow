# VetFlow — Validation & User Guidance UX Standard

> **Status: Approved — owner, 2026-07-31, "approved with modifications."**
> The twelve owner rulings of that approval are incorporated throughout and
> recorded in §0. Implementation follows the owner-ruled order (§0 ruling 12)
> and **may not begin before the owner confirms this document and the gap
> analysis are synchronized** (owner directive, 2026-07-31).
>
> Commissioned by the owner (2026-07-31) as the *Validation & User Guidance
> UX Standard* initiative. The objective is not validation for its own sake
> but **guiding the user to resolve every error with the minimum possible
> effort.**
>
> Home: `docs/ui/` — the ownership matrix (`docs/architecture/overview.md`)
> and the Boundary line in `standards/frontend-standards.md` assign error
> *presentation* to the design system. This document extends
> `design-language.md` (raised through its own §17 extension protocol) and is
> the binding validation/guidance chapter.
>
> IDs: this document owns the **`STD-UX-NNN`** range (block-per-section,
> decade-aligned, gaps deliberate). The range was renumbered once, in the
> revision that incorporated the owner rulings — before any ID was cited by
> code or an approved document; from this version on, IDs are stable and
> never renumbered. Language: engineering English per ADR-0002; every
> user-facing string is Arabic. Companion audit:
> `validation-gap-analysis.md`.
>
> Authority: Principles → ADRs (ADR-0007 · ADR-0015 · ADR-0018) → Standards
> (STD-API-010..014 · STD-BE-030..035 · STD-FE-030/036/037/040/043) → this
> document. It restates none of them; it binds what they leave open — *where,
> when, and how* an error reaches the user, and what the user does next.

**Defaults:** Scope = `UI/UX (all screens, current and future)` · Stability =
`Stable` · Class = `Mandatory` · Severity = `Error`.

**Severity policy and exception process:** ADR-0017 §7; exceptions only via
the Exception Register at the end of this document.

---

## 0 · Owner rulings (2026-07-31) — recorded and routed

| # | Ruling (owner's wording, condensed) | Incorporated in |
|---|---|---|
| 1 | Adopt **Progressive User Guidance**: hints before errors · validation only when appropriate · success state after correction. | STD-UX-009 · STD-UX-013/014 · STD-UX-068 |
| 2 | Define **three validation moments**: during typing · on field blur · on submit for business rules. | §3 (STD-UX-010/011/012) |
| 3 | **Field-related errors must never be shown only in a banner.** Banners are reserved for page-level or business-rule errors. | STD-UX-020 |
| 4 | Add a **Validation Summary component** for long forms with clickable navigation to each invalid field. | STD-UX-023 · §13 item 4 · STD-UX-129 |
| 5 | Introduce a **writing style guide** for validation messages — consistent wording system-wide. | §6 style guide · STD-UX-057 |
| 6 | **Server-side field validation errors are mapped back to their fields** whenever possible. | STD-UX-019 |
| 7 | Field validation messages **disappear immediately** after the field becomes valid. | STD-UX-015 |
| 8 | **Toast notifications must never be used for validation.** | STD-UX-044 |
| 9 | Tabs, accordions and dialogs containing invalid fields **automatically open and focus the first invalid field.** | STD-UX-074 · STD-UX-083 |
| 10 | **Accessibility requirements are mandatory** — `aria-invalid`, `aria-describedby`, appropriate alert semantics. | §10 (whole section, owner-ruled Mandatory/Error) |
| 11 | Add a **Validation Performance** section: validation frequency, debounce, avoiding unnecessary API calls. | §11 (new) |
| 12 | **Implementation order: Foundation → Shared reusable infrastructure → Module adoption.** | §13 closing note · gap analysis §5 |

Rulings 6, 7, 9 and 10 confirm rules the draft already carried; they are
marked as owner-ruled at their rows. The remaining open items the approval
did **not** rule on are listed in *Open items* at the end.

**Addendum (owner ruling, 2026-07-31 — refines ruling 12's adoption phase):**
implementation proceeds as **Phase 1 — Foundation** (the reusable
infrastructure of §13, proven on one–two representative screens only; no
module-wide adoption) → **owner review** → **Phase 2 — high-frequency
workflow adoption** (Product Editor · Purchase Invoice · Receive · Sale ·
Purchase Return · Sales Return) → **owner review** → **Phase 3 — complete
application adoption**. The objective is maximum reuse and minimum
duplication. Backend amendments AMD-1..6 stay deferred unless one becomes
strictly required to complete the approved frontend foundation.

**Second addendum (owner ruling, 2026-07-31 — Phase 1 approved; an Adoption
Test gates Phase 2):** Phase 1 is **approved**, with **one additional
validation step before Phase 2 begins**: a single **Adoption Test** — migrate
**one medium-complexity production screen** not yet on the foundation, under
four constraints: the Foundation is **frozen** (no modification unless a real
defect is discovered) · only the approved reusable infrastructure is reused ·
**no new validation components** · broad module adoption does **not** start.
The test produces an **adoption report** (lines removed/added · reuse
percentage · count of reused components/directives/services · any Foundation
defect · any insufficient Foundation API · a real-adoption-based effort
estimate for the remaining screens). **If the test succeeds without requiring
Foundation redesign, the Foundation becomes frozen (v1)** and Phase 2
proceeds on the existing infrastructure. The objective is to prove the
Foundation **minimizes future implementation effort**, not merely that it
functions correctly.

**Third addendum (owner ruling, 2026-07-31 — Adoption Test approved;
Foundation v1 frozen; the Validation UX Adoption Epic):** the Adoption Test
report is **approved**. **Validation Foundation v1 is frozen.** No further
Foundation change is allowed except for **verified defects**, **accessibility
fixes**, or **security issues**. Adoption proceeds as **one Epic — the
Validation UX Adoption Epic — under Continuous Capability Mode** (supersedes
the first addendum's Phase 2 → review → Phase 3 split): scope is **every
remaining production form, dialog, and workflow** under this standard, with
**no stop between modules and no intermediate reviews** unless a
governance-defined stop condition is reached. Completion delivers **one
consolidated Owner Report**: coverage metrics · before/after compliance by
module · remaining exceptions · remaining technical debt · reuse metrics ·
validation-UX compliance percentage · accessibility compliance summary ·
browser verification results · a final UX audit against this standard.

---

## 1 · Validation philosophy

An error is a **navigation problem**: the user is somewhere they did not
intend to be, and the system's job is to get them out in the fewest possible
moves. Every rule in this document serves that single goal. The hierarchy is:

```
Prevent  →  Guide before erring  →  Detect at the right moment
         →  Explain in business language  →  Hand the user the exit
```

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-001 | Every error answers three questions, always: **what happened · what is the state now · what to do next**. A message answering fewer than three is incomplete. | Mandatory | Error | Manual | Review + AC-UX-02 | Review | `design-language.md` §15 («الرفض يشرح ويقترح») |
| STD-UX-002 | **Prevention over correction**: constrain input at the source (pickers over free text, date bounds, per-unit steps) — but never by silently altering a business quantity. Clamping, rounding, and truncating user input are prohibited. | Mandatory | Error | Manual | Review | Review | BR-INV-061 · BR-INV-058 (rejected, never rounded) |
| STD-UX-003 | **User input is never lost.** Any failure — validation, business, system — leaves every entered value intact and editable, in place. | Mandatory | Error | Manual | Review + AC-UX-04 | Review | `product-editor-ux-architecture.md` §7 (UXD-ED-008, retry-in-place) |
| STD-UX-004 | **No silent failure.** Every mutation the user triggers reports success or failure explicitly. A failed action that changes nothing on screen is a defect of the highest UX severity. | Mandatory | Error | Semi-Automatic | AC-UX-01 · integration tests | Review | `inventory/ui.md` («الرفض — رسائل صريحة لا صامتة») |
| STD-UX-005 | Errors speak **business Arabic**. No error codes, HTTP statuses, stack traces, English fragments, or mechanism descriptions ever reach the user. | Mandatory | Error | Manual | Review | Review | `catalog/ui.md` §3 · `sales/ui.md` (mechanism meta-rule) |
| STD-UX-006 | **State guarantee**: when a rejected operation touches stock or money, the message states explicitly that nothing was saved — the canonical sentence is «لم يُحفظ أيّ تغيير.» | Mandatory | Error | Manual | Review + AC-UX-03 | Review | DEC-INV-023 · BR-INV-052/061/068 wording precedent |
| STD-UX-007 | **Same failure, same behavior, everywhere.** One error condition has one display surface, one timing, and one sentence pattern across all modules. Local variation requires a recorded ruling. | Mandatory | Error | Manual | Review + gap audits | Review | Principle: consistency over preference |
| STD-UX-008 | Validation is **layered, and the UI never assumes it has the last word**: the UI predicts (fast feedback), the API decides (VTF codes), the domain enforces (backstop). Client checks mirror documented rules only — a client-only rule the server does not enforce is an invented rule. | Mandatory | Error | Manual | Review | Review | STD-BE-010 · ADR-0018 |
| STD-UX-009 | **Progressive User Guidance** (owner ruling 1): the field guides before it judges — **hint before error** (STD-UX-013) · **validation only at the three ruled moments** (§3) · **success state after correction** (STD-UX-014). The escalation ladder is hint → error → success, never error-first. | Mandatory | Error | Semi-Automatic | `vf-form-field` (§13) | Review | Owner ruling 1 (2026-07-31) |

---

## 2 · Error categories

Every failure the user can see belongs to exactly one category. The category
decides the display surface, the copy owner, and the exit path.

| Cat | Name | Signature | Display surface | Exit path |
|---|---|---|---|---|
| **A** | **Field validation error** — the input itself is malformed or missing; the user can always fix it locally. | Client validator, or HTTP 400 `VTF-VAL-001` with the `errors` field dictionary | Inline, at the field (§3) — **never only a banner** (STD-UX-020) | Fix the field; error clears immediately (STD-UX-015) |
| **B** | **Business rule violation** — the input is well-formed but the operation is not allowed in the current state. | HTTP 400/409 with a `VTF-XXX-NNN` code (ADR-0018; 400 = malformed request, 409 = state conflict) | Operation-level banner adjacent to the action (§4) | The message proposes the concrete way out |
| **B1** | **Concurrency conflict** — sub-category of B; the state moved underneath the user. Always retryable. | `VTF-INV-056` · `VTF-INV-068` (409) | Same as B, **plus a retry affordance** (§4) | Retry the same action |
| **C** | **Advisory warning** — nothing is blocked; the system informs and the user decides. | No error response; a warning surface (e.g. duplicate-product check) | Comparison dialog / inline notice with two equal options | User chooses; never blocks (BR-CAT-042) |
| **D** | **System error** — unexpected technical failure: 500, network failure, unmapped code. | HTTP 500 (opaque, STD-API-013) · HTTP status 0 · unknown `VTF` code | Operation-level banner (mutations) or the error state (loads) (§5) | Retry in place, input preserved |
| **E** | **Not found / gone** — the target no longer exists or is not addressable. | HTTP 404 | A **distinct not-found state**, never the generic error state | A way back (link to the owning list) |

The four data-view states (loading · empty · error · success, STD-FE-030) are
unchanged by this table; categories D/E govern which state a failed *load*
renders and what it says.

---

## 3 · Field validation behavior (category A)

**The three validation moments (owner ruling 2).** Every field behavior in
the system is expressible in exactly these three moments — a screen that
validates at any other moment violates this standard:

```
Moment 1 — during typing   guidance: normalize, hint, clear fixed errors. Never judge unfinished input.
Moment 2 — on field blur   field-scoped rules run; the field may now show its error.
Moment 3 — on submit       the whole form validates; business rules go to the server.
```

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-010 | **Moment 1 — during typing**: no error appears for in-progress input. This moment does normalization (Arabic-Indic digits), live hints, and the immediate **clearing** of an already-shown error the instant the value becomes valid. Any reactive typing-time feedback is debounced (§11) and **never a server round-trip**. | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | Owner ruling 2 · `product-editor-ux-architecture.md` §6 |
| STD-UX-011 | **Moment 2 — on blur**: a touched field runs its own field-scoped checks and may show its own error, replacing its hint. Nothing else on the screen reacts. | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | Owner ruling 2 |
| STD-UX-012 | **Moment 3 — on submit**: the whole form validates — including hidden panes (§9) — every invalid field shows its error simultaneously, focus moves to the first invalid control (§8), and **business rules are validated here, by the server**, never at moments 1 or 2. | Mandatory | Error | Semi-Automatic | submit-guidance directive (§13) | Review | Owner ruling 2 |
| STD-UX-013 | **Hint before error** (owner ruling 1): a field whose expectation is not obvious from its label carries hint copy — format, example, or constraint — visible **before** any error, in the neutral hint style (§7). Hints guide; they never scold. | Mandatory | Error | Semi-Automatic | `vf-form-field` hint slot | Review | Owner ruling 1 |
| STD-UX-014 | **Success state after correction** (owner ruling 1): a field that displayed an error and then becomes valid swaps the error for the success confirmation (success style + icon, §7) — visible acknowledgment that the correction worked. Fields that never erred show no success chrome. | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | Owner ruling 1 |
| STD-UX-015 | **A field validation message disappears the moment the field becomes valid** — on the very input event that fixes it, not at blur, not at next submit (owner ruling 7). A stale error over a now-valid value is a lie. | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | Owner ruling 7 |
| STD-UX-016 | **The submit button is never disabled because the form is invalid.** A disabled button explains nothing. Submit stays enabled, and clicking it triggers STD-UX-012. Disabled is reserved for *in-flight* (`saving`) with visible progress. | Mandatory | Error | Semi-Automatic | Review + AC-UX-06 | Review | §17 AP-04 |
| STD-UX-017 | **One rule, one message.** Each validation rule has its own sentence; a single generic string covering distinct rules (required, max-length, range, cross-field) is prohibited. | Mandatory | Error | Manual | Review + message catalog | Review | adjustment screen precedent |
| STD-UX-018 | The canonical required-field message is **«هذا الحقل مطلوب.»** — already the approved standard string (`sales/ui.md`). Field-specific required copy only where a module `ui.md` rules it. | Mandatory | Error | Automatic | message catalog | CI | `sales/ui.md` («النصّ القياسيّ المعتمد») |
| STD-UX-019 | **Server field errors are mapped back to their fields whenever possible** (owner ruling 6): a 400 `VTF-VAL-001` response's `errors` dictionary is projected key-by-key onto the matching controls, with the same inline surface as client errors. Keys with no matching control render in the validation summary / form-level surface — **no key is ever dropped.** | Mandatory | Error | Semi-Automatic | projection helper (§13) | Review | Owner ruling 6 · STD-API-014 |
| STD-UX-020 | **A field-related error is never shown only in a banner** (owner ruling 3): it always renders at its field. **Banners are reserved for page-level and business-rule errors.** A summary may *accompany* field errors as navigation (STD-UX-023); it never replaces them. | Mandatory | Error | Manual | Review | Review | Owner ruling 3 |
| STD-UX-021 | Client-side validators exist **only for documented rules** (`BR-*`/`REQ-*` cited where declared) that the server also enforces. The client predicts; it never legislates. | Mandatory | Error | Manual | Review | Review | STD-UX-008 · No-Speculation (ADR-0017 §2) |
| STD-UX-022 | Quantity fields enforce their documented shape at entry: positive where the rule says positive, whole where the product is not splittable — with the rule's own message, before the server round-trip. | Mandatory | Error | Semi-Automatic | shared validators (§13) | Review | BR-SAL-004 · BR-PUR-005 |
| STD-UX-023 | **Validation Summary** (owner ruling 4): long forms — multi-section forms, or any form where an invalid field can sit outside the viewport — render the Validation Summary component on a rejected submit: **one entry per invalid field, each a link** that focuses and scrolls to its field, auto-opening its container (§9). A navigational map, not a pile of text («خريطة لا كومة نصوص»). Short single-view forms need no summary — the inline errors are already all visible. | Mandatory | Error | Semi-Automatic | `vf-validation-summary` (§13) | Review | Owner ruling 4 · `product-editor-ux-architecture.md` §6 |

---

## 4 · Business rule error behavior (category B / B1)

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-030 | Classification is by **`errorCode` only**, through the central registry (§12). Message-text parsing and bare status-code branching are prohibited (the 404 exception stands until §14 AMD-1 is ruled). | Mandatory | Error | Automatic | `ApiErrorClassifier` (§13) | CI | STD-FE-037 · STD-API-011 |
| STD-UX-031 | Business rejections display at **operation level, adjacent to the action that caused them** — the banner inside the same card or dialog, visible without scrolling when it appears (§8). Never in a global surface detached from the action. | Mandatory | Error | Manual | Review | Review | commit-dialog precedent |
| STD-UX-032 | The message follows STD-UX-001 in business terms: what was rejected · why (the rule, in the user's vocabulary) · the state guarantee where it applies (STD-UX-006) · the way out. «لا يمكن» alone is prohibited copy. | Mandatory | Error | Manual | Review | Review | `design-language.md` §15 rule 12 |
| STD-UX-033 | **Concurrency conflicts (B1) always offer a retry affordance**: the primary action relabels to «إعادة المحاولة» and re-issues the same operation. The message states the outcome, never the detection mechanism. | Mandatory | Error | Semi-Automatic | classifier `retryable` flag | Review | DEC-INV-023 · `sale-lines.store` precedent |
| STD-UX-034 | When the response carries `metadata` naming the offending object(s) — products, line, batch — the message **names them**. A metadata-less fallback wording exists for every such message. | Mandatory | Error | Semi-Automatic | message catalog (param slots) | Review | AC-SAL-009 precedent |
| STD-UX-035 | A rejection banner **clears when the user edits any input the violated rule depends on** — a `belowZero` banner must not survive the user changing the quantity. | Mandatory | Error | Semi-Automatic | store reset wiring | Review | adjustment-screen defect (gap audit) |
| STD-UX-036 | **Every `VTF` code an operation can return is mapped** in the registry before the screen ships. The generic fallback exists for the unexpected only — reaching it with a known code is a defect. | Mandatory | Error | Automatic | registry completeness check | CI | ADR-0018 §4 |
| STD-UX-037 | Irreversible operations (receive, commit) get the **same rejection fidelity as reversible ones or better** — never a single generic sentence for an operation that moves stock. | Mandatory | Error | Manual | Review + AC-UX-08 | Review | receive-dialog defect (gap audit) |

---

## 5 · System error behavior (category D / E)

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-040 | Unexpected mutation failure (500 · network · unmapped code) shows the generic technical-failure copy: what failed in plain terms, that the user's input is intact, and a retry action **in place**. No code, no traceId, no mechanism. | Mandatory | Error | Semi-Automatic | classifier fallback | Review | STD-API-013 · UXD-ED-008 |
| STD-UX-041 | Failed **loads** render the error state of the four states, with «إعادة المحاولة» (the existing `vf-empty-state` triplet). **A failed load may never degrade to an empty list** — an empty picker that actually failed is a silent lie. | Mandatory | Error | Semi-Automatic | store error channels | Review | STD-FE-030 · adjustment picker defect |
| STD-UX-042 | A multi-step client sequence behind one button is presented as **one action with one outcome**. If a step fails mid-sequence, the message states what was and was not saved — partial state is never left unexplained. | Mandatory | Error | Manual | Review | Review | returns 3-step sequence (gap audit) |
| STD-UX-043 | **404 renders a distinct not-found state**, not the generic error state, with a way back to the owning list — «فاتورة الشراء غير موجودة» pattern. Where a 404 deliberately means "nothing to do here" (returnable lines), the screen says that in business terms. | Mandatory | Error | Manual | Review | Review | `purchasing/ui.md` four-state block |
| STD-UX-044 | **Toast notifications are never used for validation** (owner ruling 8) — and, more broadly, **errors never auto-dismiss**: no timed surface for anything the user must act on. An error leaves the screen only because the user fixed it, retried, or navigated away. | Mandatory | Error | Manual | Review | Review | Owner ruling 8 · §17 AP-05 |
| STD-UX-045 | Success is explicit but never obstructive: a success banner (`role="status"`) or navigation-with-confirmation. Success feedback never interrupts the next task with a dialog. | Mandatory | Error | Manual | Review | Review | adjustment success precedent |

---

## 6 · Message writing guidelines and style guide

**Tone (binding):** مهنية، هادئة، مباشرة — بلا اعتذار، بلا لوم، بلا تهويل،
وبلا مصطلح تقني. (Adopted verbatim from `catalog/ui.md` §13.)

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-050 | Structure every message as **[ما حدث] ثم [الحالة الآن] ثم [المخرج]** — short complete sentences, each ending with a period. No exclamation marks, no ellipses. | Mandatory | Error | Manual | Review | Review | STD-UX-001 |
| STD-UX-051 | **Name the thing.** Use the field's label, the product's name, the document's number — never "بعض الحقول" or "أحد البنود" when the specific one is known. | Mandatory | Error | Manual | Review | Review | AC-SAL-009 |
| STD-UX-052 | Forbidden phrasings: «لا يمكن» with no reason and no exit · «غير صالح» alone · «خطأ في النظام» for a business rejection · any wording that blames the user · any English or technical term. | Mandatory | Error | Manual | Review | Review | `design-language.md` §15 |
| STD-UX-053 | Messages describe **outcomes, never mechanisms** — a concurrency message says the stock changed and nothing was saved; it never mentions how the conflict was detected. | Mandatory | Error | Manual | Review | Review | `sales/ui.md` meta-rule (DEC-INV-023) |
| STD-UX-054 | **One rule, one sentence pattern, repository-wide.** The same rule violated in different operations uses the same template with only the operation's verb substituted. Four unrelated wordings for one server code is a defect. | Mandatory | Error | Manual | message catalog review | Review | `VTF-INV-061` four-wordings finding |
| STD-UX-055 | All copy is Arabic, lives in the message catalog (§12), and its canonical terms follow `docs/shared/GLOSSARY.md`. New vocabulary this standard introduces joins the recorded GLOSSARY sync debt. | Mandatory | Error | Automatic | STD-FE-040 lint | CI | ADR-0007 · ADR-0002 |
| STD-UX-056 | Numbers, dates, and money inside messages go through the localization format service — never string-concatenated raw values. | Mandatory | Error | Semi-Automatic | Review | Review | STD-FE-042 |
| STD-UX-057 | **All validation and guidance copy conforms to the style guide below** (owner ruling 5). New copy that deviates fails review regardless of being otherwise correct. | Mandatory | Error | Manual | Review | Review | Owner ruling 5 |

### The writing style guide (owner ruling 5)

**Grammar and form:**

| Aspect | Rule | مثال |
|---|---|---|
| Voice for outcomes | Impersonal / passive — the system states facts, it does not accuse | «لم يُحفظ أيّ تغيير.» — not «أنت لم تحفظ» |
| Voice for the exit | Direct imperative, one concrete action | «راجع الكمّية ثم أعد التثبيت.» |
| Sentence length | Each part of [ما حدث · الحالة · المخرج] is one short sentence; a whole message ≤ 3 sentences | — |
| Punctuation | Every sentence ends with «.» — never «!»، never «…» | — |
| Hints | Nominal phrases or gentle imperative, no «يجب» | «مثال: 10» — not «يجب إدخال رقم» |
| Success confirmations | Past-tense fact, no celebration | «تمّ تثبيت المرتجع.» |
| Numbers & units | Through the format service; the unit is always named | «480 قرصًا» — not «480» |
| Technical terms | Prohibited — no HTTP, codes, JSON, حقول بالإنجليزية | — |

**Standard operation verbs** (one verb per operation, everywhere):

| Operation | الفعل القياسي |
|---|---|
| Save (draft/edit) | حفظ |
| Sale commit | إثبات |
| Receiving | استلام |
| Return | إرجاع |
| Adjustment (decrease) | خصم |
| Write-off | إهلاك |
| Retry | إعادة المحاولة |

**Right / wrong pairs:**

| ✗ | ✓ |
|---|---|
| «خطأ! بيانات غير صالحة.» | «هذا الحقل مطلوب.» |
| «لا يمكن إتمام العملية.» | «الكمّية المرتجَعة تتجاوز المتبقّي القابل للإرجاع. لم يُحفظ أيّ تغيير. راجع الكمّية ثم أعد التثبيت.» |
| «حدث خطأ 409.» | «تغيّر المخزون أثناء إتمام البيع. لم يُحفظ أيّ تغيير. حاول مرة أخرى.» |
| «فشل التحقق من الحقول المميّزة.» (ولا شيء مميّز) | كل حقل يحمل رسالته في مكانه + ملخّص تنقّل عند الطول |

**Canonical templates** (existing ruled strings marked ✓; the rest are the
approved patterns of this standard):

| Case | Template | Status |
|---|---|---|
| Required field | «هذا الحقل مطلوب.» | ✓ ruled (`sales/ui.md`) |
| Exceeds remaining | «الكمية المطلوب [خصمها/إهلاكها/إرجاعها] تتجاوز المتبقّي [في الدفعة/القابل للإرجاع]. لم يُنفَّذ أيّ تغيير.» | partly ruled (BR-INV-061 · BR-PUR-016 wordings) |
| Concurrency retry | «تغيّر[ت] [المخزون/الدفعة] أثناء [العملية]. لم يُحفظ أيّ تغيير. حاول مرة أخرى.» | ✓ ruled (DEC-INV-023 · BR-INV-068) |
| Insufficient stock | must name the products; must not say «الرصيد صفر» (DEC-INV-021) | **wording open — DEC-INV-022 (owner)** |
| Generic save failure | «تعذّر حفظ [الشيء]. لم يُحفظ أيّ تغيير — أعد المحاولة، وإن تكرّر الخطأ أبلغ المسؤول.» | approved with this standard |
| Load failure | Existing triplet: title + body + «إعادة المحاولة» | ✓ in use |
| Not found | «[الشيء] غير موجود[ة].» + link back | ✓ pattern in use |

---

## 7 · Visual behavior

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-060 | An invalid field shows **all three signals**: the invalid border style, the field's error icon, and the inline message under the field. Color is never the only signal. | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | `design-language.md` §14 |
| STD-UX-061 | Field error text renders **directly under its field**, inline-start aligned, in the danger token color — one consistent surface app-wide. | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | UI Kit pattern |
| STD-UX-062 | Operation-level messages use **one shared banner component** (`vf-banner`, §13) with tones error/success/warning — locally re-declared banner CSS is prohibited. | Mandatory | Error | Automatic | component + Stylelint | CI | ten copy-pasted `.banner` blocks (gap audit) |
| STD-UX-063 | All error/success/hint visuals use **existing design tokens only**. A token that does not exist fails silently — inventing one locally is prohibited; missing tokens are raised as design-system extension requests. | Mandatory | Error | Semi-Automatic | Stylelint + review | CI | C4 defect class · `design-language.md` §17 |
| STD-UX-064 | Placement: the operation banner sits **inside the card or dialog that owns the action, above the action area** — never at the top of the page detached from context, never behind a modal. | Mandatory | Error | Manual | Review | Review | STD-UX-031 |
| STD-UX-065 | Section/tab/accordion headers containing invalid fields show a **non-color-only indicator** (⚠ + count) — the section rail is a map, not a pile of text. | Mandatory | Error | Semi-Automatic | container components | Review | `product-editor-ux-architecture.md` §6 |
| STD-UX-066 | Warning surfaces (category C) are visually distinct from errors — a warning never wears the danger tone, and its two options carry equal visual weight («بلا زرٍّ مخيف»). | Mandatory | Error | Manual | Review | Review | BR-CAT-042 · DEC-CAT-027 |
| STD-UX-067 | The in-flight state is visible: the acting button shows progress and is disabled **only while saving** (STD-UX-016); nothing else on the screen locks. | Mandatory | Error | Semi-Automatic | `vf-button` | Review | STD-FE-036 |
| STD-UX-068 | **Hint and success styling** (owner ruling 1): hints render in the neutral muted style below the field; the success state uses the success token + icon. Hint, error, and success share one slot in `vf-form-field` — error replaces hint; success replaces error after correction. The swap must not shift the field under the user's cursor (reserve the space, §11). | Mandatory | Error | Semi-Automatic | `vf-form-field` | Review | Owner ruling 1 |

---

## 8 · Focus and scrolling behavior

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-070 | On a rejected submit with field errors, **focus moves to the first invalid control in DOM order** and it scrolls into view with headroom (`scroll-margin`), never hidden under a sticky header. | Mandatory | Error | Semi-Automatic | submit-guidance directive (§13) | Review | `product-editor-ux-architecture.md` §6 |
| STD-UX-071 | An operation-level error with no field target: **the banner itself receives focus** (`tabindex="-1"`) and is announced — the user's next keystroke starts from the explanation. | Mandatory | Error | Semi-Automatic | `vf-banner` | Review | a11y: the error must be reachable |
| STD-UX-072 | The focus ring is always visible and never removed. | Mandatory | Error | Automatic | Stylelint / review | CI | `design-language.md` §14 («لا تُزال أبدًا») |
| STD-UX-073 | Dialogs trap focus while open and **return focus to the opener on close** — including after a failed submit that kept the dialog open. | Mandatory | Error | Semi-Automatic | `vf-dialog` | Review | `design-language.md` §14 |
| STD-UX-074 | If the first invalid control lives inside a collapsed or hidden container — tab, accordion, section, or a dialog that must open — **the container opens automatically and the field receives focus** (owner ruling 9; §9). | Mandatory | Error | Semi-Automatic | container components | Review | Owner ruling 9 |
| STD-UX-075 | No other scroll-jacking: the screen scrolls only for STD-UX-070/071/076 — never to "helpfully" reposition on unrelated events. | Mandatory | Error | Manual | Review | Review | — |
| STD-UX-076 | **Validation-summary links move focus**: activating a summary entry (pointer or keyboard) focuses and scrolls to its field per STD-UX-070/074. The summary itself is keyboard-reachable. | Mandatory | Error | Semi-Automatic | `vf-validation-summary` | Review | Owner ruling 4 |

---

## 9 · Validation inside dialogs, tabs and accordions

*(Today no screen has tabs or accordions; these rules are forward-binding so
the first tabbed screen does not invent its own behavior.)*

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-080 | A dialog with a failed submit **stays open**, shows the error inside itself above its actions, and preserves every input. Success is what closes it. | Mandatory | Error | Manual | Review | Review | existing dialog behavior, made binding |
| STD-UX-081 | The outcome of a dialog-hosted mutation is reflected **in the page** after close (updated data + announcement, §10) — the user never wonders whether the dialog "worked". | Mandatory | Error | Manual | Review | Review | AC-UX-05 |
| STD-UX-082 | Errors are never rendered outside or behind an open modal. While a dialog is open, it is the only error surface. | Mandatory | Error | Manual | Review | Review | STD-UX-064 |
| STD-UX-083 | **Submit validates all panes, not the visible one** — hidden tabs/accordion sections validate too; **the pane containing the first invalid field opens automatically and that field receives focus** (owner ruling 9), and the pane's header carries the indicator (STD-UX-065) until clean. | Mandatory | Error | Semi-Automatic | container components | Review | Owner ruling 9 · `product-editor-ux-architecture.md` §6 |
| STD-UX-084 | Per-line editors inside a dialog (receive expiry dates) follow §3 exactly: per-line inline errors, focus to the first offending line — not one sentence for N lines. | Mandatory | Error | Manual | Review | Review | receive-dialog gap |

---

## 10 · Accessibility requirements

**Owner ruling 10: this entire section is Mandatory / Error** — an acceptance
condition, not a layer added later (`product-editor-ux-architecture.md` §12).

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-090 | Every invalid control carries `aria-invalid="true"` — **including selects and checkboxes**, not only text inputs. | Mandatory | Error | Automatic | UI Kit + a11y checks | CI | Owner ruling 10 · STD-FE-043 |
| STD-UX-091 | Every error message — and every hint — is **programmatically associated** with its control: the element has an `id`, the control points at it with `aria-describedby`. An error "hanging in the air" fails review. | Mandatory | Error | Automatic | `vf-form-field` + a11y checks | CI | Owner ruling 10 · `design-language.md` §14 |
| STD-UX-092 | Newly appearing errors use **appropriate alert semantics** (owner ruling 10): `role="alert"` on the error surface; screen-level outcomes (saved · rejected · loaded) additionally announce through the visually-hidden `aria-live="polite"` region, present on **every screen with a mutation or data view**. Success-after-correction announces politely, never as an alert. | Mandatory | Error | Semi-Automatic | shared announcer | Review | Owner ruling 10 |
| STD-UX-093 | Every field has exactly one programmatic label (`for`/`id` or `aria-labelledby`) — no duplicated `aria-label` double-announcing, no unlabeled checkboxes. | Mandatory | Error | Automatic | UI Kit + a11y checks | CI | `vf-checkbox` gap |
| STD-UX-094 | The whole error journey is keyboard-operable: reach the summary, reach the error, reach the retry, fix the field, resubmit — without a pointer. | Mandatory | Error | Semi-Automatic | a11y checks | CI | STD-FE-043 |
| STD-UX-095 | Danger/success/hint tokens meet contrast on their backgrounds; no information is carried by color alone (restates STD-UX-060 for non-field surfaces). | Mandatory | Error | Semi-Automatic | design tokens + review | Review | `design-language.md` §14 |
| STD-UX-096 | Messages render with correct `dir`/`lang`, and digits follow the format service — assistive tech reads the Arabic correctly. | Mandatory | Error | Semi-Automatic | review | Review | STD-FE-041/042 |

---

## 11 · Validation performance (owner ruling 11)

Validation must feel instant and cost nothing — neither the user's keystrokes
nor the network pay for it.

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-100 | **Validation work is scoped to its moment**: moment 1 touches only the active control; moment 2 the blurred control; **whole-form validation runs only at moment 3**. Re-validating the entire form on every keystroke is prohibited. | Mandatory | Error | Semi-Automatic | `vf-form-field` / directive | Review | Owner ruling 11 |
| STD-UX-101 | **Debounce**: any reactive typing-moment feedback — live hints, format checks, async advisory checks — waits for a typing pause (**default 300 ms**, adjusted only with a recorded reason) and a minimal input length before running. Nothing evaluates on every keystroke. | Mandatory | Error | Semi-Automatic | shared debounce utility (§13) | Review | Owner ruling 11 |
| STD-UX-102 | **No server call per keystroke, ever.** Server-backed advisory checks (e.g. the duplicate-name check) run at most on a debounced pause or on blur, **cancel any superseded in-flight request** (switch semantics), and cache per entered value for the life of the form. | Mandatory | Error | Automatic | shared async-check utility | CI | Owner ruling 11 |
| STD-UX-103 | **Submit is the only moment that calls mutation endpoints**, and validation never issues a redundant duplicate call with an identical payload. Client-side moments 1–2 are pure computation. | Mandatory | Error | Semi-Automatic | Review | Review | Owner ruling 11 |
| STD-UX-104 | Validation never blocks input: no synchronous heavy work in input handlers, and hint/error/success swaps must not reflow the field under the cursor (space reserved, STD-UX-068). | Mandatory | Error | Manual | Review | Review | Owner ruling 11 |

---

## 12 · Message catalog strategy

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-110 | **One frontend registry** maps every `VTF` code to its category, message key, metadata slots, and `retryable` flag. The five per-store `classify()` copies are replaced by it; a screen may narrow the registry, never fork it. | Mandatory | Error | Automatic | `ApiErrorClassifier` (§13) | CI | STD-FE-037 · gap audit |
| STD-UX-111 | Key naming: code-mapped copy lives under **`errors.<VTF-code>`**; a context override (**`errors.<VTF-code>.<context>`**) exists only where a module `ui.md` rules a contextual wording (e.g. the operation verb of `VTF-INV-061`). Shared field-rule copy lives under **`validation.*`**; hints under **`hint.*`**; screen-specific copy keeps its screen namespace. | Mandatory | Error | Automatic | catalog structure + typing | CI | existing `MessageKey` typing |
| STD-UX-112 | The backend's localized `detail` is **never rendered** (affirms STD-FE-037 practice); the frontend catalog is the single source of what the user sees. The backend resx and frontend catalog are two authored copies of ruled wording — **any wording change updates both**, checked in review. | Mandatory | Error | Manual | Review checklist | Review | ADR-0007 · duplication finding |
| STD-UX-113 | A missing key renders the key itself, visibly — never a blank — and is a defect. The registry has a completeness check: every code in the backend catalog has a frontend mapping. | Mandatory | Error | Automatic | CI check | CI | backend `ErrorMessages` fallback precedent |
| STD-UX-114 | **Module `ui.md` remains the approval home of user-facing copy** (documentation rule). The catalog implements approved copy; new copy — including hints — lands in the module's `ui.md` first, then the catalog. | Mandatory | Error | Manual | Review | Review | `.claude/rules/documentation.md` |
| STD-UX-115 | An unknown code at runtime falls back to the category-D generic copy and is logged — the user never sees the code. | Mandatory | Error | Automatic | classifier | CI | STD-UX-005 |

---

## 13 · Frontend architecture for reusable validation components

**Approved architecture — building it is the first two phases of the
owner-ruled implementation order.** These rules bind all new and refactored
screens once the pieces exist.

The pieces (all inside the UI Kit / core, registered in `docs/ui/components.md`):

1. **`vf-form-field`** — the single field wrapper: label (`for`/`id`),
   control slot, and the **hint → error → success** slot (STD-UX-009/013/014/
   015/068). Owns the aria wiring (`aria-describedby`, `aria-invalid`,
   STD-UX-090/091/093) and the moment-1/2 timing, so no screen re-implements
   them. All `vf-*` inputs render inside it.
2. **`vf-banner`** — the single operation-message component: tones
   error/success/warning, focusable (`tabindex="-1"`), `role="alert"`/`"status"`
   built in, standard tokens. Owns STD-UX-062/071.
3. **Submit-guidance directive** (`vfSubmitGuide` on the form): on submit —
   mark all touched, run moment 3 (STD-UX-012), focus/scroll first invalid
   (STD-UX-070), open containing pane (STD-UX-074), show the summary when the
   form qualifies (STD-UX-023). One implementation, zero per-screen code.
4. **`vf-validation-summary`** (owner ruling 4) — the long-form summary:
   one linked entry per invalid field, keyboard-operable, wired to the
   focus/auto-open behavior (STD-UX-076).
5. **`ApiErrorClassifier` + `VTF_ERROR_REGISTRY`** — `ApiError → { category,
   code, messageKey, params (from metadata), retryable }`. The registry is
   the §12 catalog; stores consume the classification, never re-derive it.
6. **Server-error projection helper** — maps a `VTF-VAL-001` `errors`
   dictionary onto a typed `FormGroup` (STD-UX-019), returning unmatched keys
   for the summary surface.
7. **Shared validators + async-check utilities** — the documented-rule
   library (§3): positive quantity, whole-if-not-splittable, max-length with
   message, required with the canonical message; plus the debounced,
   cancelling, cached async-check helper (STD-UX-101/102).
8. **UI Kit repairs** — `vf-date-input` becomes a real ControlValueAccessor
   (blur/touched work); `vf-select` gains `aria-invalid`; `vf-checkbox` gains
   label association and an error channel.
9. **One forms regime** — reactive typed forms for every form this initiative
   touches; the template-driven and raw-signal regimes retire as screens are
   brought into compliance (no big-bang rewrite).

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-UX-120 | Every form field on a compliant screen renders through `vf-form-field`; hand-wired label/hint/error markup is prohibited. | Mandatory | Error | Automatic | ESLint/review | CI | items 1, 8 |
| STD-UX-121 | Every operation-level message renders through `vf-banner`; local banner markup/CSS is prohibited. | Mandatory | Error | Automatic | Stylelint/review | CI | item 2 |
| STD-UX-122 | Every submitting form carries the submit-guidance directive; per-screen focus code is prohibited. | Mandatory | Error | Semi-Automatic | review | Review | item 3 |
| STD-UX-123 | Every API failure passes through `ApiErrorClassifier`; per-store code lists are prohibited. | Mandatory | Error | Automatic | ESLint/review | CI | item 5 |
| STD-UX-124 | Server field errors go through the projection helper (STD-UX-019). | Mandatory | Error | Semi-Automatic | review | Review | item 6 |
| STD-UX-125 | Client validators and async checks come from the shared library and cite their rule IDs. | Mandatory | Error | Manual | review | Review | item 7 |
| STD-UX-126 | New/refactored forms are reactive typed forms. | Mandatory | Error | Manual | review | Review | item 9 |
| STD-UX-127 | UI Kit additions/changes from this list are registered in `docs/ui/components.md` like every other Kit component. | Mandatory | Error | Manual | review | Review | ADR-0012 |
| STD-UX-128 | No new library is introduced for any of this without owner approval — the pieces above are Angular + the existing Kit only. | Mandatory | Error | Manual | review | Review | ai-governance «Never» list |
| STD-UX-129 | Qualifying long forms (STD-UX-023) use `vf-validation-summary`; ad-hoc error lists are prohibited. | Mandatory | Error | Semi-Automatic | review | Review | Owner ruling 4 |

**Implementation order (owner ruling 12, binding):**

```
Phase 1 — Foundation:            UI Kit repairs (item 8) · tokens · a11y primitives
Phase 2 — Shared infrastructure: items 1–7 · catalog re-keying (§12) · announcer
Phase 3 — Module adoption:       screen-by-screen compliance, per the gap analysis
```

---

## 14 · Backend validation response conventions

**Affirmed unchanged** (this standard adds no backend shape): RFC 9457
everywhere (STD-API-010) · `errorCode` from the Error Catalog, clients branch
on it only (STD-API-011) · single translation point in middleware
(STD-API-012) · opaque 500 + `traceId` (STD-API-013) · fixed `errors`
dictionary for validation (STD-API-014) · codes `VTF-<MODULE>-NNN`, one per
rule (STD-BE-032/033) · localized resources per language (STD-BE-034,
ADR-0007) · `metadata` is data, never UI copy (ADR-0018) · 400 = malformed
request, 409 = state conflict (the documented Error Catalog convention).

**Proposed amendments — NOT ruled by the 2026-07-31 approval; each still
needs its own owner decision.** Each lands in its own home (api-standards /
backend-standards / an ADR-0018 amendment) if approved:

| # | Proposal | Evidence | Home if approved |
|---|---|---|---|
| AMD-1 | Entity-missing 404s carry an `errorCode` and entity-flavored `detail` («فاتورة الشراء غير موجودة»), distinct from route-404s — today all 29 fall through to «المسار المطلوب غير موجود» with no code, forcing the status-code branching STD-FE-037 forbids. | backend audit §5(1) | api-standards + ADR-0018 |
| AMD-2 | Category/Manufacturer duplicate-name becomes a dedicated business code instead of the `VTF-VAL-001` + `errors.name` heuristic — one rule, one code (STD-BE-033's own spirit). | backend audit §5(4) | backend-standards + Error Catalog |
| AMD-3 | Nested collection field keys are fully camelCased (`units[0].quantityInNextUnit`). | backend audit §5(7) | api-standards |
| AMD-4 | An architecture test pins that every command/query with a validator class has it **registered** — the C5 unregistered-validator defect class, currently 12 handlers unvalidated with nothing preventing recurrence. | backend audit §5(2) | backend-standards (STD-BE-06x) |
| AMD-5 | A CI/test assertion pins constants ⊆ ErrorCatalog ⊆ resx (both languages) — closing the fail-open gaps in `ErrorCatalog.Get`/`ErrorMessages.Get`. | backend audit §5(3) | backend-standards |
| AMD-6 | The deliberate business-rule-as-404 overloads (`returnable-lines` covering both "missing" and "not committed/received") get an explicit ruling: keep the documented 404, or return the coded 409 the rule owns. | backend audit §5(1) | module decisions + ADR-0018 |

---

## 15 · Acceptance criteria

A screen **complies with this standard** when all of the following hold.
These are the audit dimensions the gap analysis uses.

| AC | Criterion |
|---|---|
| AC-UX-01 | Every mutation the screen offers has an explicit visible outcome — success and failure. No silent path exists (including toggles, row deletes, and picker loads). |
| AC-UX-02 | Every rejection message answers all three: what happened · state now · way out (STD-UX-001), in catalog Arabic conforming to the §6 style guide. |
| AC-UX-03 | Every atomic stock/money rejection carries the state guarantee sentence (STD-UX-006). |
| AC-UX-04 | No failure path loses or resets user input (STD-UX-003). |
| AC-UX-05 | Field validation follows the three moments exactly (§3): guidance only while typing · field rules on blur · full form + business rules on submit · per-rule messages · hints before errors · success state after correction · errors disappear the moment the field is valid · server `errors` mapped back to fields. |
| AC-UX-06 | Submit is enabled when invalid and guides on click; disabled only while saving, with progress (STD-UX-016/067). |
| AC-UX-07 | Every `VTF` code the screen's operations can return is mapped in the registry; the fallback is unreachable for known codes (STD-UX-036). |
| AC-UX-08 | Business rejections render adjacent to their action, persist until resolved, clear on relevant edit, and concurrency offers retry (§4). No field-related error appears only in a banner (STD-UX-020). |
| AC-UX-09 | On rejected submit, focus lands on the first invalid control (or the banner), scrolled into view; dialogs trap and restore focus (§8). |
| AC-UX-10 | Dialog failures keep the dialog open with the error inside it; hidden panes validate; the pane holding the first invalid field auto-opens and the field receives focus (§9). |
| AC-UX-11 | `aria-invalid` on every invalid control · `aria-describedby` links every error and hint to its field · outcomes are announced with appropriate alert semantics via the screen's live region · every field has one programmatic label (§10 — owner-ruled mandatory). |
| AC-UX-12 | All error/success/hint visuals come from `vf-form-field`/`vf-banner` with design tokens only — no local banner CSS, no invented tokens (§7, §13). |
| AC-UX-13 | Data views have the four states; failed loads show the error state with retry, never an empty state (STD-UX-041). |
| AC-UX-14 | 404 renders the distinct not-found state with a way back (STD-UX-043). |
| AC-UX-15 | All copy lives in the message catalog under §12 naming, matching the module's `ui.md`; no hardcoded strings (STD-FE-040). |
| AC-UX-16 | The full error journey is keyboard-operable, including the validation summary (STD-UX-094). |
| AC-UX-17 | Qualifying long forms render the clickable Validation Summary on rejected submit; each entry navigates to its field (STD-UX-023/076). |
| AC-UX-18 | No toast is used for validation or any error surface; nothing the user must act on auto-dismisses (STD-UX-044). |
| AC-UX-19 | Typing-moment feedback is debounced; no server call per keystroke; async checks cancel and cache; submit is the only mutation call (§11). |

---

## 16 · UX examples

Worked behavior sequences showing the standard end-to-end. Copy shown is
canonical where ruled, style-guide-conformant otherwise.

**EX-1 — Required fields on save (product editor, a long form).** The user
saves with the Arabic name and category empty. Nothing was flagged while
typing. On save: both fields flag simultaneously — border + icon + «هذا
الحقل مطلوب.» under each — the **Validation Summary** appears listing both
fields as links, the section rail marks the section ⚠, focus lands in the
name field, scrolled clear of the header, the announcer reads the rejection.
The user types one character — the name's error disappears at once and the
field shows the success mark; the summary entry drops out. Save stays
enabled throughout.

**EX-2 — Progressive guidance on one field (conversion factor).** From the
start the field carries the hint «عدد الوحدات الأصغر داخل هذه الوحدة —
مثال: 10». The user types `0`; nothing judges the unfinished input. On blur,
the error replaces the hint: «يجب أن يكون معامل التحويل أكبر من صفر.» The
user corrects to `10` — the error disappears on that keystroke and the
success mark appears. No banner, no toast, no server call.

**EX-3 — Over-return rejection (`VTF-SAL-016`, category B).** The user
returns 99 against a remaining 7 and commits. The screen keeps every entered
quantity. The banner appears inside the return card, above the actions,
focused and announced: «الكمّية المرتجَعة تتجاوز المتبقّي القابل للإرجاع.
لم يُحفظ أيّ تغيير. راجع الكمّية في البند المحدَّد ثم أعد التثبيت.» The user
edits the quantity — the banner clears (STD-UX-035).

**EX-4 — Concurrency on sale commit (`VTF-INV-056`, category B1).** The
commit dialog stays open, the error renders inside it: «تغيّر المخزون أثناء
إتمام البيع. لم يُحفظ أيّ تغيير. حاول مرة أخرى.» The primary button now reads
«إعادة المحاولة» and re-issues the commit. (Today's commit dialog — the
reference implementation this standard generalizes.)

**EX-5 — Server field error (duplicate category name, category A).** The
save returns 400 `VTF-VAL-001` with `errors.name`. The dialog stays open; the
message renders inline under the name field itself — «يوجد تصنيف بهذا الاسم
بالفعل.» — with `aria-describedby` linking it. Typing clears it. The banner
carries nothing: this is a field error (STD-UX-020).

**EX-6 — Receive dialog, missing expiry (§9).** Two of five lines require an
expiry date. Confirm flags **each offending line inline** («هذا الحقل
مطلوب.»), and focus moves to the first offending line's date field. A server
rejection (already received · concurrency) renders as its own mapped message
inside the dialog — never one generic sentence for every cause.

**EX-7 — Network failure on save (category D).** The API is unreachable. The
form keeps all input; the banner reads the generic technical copy with
«إعادة المحاولة» in place; retry re-submits the same payload. No code, no
jargon, no lost work.

---

## 17 · Anti-patterns — prohibited, each observed in the current codebase or newly barred by ruling

| AP | Anti-pattern | Replaced by |
|---|---|---|
| AP-01 | **Silent failure** — a toggle, row delete, or picker load that fails and shows nothing. | STD-UX-004/041 |
| AP-02 | **One message for every rule** — a single «مطلوب» string covering required, max-length, and cross-field violations alike. | STD-UX-017 |
| AP-03 | **The lying banner** — "راجع الحقول المميّزة" while no field is actually highlighted. | STD-UX-019/020 |
| AP-04 | **Disabled submit as the only explanation** — a dead button with no reason. | STD-UX-016 |
| AP-05 | **Toasts for validation, or any auto-dismissing error.** | STD-UX-044 |
| AP-06 | **Color-only signaling** — an invalid state carried by border color alone. | STD-UX-060 |
| AP-07 | **Technical language in the UI** — codes, statuses, mechanism talk. | STD-UX-005/053 |
| AP-08 | **Clamping or rounding user quantities** to make them fit. | STD-UX-002 |
| AP-09 | **Branching on message text or bare status codes** instead of `errorCode`. | STD-UX-030 |
| AP-10 | **Copy-pasted local infrastructure** — per-store `classify()` maps, per-component banner CSS. | STD-UX-110/062 |
| AP-11 | **Rendering the backend's `detail` text** (couples UI copy to Accept-Language negotiation). | STD-UX-112 |
| AP-12 | **Errors behind or outside an open modal.** | STD-UX-082 |
| AP-13 | **Dead-end error states** — an error with no retry, no way back, no next step. | STD-UX-001/041/043 |
| AP-14 | **Stale errors** — a rejection banner surviving the edit that addresses it; a field error surviving its fix. | STD-UX-015/035 |
| AP-15 | **Generic rejection for irreversible operations** — one sentence for every possible receive/commit failure. | STD-UX-037 |
| AP-16 | **Cross-context key borrowing** — one screen rendering another screen's message keys because the wording "fits". | STD-UX-111 |
| AP-17 | **Premature errors** — judging input while the user is still typing it (error on first keystroke, error on an untouched field). | STD-UX-010 (§3) |
| AP-18 | **Field errors surfaced only in a banner** — the banner points nowhere; the field stays unmarked. | STD-UX-020 |
| AP-19 | **Per-keystroke server validation** — network calls riding the input events. | STD-UX-102 |
| AP-20 | **A summary without navigation** — a wall of error text with no links to the fields. | STD-UX-023/076 |

---

## Open items after the 2026-07-31 approval

Resolved by the approval: the `STD-UX` range and its `docs/ui/` home · the
document's language · the implementation order (ruling 12) · implementation
is authorized once the owner confirms both documents are synchronized.

Still open, each needing its own ruling:

1. **The six backend amendments (§14 AMD-1..6)** — not covered by the
   approval; each needs a separate decision before its backend work exists.
2. **DEC-INV-022** (insufficient-stock wording) — the one canonical template
   this standard cannot finish.
3. **Calendar scheduling vs the Pilot**: the ruling «no new features during
   the Pilot unless required to keep the system operational» stands. The
   implementation *order* is ruled; **when** Phase 1 starts relative to
   UAT/Pilot remains the owner's scheduling call.
4. **GLOSSARY debt**: the new Arabic vocabulary of §6 (hint/success/summary
   vocabulary and the operation-verb table) joins the recorded sync debt.

## Exception Register

| STD | Scope of exception | Reason | Approved by | Date |
|---|---|---|---|---|
| — | *(none)* | | | |

## Tombstones

| STD | Removed | Reason |
|---|---|---|
| — | *(none)* | |
