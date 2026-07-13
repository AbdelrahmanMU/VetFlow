# ADR-0017: AI Execution Model — Gates, Context, and Playbooks

- **Status:** Proposed
- **Date:** 2026-07-13
- **Relationship to `.claude/rules/ai-governance.md`:** that file is loaded in
  every session and holds **policy and pointers**. **This ADR is the
  authoritative enumeration** of the Definition of Ready, the quality gates, and
  the context model. One rule, one source: the rules file points here, it does
  not restate.

## Context

VetFlow is built primarily by an AI under owner supervision. An AI is fast,
tireless, and — unmanaged — an efficient producer of plausible drift: rules
re-derived instead of read, boundaries crossed for convenience, documentation
left behind by code. The answer is not more prose. It is **predictability**:
a fixed context model, fixed gates, and a fixed stop-rule.

## Decision

### 1. Context model — Mandatory / Optional / Forbidden-by-default

Every task classifies its documentation **before reading it**:

- **Mandatory** — the playbook for this kind of work, the standards it names,
  the target module's documentation, the GLOSSARY rows for the terms in play.
- **Optional** — related ADRs, adjacent modules' `Contracts`. Loaded on demand,
  never pre-loaded.
- **Forbidden-by-default** — other modules' internal documentation, discovery
  transcripts, superseded or annulled artifacts, `docs/business/` during pure
  implementation. Not banned outright: loading one requires **stating the reason
  in-session**.

**Context budget.** Every playbook declares its budget. **If mandatory context
exceeds the budget, STOP and split the task** — never widen the net to
compensate. If a needed fact is not in the mandatory context, **ask the owner**;
do not go hunting through the repository for it.

Why a budget at all: an AI with too much context is not better informed, it is
less accurate — and every token spent re-reading is a token not spent thinking.

### 2. Two permanent principles of AI execution

**The Minimal Change Principle.** The AI always prefers the **smallest correct
change**. It never rewrites a large file to solve a small problem, never
reformats code it was not asked to touch, never "tidies up" adjacent code inside
an unrelated change. A diff must contain the change and nothing else — because a
large diff hides its own defects, and a reviewer who cannot see the change
cannot approve it.

**The No Speculation Principle.** If the repository does not contain enough
information to proceed: **STOP. Ask.** The AI never invents a missing business
rule, requirement, acceptance criterion, or architectural decision — not as a
placeholder, not as a "reasonable assumption", not as a TODO. An invented
decision that looks plausible is worse than no decision at all, because it will
be trusted.

### 3. Contradiction policy

- The contradiction affects **the current wave or the current task** → **STOP.**
  Explain it. Write nothing until the owner rules.
- The contradiction affects **a future wave or other work** → **record it in
  `STATUS.md` and continue.**
- Never resolve a contradiction by inventing an alternative. Never overwrite a
  newer repository decision with an older one.

### 4. Definition of Ready — no implementation starts without it

- The module's `business-rules`, `requirements`, `acceptance`, `workflow`,
  `decisions`, and `ui` documents exist **and are Approved**.
- The API contract exists, where the slice has one.
- Every new identifier has an approved term in `docs/shared/GLOSSARY.md`.
- The slice names the `REQ-` / `BR-` / `AC-` IDs it implements.
- Anything missing → **stop and ask the owner.** Never fill the gap by
  inventing.

### 5. Commit gate — all must pass, in order

**Automated:**

Restore → Build (**zero warnings**) → Formatting → Roslyn analyzers →
**Architecture tests** → Unit tests → Integration tests (**zero failures**).

**Clean diff — none of these may enter a commit:**

`TODO` · `FIXME` · `HACK` · dead code · commented-out code · debug code
(`Console.WriteLine`) · temporary configuration · sensitive data.

**Synchronized:**

Documentation updated · ADR added or updated if architecture changed · business
documentation updated if behavior changed · acceptance criteria still valid ·
**business-rule traceability present** (every implemented `BR-*` has a test
naming its ID, ADR-0016) · UI Kit compliance (ADR-0012) · `STATUS.md` current.

Only then may a commit proceed.

### 6. Push gate — stricter than commit

Repository clean · no untracked files · no merge conflicts · no broken
references · no traceability gaps · no documentation drift · no architecture
drift · no ADR violations · no circular dependencies · no duplicated ownership ·
no unapproved public API break · no security regression · no performance
regression (against ADR-0014 budgets) · no standards violation · no playbook
violation · no business-rule inconsistency.

### 7. Repository integrity gate — before every push

- Every decision has exactly one owner. Every rule has exactly one source.
- Every document has exactly one responsibility.
- Every implementation traces back to documentation; every documentation change
  traces back to a business or architectural decision.
- No duplicated architectural responsibility. No conflicting governance.

### 8. When a gate fails

**STOP.** Explain the failure. Recommend the **minimum** correction.

Never weaken the gate, disable a test, suppress an analyzer, remove a
validation, or proceed past a failure. **Repository integrity always outranks
completing the task** (constitutional).

### 9. Playbooks

Execution is standardized in `.claude/playbooks/`. Every playbook has the same
header: **Inputs → Context Budget (Mandatory / Optional / Forbidden-by-default
/ budget / escalation rule) → Steps → Validation → Stop conditions → Review
gate.**

### 10. Review checkpoints — the AI stops for the owner at

A new ADR · a new library · a module boundary change · any deviation from a
standard · a governance wave · completion of a feature slice.

## Alternatives Considered

- **Trusting instructions in conversation:** fails the constitution's twelfth
  principle — conversation history is never authoritative, and a new session
  starts blind.
- **One enormous always-loaded rules file:** every session pays for it, and long
  files are skimmed rather than followed. Split adopted: thin always-loaded
  policy, deep enumeration one link away.
- **Coverage/lint gates only:** catches code defects, catches no drift in the
  documentation or the boundaries — which is where AI-built systems actually rot.

## Consequences

- AI behavior becomes predictable and auditable; a future session behaves like
  this one without being re-taught.
- Rules that are enforced by CI never need re-explaining in conversation — the
  build says no. This is the largest long-term token saving in the repository.
- The gates are demanding by design. A task that cannot pass them is not
  finished, however much of it is written.
