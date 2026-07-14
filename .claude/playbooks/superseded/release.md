> **SUPERSEDED 2026-07-14 — do not use, do not load.**
> Replaced by the **Release** mode in `.claude/playbooks/implementation.md`.
> Kept for repository history only.

# Playbook: Release

Repeatable procedure for cutting a release.

1. **Check the scope contract.** The release's file in
   `docs/shared/roadmap/releases/` is the source of truth. Anything not in it does
   not ship; scope changes are the owner's decision and are recorded there.
2. **Verify readiness:**
   - Every in-scope module: docs Approved, implementation matches every
     `REQ-`/`BR-`/`AC-` ID and `test-scenarios.md`, `docs/modules/_INDEX.md`
     status correct.
   - `/review-docs` passes; no open blocking questions in `STATUS.md`.
   - Full test suite passes; Arabic UI copy matches each module's `ui.md`.
3. **Owner sign-off** against the release's definition of done.
4. **Cut the release.** Move `CHANGELOG.md` Unreleased entries under the new
   version with the date; tag per the (future) versioning ADR.
5. **Close out.** Update `STATUS.md` and `docs/shared/roadmap/ROADMAP.md` (what
   shipped, what moves to the next release); record deferred scope in the
   release file.
