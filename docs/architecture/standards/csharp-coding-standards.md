# C# Coding Standards

> **Status: Draft.** Executable engineering contract — not documentation.
> Rationale lives in the ADRs; this document contains only enforceable rules.

**Adopted by reference:** Microsoft's C# coding conventions and .NET library
design guidelines. **This document records only what Microsoft does not decide
for VetFlow.** Nothing here restates them.

**Defaults (declared once, apply unless a row says otherwise):**
Scope = `Cross-Cutting` · Stability = `Stable` · Depends On = none ·
Class = `Mandatory` · Severity = `Error`.

**Severity policy:** only `Error` blocks CI. `Warning` never blocks CI — it
blocks push only when the active playbook requires it, otherwise it is a review
item. `Info` never blocks. (ADR-0017 §7.)

**Exceptions:** only via the Exception Register at the foot of this document.
No inline suppressions, ever.

## Language and type rules

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-CS-001 | Nullable reference types are enabled repository-wide | Mandatory | Error | Automatic | Compiler + analyzer | Compilation | [P](../principles.md) |
| STD-CS-002 | Analyzer and compiler warnings are treated as errors (`TreatWarningsAsErrors`) | Mandatory | Error | Automatic | Compiler | Compilation | [P](../principles.md) |
| STD-CS-003 | The null-forgiving operator `!` is prohibited | Mandatory | Error | Automatic | Analyzer | Compilation | [P](../principles.md) |
| STD-CS-004 | `dynamic` is prohibited | Mandatory | Error | Automatic | Analyzer | Compilation | [P](../principles.md) |
| STD-CS-005 | File-scoped namespaces are mandatory | Mandatory | Error | Automatic | Analyzer (IDE0161) | Compilation | — |
| STD-CS-006 | One public type per file; file name equals type name | Mandatory | Error | Automatic | Analyzer | Compilation | — |
| STD-CS-007 | `partial` types are allowed only when pairing with generated code | Mandatory | Error | Semi-Automatic | Analyzer + review | Review | — |
| STD-CS-008 | Code formatting follows `.editorconfig`; `dotnet format --verify-no-changes` passes | Mandatory | Error | Automatic | CI script | CI | — |

## Modern C# usage

New language features are adopted when they **remove ceremony**, and rejected
when they **hide behavior** (principle 4).

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-CS-010 | `record` for commands, queries, DTOs, and value objects; `class` for entities | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-CS-011 | Primary constructors are allowed for DI services; **prohibited on domain entities** (invariant-enforcing constructors stay explicit) | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-CS-012 | DTO and command properties use `required` / `init`; no settable public state after construction | Mandatory | Error | Semi-Automatic | Analyzer + review | Compilation | — |
| STD-CS-013 | Value objects are immutable | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |
| STD-CS-014 | Expression-bodied members only for a single expression; never to compress multi-step logic | Recommended | Warning | Manual | Engineering review | Review | — |
| STD-CS-015 | Collection expressions preferred over explicit collection construction | Recommended | Info | Automatic | Analyzer (IDE0300) | Compilation | — |
| STD-CS-016 | `readonly struct` only for small value types with a measured need | Recommended | Info | Manual | Engineering review | Review | [P14](../principles.md) |
| STD-CS-017 | Static classes hold pure helpers only — **no static mutable state** | Mandatory | Error | Automatic | Analyzer | Compilation | — |
| STD-CS-018 | Extension methods are for mapping and fluent configuration; **never for business logic on domain types** | Mandatory | Error | Semi-Automatic | Architecture test + review | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |

## Correctness and hygiene

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-CS-020 | Async all the way: no `async void`, no sync-over-async (`.Result`, `.Wait()`) | Mandatory | Error | Automatic | Analyzer | Compilation | — |
| STD-CS-021 | Every async method accepts and forwards a `CancellationToken` | Mandatory | Error | Automatic | Analyzer | Compilation | — |
| STD-CS-022 | `System.Exception` is never thrown; only typed exceptions | Mandatory | Error | Automatic | Architecture test | Architecture test | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-CS-023 | Exceptions are never caught-and-ignored or swallowed; an empty `catch` is prohibited | Mandatory | Error | Automatic | Analyzer | Compilation | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-CS-024 | Exceptions are never used for normal control flow | Mandatory | Warning | Manual | Engineering review | Review | [ADR-0018](../decisions/ADR-0018-business-failure-strategy.md) |
| STD-CS-025 | No magic strings: repeated literals become named constants or typed values | Mandatory | Warning | Semi-Automatic | Analyzer + review | Compilation | — |
| STD-CS-026 | No magic numbers: numeric literals other than `0`/`1` are named | Mandatory | Warning | Semi-Automatic | Analyzer + review | Compilation | — |
| STD-CS-027 | `Console.WriteLine` and debug output are prohibited in product code; use `ILogger` | Mandatory | Error | Automatic | Analyzer | Compilation | [ADR-0015](../decisions/ADR-0015-api-contract.md) |
| STD-CS-028 | No `TODO`, `FIXME`, or `HACK` in committed code | Mandatory | Error | Automatic | CI script | CI | [ADR-0017](../decisions/ADR-0017-ai-execution-model.md) |
| STD-CS-029 | No commented-out code and no dead code | Mandatory | Error | Automatic | Analyzer + CI script | CI | [ADR-0017](../decisions/ADR-0017-ai-execution-model.md) |
| STD-CS-030 | No secrets, connection strings, or credentials in source or configuration files | Mandatory | Error | Automatic | CI secret scan | CI | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |

## Craft rules — honest about their enforcement

These are **not** mechanically checkable. They are binding on review, and they
are labelled Manual rather than dressed up as automation (principle 14).

| ID | Rule | Class | Sev | Autom | Enforced By | Cost | Source |
|---|---|---|---|---|---|---|---|
| STD-CS-040 | Names are explicit and drawn from the domain vocabulary in `GLOSSARY.md` — no abbreviations, no invented synonyms | Recommended | Warning | Manual | Engineering review | Review | [ADR-0002](../decisions/ADR-0002-documentation-language.md) |
| STD-CS-041 | No hidden side effects: a method does what its name says and nothing else | Recommended | Warning | Manual | Engineering review | Review | [P4](../principles.md) |
| STD-CS-042 | Methods are small; a method that needs a section comment to be understood is split | Recommended | Info | Manual | Engineering review | Review | [P5](../principles.md) |
| STD-CS-043 | One responsibility per class — one reason to change | Recommended | Warning | Manual | Engineering review | Review | [P5](../principles.md) |
| STD-CS-044 | Code is deterministic: no reliance on ambient time, randomness, or culture — inject `TimeProvider` and pass culture explicitly | Mandatory | Error | Semi-Automatic | Architecture test (no `DateTime.Now`/`Random`) + review | Architecture test | [ADR-0014](../decisions/ADR-0014-backend-architecture.md) |

## Exception Register

Every approved exception is logged here once — `STD-*` ID, scope, owner
approval, date. **No inline exceptions. No suppression comments.**

| STD | Scope of exception | Reason | Approved by | Date |
|---|---|---|---|---|
| — | *(none)* | | | |

## Tombstones

Removed standards keep their ID forever; references never break.

| STD | Removed | Reason |
|---|---|---|
| — | *(none)* | |
