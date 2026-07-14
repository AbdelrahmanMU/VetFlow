> **SUPERSEDED 2026-07-14 — do not use, do not load.**
> Replaced by the **Bug Fix** mode in `.claude/playbooks/implementation.md`.
> Kept for repository history only.

# Playbook: Bug Fix

Repeatable procedure for fixing a defect.

1. **Reproduce.** Confirm the behavior and capture steps to reproduce.
2. **Classify against the docs** — this decides the whole path:
   - **Code disagrees with Approved docs** → plain bug. Fix the code; docs
     stay authoritative.
   - **Docs are wrong or silent** → not a bug fix; it is a requirements
     change. Ask the owner, update the module docs first, get re-approval
     (new-feature playbook, steps 2–4), then fix.
3. **Fix** the root cause, not the symptom. Keep the change minimal.
4. **Test.** Add a regression test that fails before the fix and passes after.
5. **Record** anything the owner decided along the way (module `decisions.md`
   or `docs/business/DECISION_LOG.md`).
6. **Close.** Update `STATUS.md` if the session was significant; note the fix
   in `CHANGELOG.md` under Unreleased.
