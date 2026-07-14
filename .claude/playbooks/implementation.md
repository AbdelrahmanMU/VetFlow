# Playbook: Implementation

> **Status: Draft.** The **only** implementation playbook. Every implementation
> session starts here.
>
> This is an **execution orchestrator**, not governance. It decides *what to
> load, when, how to validate, and when to stop* — nothing more. It never
> restates a principle, an ADR, or a standard; it points at them.
>
> Authority: Principles → ADRs → Standards → **this playbook** (lowest). If it
> ever contradicts a higher document, the higher document wins and this file is
> the defect.

## Execution pipeline — no step may be skipped

```
Definition of Ready → Context Loading → Implementation → Self Review
      → Validation → Commit Gate → Push Gate → STOP
```

**Step 0 — Definition of Ready** (ADR-0017 §5). If the module's docs are not
Approved, or the slice cannot name its `REQ-`/`BR-`/`AC-` IDs: **STOP and ask
the owner.** Never fill a gap by inventing (No Speculation, ADR-0017 §2).

## Context loader — deterministic, staged, budgeted

Load stages **in order**. Load nothing a stage does not name.

| Stage | Load | Condition |
|---|---|---|
| **1 — always** | `CLAUDE.md` · `.claude/rules/ai-governance.md` · `docs/architecture/principles.md` · **the current module's documentation** — its `overview.md` plus the documents the slice actually names (`business-rules`, `requirements`, `acceptance`, `workflow`, `ui`, `decisions`, `test-scenarios`). A full module doc set runs ~20k tokens; load the whole set only when the whole set is in scope (New Module) | Always. **Nothing else.** |
| **2 — by work type** | Backend → `standards/backend-standards.md` + `standards/csharp-coding-standards.md` · Frontend → `standards/frontend-standards.md` · API → `standards/api-standards.md` · Database → `standards/backend-standards.md` + ADR-0019 | Only the types this task actually touches. Otherwise skip. |
| **3 — architectural change only** | The **specific** ADRs the change touches | **Never load all ADRs.** No architectural change → load none. |
| **4 — module docs** | Only the target module's documents | **Never load an unrelated module.** Another module's `Contracts` only if the task crosses the boundary. |
| **5 — budget check** | — | Over budget → **STOP and split the task.** Never widen context automatically. |

**Context budget — mandatory:**

| Size | Budget | Typical task |
|---|---|---|
| Small | **≤ 25k tokens** | Bug fix · single endpoint · one component |
| Medium | **≤ 60k tokens** | Feature slice · new aggregate · page + API |
| Large | **≤ 120k tokens** | New module scaffold |
| **> 120k** | **STOP** | Split into multiple implementation sessions |

If mandatory context alone exceeds the budget, the **task** is too big — not the
budget. Split it. If a needed fact is not in the loaded context, **ask the
owner**; do not go hunting.

**Never** load: all ADRs · all standards · unrelated modules · discovery
transcripts · `docs/business/` during implementation · superseded or annulled
artifacts. (Forbidden-by-default: loading one requires stating the reason
in-session — ADR-0017 §1.)

## Execution modes

Modes differ **only** in context, validation, and stop conditions. The pipeline
above is identical for all of them.

| Mode | Context (beyond Stage 1) | Validation | Stop conditions |
|---|---|---|---|
| **New Module** | Stages 2–4 for every layer the module needs; the module's full doc set | Full gate + module scaffolded per ADR-0014 boundaries; `Contracts` namespace exists; row added to `docs/modules/_INDEX.md` | Module docs not Approved · no owner-approved module name in `GLOSSARY.md` · budget > Large |
| **New Feature** | Stage 2 (backend + frontend + API as touched); target module docs | Full gate + every `BR-*` in the slice has a test naming its ID | DoR fails · slice spans more than one module · budget > Medium |
| **API Only** | `api-standards.md`, `backend-standards.md`; module docs | Full gate + contract tests + error codes registered | Endpoint has no `REQ-*` · resource term missing from `GLOSSARY.md` |
| **Frontend Page** | `frontend-standards.md`; module `ui.md` | Full gate + all four data-view states present (`STD-FE-030`) + UI Kit only | `ui.md` missing or Draft · design token needed that `docs/ui/` does not define |
| **Backend Change** | `backend-standards.md`, `csharp-coding-standards.md`; module docs | Full gate + architecture tests | Change alters a documented behavior with no `DEC`/ADR to justify it |
| **Bug Fix** | Only the standards for the layer touched; the failing test | Full gate + a **regression test that failed before the fix** | Fix would change a business rule → that is not a bug, it is a decision: **STOP, ask the owner** |
| **Refactor** | Standards for the layer touched | Full gate + **no behavior change**: the test suite passes untouched | A test must be modified to make the refactor pass → **STOP** (that is a behavior change) |
| **Documentation** | The docs being changed | Traceability intact · IDs unchanged · statuses untouched | Would create a **new governance artifact** → **STOP** (the Foundation is frozen) |
| **Review** | The diff + the standards it touches | Findings reported; nothing fixed silently | — |
| **Release** | `STATUS.md`, `CHANGELOG.md` | Push gate + all module docs Approved | Any Draft document that the released code depends on |

## Self review — before Commit, answer all nine

**Any YES → STOP. Do not commit. Explain why.**

1. Did I violate a **Principle**?
2. Did I violate an **ADR**?
3. Did I violate a **Standard** (Error-severity)?
4. Did I **duplicate code** — or business logic that already exists?
5. Did I **weaken a module boundary**?
6. Did I introduce **unnecessary complexity** (Simplicity Budget, ADR-0014 §12)?
7. Did I **invent business logic** that no document approves?
8. Did I **fail to update documentation** the change required?
9. **Should this change have been an ADR instead?**

## Commit gate

Run the full commit gate (**ADR-0017 §6** — authoritative). Commit only when:

- Definition of Done passes · architecture tests pass · all CI checks pass
- **No Error-severity standard violations**
- Documentation synchronized · no TODOs introduced · no hidden assumptions
- The repository is left consistent

**Otherwise: STOP.**

## Push gate

Run the full push gate (**ADR-0017 §7** — authoritative). Push only when:

- The commit gate passed
- **Owner review completed** and required approvals exist
- Repository integrity verified (ADR-0017 §8) · no broken references
- **No Draft document that the implementation depends on**

**Otherwise: STOP.**

## Standing rules for every mode

- **Minimal change.** The smallest correct change. Never rewrite a large file to
  fix a small problem. Never grow the architectural surface without measurable
  value (ADR-0017 §2, principle 14).
- **Determinism.** Same repository + same playbook + same prompt → substantially
  the same implementation. Ambiguity found in a document is a **defect to
  report**, not a gap to fill (ADR-0017 §3).
- **Stay modular.** The repository is modular; the AI must be too. Load what the
  task needs and nothing more.
- **The Foundation is frozen.** No new governance artifact unless a real
  implementation problem proves the governance insufficient (`principles.md`).
