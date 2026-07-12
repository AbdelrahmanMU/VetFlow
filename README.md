# VetFlow

Veterinary clinic management platform. Arabic-first UI. Primary users: a
veterinary doctor (owner) and a cashier/assistant.

## How this repository works

This project follows **Documentation-First Development**: no implementation
starts before the relevant documentation is written and approved by the owner.
Development is AI-assisted (Claude Code); the repository layout is optimized
for that — see `CLAUDE.md` and ADR-0001.

## Navigation

| Where | What |
|---|---|
| `STATUS.md` | Current state of work — always read this first |
| `docs/PROJECT_CONTEXT.md` | Stable project snapshot |
| `docs/modules/` | One folder per business module (the core of all documentation); `_TEMPLATE/` is the standard doc set |
| `docs/architecture/` | Architecture docs and ADRs |
| `docs/business/` | Domain knowledge and the global decision log |
| `docs/shared/` | Cross-module business docs: vision, glossary, personas, events, roadmap, reference |
| `docs/ui/` | Shared UI standards (RTL, components, navigation) |
| `docs/templates/` | Templates for ADRs and decision-log entries |
| `.claude/` | AI configuration: rules, slash commands, playbooks |
| `scripts/` | Development helper scripts |
| `src/`, `tests/` | Reserved — empty until documentation is approved |

## Current status

Sprint 0 — repository foundation only. No documentation content, no code.
See `STATUS.md` for open questions awaiting the owner.
