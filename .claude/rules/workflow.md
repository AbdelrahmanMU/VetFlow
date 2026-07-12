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
