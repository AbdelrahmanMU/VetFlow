# VetFlow — Claude Code Router

VetFlow is a veterinary clinic management platform with an Arabic-first UI.
Users: a veterinary doctor (owner) and a cashier/assistant. This repository is
**Documentation-First**: no implementation before the relevant docs are approved.

## Rules (always apply)

@.claude/rules/workflow.md
@.claude/rules/documentation.md
@.claude/rules/naming.md
@.claude/rules/ai-governance.md

## Non-negotiables

1. Never invent business requirements — ask the owner.
2. No code before the relevant module documentation is Approved.
3. Business decisions are final; record them, don't relitigate them.
4. Update `STATUS.md` before ending a significant work session (`/close-session`).
5. The repository is the only source of truth — conversation history never is.
6. Never bypass a quality gate; repository integrity outranks finishing the task.
7. Keep this file under 80 lines — deep knowledge belongs in `docs/`.

## Where knowledge lives

| Need | Read |
|---|---|
| Current state / active work | `STATUS.md` |
| Stable project facts | `docs/PROJECT_CONTEXT.md` |
| A business module | `docs/modules/<name>/` (list: `docs/modules/_INDEX.md`) |
| Architecture & ADRs | `docs/architecture/` (ADR list: `decisions/_INDEX.md`) |
| Business domain & product decisions | `docs/business/` |
| Shared UI standards | `docs/ui/` |
| Cross-module business docs (vision, personas, events) | `docs/shared/` (index: `_INDEX.md`) |
| Release scope & direction | `docs/shared/roadmap/` |
| Canonical EN↔AR vocabulary | `docs/shared/GLOSSARY.md` |
| External reference material | `docs/shared/reference/` |
| How to run a kind of work (feature, review, bug, refactor, release) | `.claude/playbooks/` |

## Session start

1. Read `STATUS.md`.
2. If context was lost (new session / compaction), run `/recover-context`.
3. Read only the docs `STATUS.md` points at — do not preload module docs you
   are not working on.
