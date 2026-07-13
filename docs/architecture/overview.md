# Architecture Overview

> **Status: Draft** — pending owner review (Wave 1 of the governance
> foundation).
>
> The map of VetFlow: what the system is, and **where every kind of decision
> lives**. Start here. This document holds no decisions of its own — it points
> at the documents that do.

## The system in one picture

```
                    Angular SPA (Arabic-first, RTL, adaptive)
                    └── features/*  →  VetFlow UI Kit  →  [PrimeNG]
                                   │
                                   │  HTTPS / product API (/api/v1)
                                   ▼
  ┌──────────────────────────────────────────────────────────────┐
  │  Api            HTTP, composition root, error → ProblemDetails│
  ├──────────────────────────────────────────────────────────────┤
  │  Application    use cases, ports, validation, pipeline        │
  ├──────────────────────────────────────────────────────────────┤
  │  Domain         entities, invariants, business rules, events  │  ← no dependencies
  ├──────────────────────────────────────────────────────────────┤
  │  Infrastructure EF Core, identity, cache, files, time         │  → implements Application ports
  └──────────────────────────────────────────────────────────────┘
                                   │
                              PostgreSQL  (in Docker, all environments)
```

**The dependency rule:** dependencies point inward. Domain depends on nothing.
Application depends only on Domain. Infrastructure and Api depend inward and
are never depended upon. Enforced by architecture tests in CI — not by
convention.

**Modules** (`Catalog`, `Purchasing`, `Inventory`, `Sales`, …) mirror
`docs/modules/` and exist as boundaries *inside* each layer. Cross-module
access goes through sanctioned contracts only.

## Technology

Decided by the owner; the ADRs are written and currently **Proposed** — they
become Accepted when the owner flips their status. Per principle 12, the ADR
status in the repository is the authority, not this table.

| Concern | Choice | Decided in |
|---|---|---|
| Backend | ASP.NET Core, latest stable LTS | ADR-0003 |
| ORM | EF Core only — no hybrid ORM | ADR-0004 |
| Frontend | Angular, latest stable | ADR-0005 |
| API posture | Product API from day one (mobile, portal, AI agents, integrations) | ADR-0006 |
| Localization | Arabic-first MVP, localization-ready; EGP · Gregorian · 0-9 | ADR-0007 |
| Tooling | Docker from the beginning | ADR-0008 |
| UI | Own design system + adaptive UI | ADR-0009 |
| Authentication | Identity in MVP, behind a provider-neutral abstraction | ADR-0010 |
| Caching | Infrastructure-only; invisible to Domain/Application | ADR-0011 |
| UI library | VetFlow UI Kit is the public architecture; PrimeNG is an internal detail | ADR-0012 |
| Frontend structure | Feature-based (`/core`, `/shared`, `/features/*`) | ADR-0013 |
| Database | **PostgreSQL** — Infrastructure-only knowledge; replaceable | ADR-0019 *(planned, Wave 2)* |

Planned in Wave 2: ADR-0014 backend architecture · ADR-0015 API contract ·
ADR-0016 testing & architecture enforcement · ADR-0017 AI execution model ·
ADR-0018 business failure strategy (business exceptions + error catalog;
`Result<T>` rejected) · ADR-0019 database platform.

## Engineering Decision Matrix

**Every decision has exactly one owner and exactly one home.** Find the row
before writing anything.

| Decision type | Governed by | Recorded in | Change requires |
|---|---|---|---|
| Business rule, requirement, workflow | The owner | `docs/modules/<m>/` (Arabic) | Owner ruling → module `decisions.md` |
| Cross-module business decision | The owner | `docs/business/DECISION_LOG.md` | Owner ruling |
| Architecture, hard to reverse (> 1 day) | Architect + owner approval | ADR in `decisions/` | **New ADR** superseding the old |
| Engineering principle | The owner | `principles.md` | Owner decision, dated + reasoned |
| Code convention, naming, EF/infra pattern | Architect | `standards/*.md` | Owner-approved change citing a reason |
| Execution order for a kind of work | Architect | `.claude/playbooks/*.md` | Update playbook + affected standards |
| AI operating rules, quality gates | The owner | `.claude/rules/ai-governance.md` | Owner decision |
| Architectural rule enforcement | Architecture tests | Test code (the rule registry) | **ADR + owner approval** to weaken |
| UI appearance, tokens, component look | Design system | `docs/ui/` *(pending UI/UX discovery)* | Design system change |
| Current state, in-flight work | Whoever works | `STATUS.md` | Update every significant session |

If a decision fits no row, it is an architectural gap: **stop and ask the
owner.** Do not invent a home for it.

## Where knowledge lives

| Need | Read |
|---|---|
| Why we build this way | `principles.md` (the constitution) |
| What was decided, and why | `decisions/_INDEX.md` |
| How to write code here | `standards/` (C#, backend, frontend, API) |
| How to execute a kind of work | `.claude/playbooks/` |
| Rules every AI session must follow | `.claude/rules/ai-governance.md` |
| A business module | `docs/modules/<name>/` (index: `_INDEX.md`) |
| Arabic ↔ English vocabulary | `docs/shared/GLOSSARY.md` |
| Current state / active work | `STATUS.md` |
| Database platform trade-offs | `database-platform-study.md` |

## Cross-cutting concerns

`cross-cutting/` holds concerns that genuinely span modules: `audit.md`,
`data-integrity.md`, `localization.md`. They are placeholders until their
topics are documented; the constitutional anchor for the first two is
principle 9 (*integrity over convenience*).
