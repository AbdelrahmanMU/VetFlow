# Playbook: Discovery

Repeatable procedure for discovering and documenting a domain or module with
the owner — the phase that precedes everything else in Documentation-First.

1. **Prepare.** Read `STATUS.md`, `docs/PROJECT_CONTEXT.md`, the module's
   `_INDEX.md` row, and any related modules' overviews. List what is already
   decided (ADRs, decision logs) so it is not relitigated.
2. **Ask, never assume.** Collect from the owner: purpose, actors, main
   flows, business rules, edge cases, and what is explicitly out of scope.
   Business content is captured in Arabic (per ADR-0002).
3. **Capture as you go:**
   - Answers → the module's docs (`overview`, `requirements`,
     `business-rules`, `workflow`, `ui`, `acceptance`, `test-scenarios`).
   - Decisions → module `decisions.md` / `docs/business/DECISION_LOG.md` /
     ADR, per the routing rule.
   - New domain terms → `docs/shared/GLOSSARY.md` (one approved Arabic form).
   - Cross-module events → `docs/shared/events.md`.
4. **Confirm boundaries.** Restate scope and out-of-scope back to the owner;
   record open questions in `STATUS.md` rather than guessing.
5. **Hand off.** Leave all docs in `Draft`, ready for the review/approval
   cycle (new-feature playbook, step 4). Update `STATUS.md`.
