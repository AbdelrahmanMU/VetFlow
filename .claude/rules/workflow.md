# Rule: Documentation-First Workflow

- No implementation before the relevant module documentation is **Approved**
  by the owner. Doc lifecycle: `Draft → Review → Approved` (status at the top
  of each doc).
- Never invent business requirements. If information is missing, ask the owner
  and record the answer in a decision log.
- Decision routing (one place per decision): expensive to reverse (> 1 day) →
  ADR in `docs/architecture/decisions/`; otherwise **Global** (spans modules)
  → `docs/business/DECISION_LOG.md`, **Module** (single module) → that
  module's `decisions.md`.
- Update `STATUS.md` before ending a significant session (`/close-session`).

## Continuous Capability Mode (owner ruling, 2026-07-31)

**The unit of implementation is the approved Epic — not the slice, the screen,
or the capability.** Inside an Epic: continue automatically between
capabilities, verify after each one, fix what you find immediately, and do
**not** wait for the owner between them.

**Stop only when all seven hold:** the Epic is fully implemented · all tests
pass · architecture tests pass · browser verification passes · performance
verification passes · self review is complete · the **Epic Owner Report** is
ready. Then **do not commit and do not push — wait for Epic Commit Approval.**

This changes **when** work stops, nothing else. **A failing gate still stops
everything** — "continue without waiting" never means continue past red. The
Definition of Ready, the No-Speculation rule, and every gate apply unchanged to
each capability. Authoritative enumeration: **ADR-0017 §11/§11a**.

**Exception:** the owner may explicitly request a design review, which returns
that work to slice-by-slice review.

## Pilot Observation Mode (owner ruling, 2026-08-01) — **currently in force**

**The Pilot phase suspends Continuous Capability Mode.** While the Pilot runs,
the AI contributor is a **Pilot Observation & QA assistant**, not an
implementation agent, and does exactly four things: help reproduce issues ·
help classify findings · explain expected behavior when asked · prepare fixes
**only after the owner approves them**.

**Prohibited until the owner says «Fix this issue»:** implementing a feature ·
refactoring · improving architecture · optimizing performance · creating new
documentation unless explicitly requested · suggesting enhancements while the
owner is testing. An observation that is merely described is **classified and
then waited on** — never acted upon.

Every finding is classified into exactly one of **Bug · Usability ·
Enhancement** (the 2026-07-31 GO ruling, unchanged) and reported in the ruled
structure recorded in `docs/operations/pilot-findings.md`. **Related findings
are grouped into one issue, never duplicated.**

**The goal of the Pilot is operational stability, not feature expansion.**
This changes **what work is permitted**, not the gates: if the owner approves a
fix, it passes every existing gate unchanged.
