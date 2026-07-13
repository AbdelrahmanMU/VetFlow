# Rule: AI Development Governance

> Status: Draft. Loaded in every session. Binding on every AI contributor.
> Policy and pointers only — the detailed rules live where they are linked.
> Rationale: `docs/architecture/principles.md`. Execution model: ADR-0017.

**Repository integrity always outranks completing the task.** When a rule here
and the task conflict, the rule wins — stop and explain.

**Authority hierarchy** (`principles.md`): Principles → ADRs → Standards →
Playbooks. The highest applicable authority always wins.

## Session protocol

1. Read `STATUS.md` first. New session or after compaction: `/recover-context`.
2. Load only what the playbook for this kind of work names (`.claude/playbooks/`).
3. The **repository is the only source of truth**. Never rely on conversation
   history or memory — verify in the repository.
4. Update `STATUS.md` before ending a significant session (`/close-session`).

## Context loading

- **Mandatory** — the playbook, its named standards, the target module's docs,
  the GLOSSARY rows in play.
- **Optional** — related ADRs, adjacent modules' contracts. On demand only.
- **Forbidden-by-default** — other modules' internals, discovery transcripts,
  superseded/annulled artifacts, `docs/business/` during implementation.
  Loading one requires stating the reason in-session.

**Escalation:** if mandatory context exceeds the playbook's context budget,
**STOP and split the task** — never widen the net. If a needed fact is missing,
**ask the owner** — do not go hunting.

## Contradiction policy

- Contradiction affects the **current wave / current task** → **STOP.** Explain
  it. Do not write until the owner rules.
- Contradiction affects a **future wave / other work** → **record it** in
  `STATUS.md` and continue.
- Never resolve a contradiction by inventing an alternative, and never overwrite
  a newer repository decision with an older one.

## Never

- Invent a business rule, requirement, or acceptance criterion.
- Change architecture silently — ADR first, owner approval before it lands.
- Bypass a module boundary, or duplicate business logic instead of extending
  the module that owns it.
- Introduce a library, framework, or tool without owner approval.
- Weaken, disable, or delete an architecture test; suppress an analyzer; remove
  a validation; skip a failing test.
- Bypass a quality gate, or "fix" a gate by lowering it.
- Modify an ADR's meaning, or a business rule, silently.
- Leave documentation contradicting the code.

Prefer existing abstractions · small atomic changes · explain trade-offs when
deviating · business before technology · simplicity over cleverness ·
consistency over preference · measure before optimizing.

## Gates — mandatory, never bypassed

- **Definition of Ready** — no implementation starts without it → ADR-0017.
- **Commit gate** — build/format/analyzers/architecture tests/all tests, clean
  diff, docs synchronized → ADR-0017.
- **Push gate** and **Repository Integrity gate** → ADR-0017.

**When any gate fails: STOP.** Explain the failure, recommend the *minimum*
correction. Never weaken the gate, never proceed past it, never hide it.

## Review checkpoints — stop for the owner at

A new ADR · a new library · a module boundary change · any deviation from a
standard · a governance wave · completion of a feature slice.
