> **SUPERSEDED 2026-07-14 — do not use, do not load.**
> Replaced by the **New Feature** mode in `.claude/playbooks/implementation.md`.
> Kept for repository history only.

# Playbook: New Feature

Repeatable procedure for adding a feature. Documentation-First applies: no
code before step 4 is complete.

1. **Locate the module.** Find the owning module in `docs/modules/_INDEX.md`.
   If the feature does not belong to an existing module, discuss with the
   owner whether it is a new module (`/new-module`) or an extension.
2. **Document.** Update the module's `requirements.md` (stable `REQ-` IDs),
   `business-rules.md` (stable `BR-` IDs), `workflow.md` (flows),
   `ui.md` (screens + Arabic copy), `acceptance.md` (stable `AC-` IDs), and
   `test-scenarios.md` (stable `TS-` IDs). Business content in Arabic per
   ADR-0002. Never invent requirements — ask the owner and record answers per
   the decision routing rule (`.claude/rules/workflow.md`).
3. **Record decisions.** Module-scoped → module `decisions.md`; cross-module →
   `docs/business/DECISION_LOG.md`; expensive to reverse → ADR (`/new-adr`).
   New domain terms → `docs/shared/GLOSSARY.md`; new cross-module events →
   `docs/shared/events.md`.
4. **Get approval.** Owner moves the affected docs `Draft → Review → Approved`.
5. **Implement** per the approved docs and `.claude/rules/coding.md`.
   Deviations discovered during implementation go back through step 2.
6. **Verify** against every affected `REQ-`/`BR-`/`AC-` ID; cover the
   module's `test-scenarios.md`.
7. **Close.** Update `docs/modules/_INDEX.md` status, `STATUS.md`
   (`/close-session`), and `CHANGELOG.md` if a release is being cut.
