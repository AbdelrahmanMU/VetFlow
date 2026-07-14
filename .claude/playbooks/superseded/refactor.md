> **SUPERSEDED 2026-07-14 — do not use, do not load.**
> Replaced by the **Refactor** mode in `.claude/playbooks/implementation.md`.
> Kept for repository history only.

# Playbook: Refactor

Repeatable procedure for restructuring without changing behavior.

1. **State the goal** in one sentence (what improves; e.g. duplication,
   coupling, naming). If observable behavior would change, this is not a
   refactor — use the new-feature or bug-fix playbook.
2. **Check the blast radius.** A refactor that is expensive to reverse
   (> 1 day) or changes an architectural boundary needs an ADR first
   (`/new-adr`).
3. **Baseline.** Ensure existing tests pass before touching anything; add
   characterization tests where coverage is thin.
4. **Refactor in small, individually-safe steps.** Keep each step passing.
   Documentation refactors follow the same rule: preserve meaning, keep
   stable IDs (`REQ-`/`BR-`/ADR numbers) — never renumber.
5. **Verify** behavior is unchanged: full test run; docs still consistent
   (`/review-docs` for documentation refactors).
6. **Close.** Update any docs whose file paths or links moved, and
   `STATUS.md` if the session was significant.
