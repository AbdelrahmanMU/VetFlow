> **SUPERSEDED 2026-07-14 — do not use, do not load.**
> Replaced by the **Review** mode in `.claude/playbooks/implementation.md`.
> Kept for repository history only.

# Playbook: Review

Repeatable procedure for reviewing work (documentation or code).

1. **Scope.** Identify what changed and which modules it touches.
2. **Documentation review** (`/review-docs` covers the whole repo):
   - Standard module file set complete (per `docs/modules/_TEMPLATE/`);
     module has an `_INDEX.md` row.
   - Language per ADR-0002: engineering docs English; business/product
     content Arabic; IDs and statuses in English.
   - No contradiction with Accepted ADRs or decision-log entries.
   - Domain terms exist in `docs/shared/GLOSSARY.md` with one approved Arabic
     form.
   - Doc statuses honest (`Draft/Review/Approved`).
3. **Code review** (only after implementation begins):
   - Change is covered by Approved documentation; every behavior traces to a
     `REQ-`/`BR-` ID — anything else is an undocumented requirement: stop and
     route it through the new-feature playbook.
   - Follows `.claude/rules/coding.md`; user-facing text matches the module's
     `ui.md` Arabic copy.
4. **Report findings without fixing** — fixes need owner approval when they
   touch business meaning.
5. **Record** accepted review outcomes per the decision routing rule.
