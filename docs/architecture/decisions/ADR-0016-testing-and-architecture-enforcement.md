# ADR-0016: Testing Strategy and Architecture Enforcement

- **Status:** Proposed
- **Date:** 2026-07-13

## Context

Two problems, one answer. **Confidence:** the system holds medical and
financial records, so tests must prove behavior, not implementation detail.
**Drift:** governance written in prose decays — a rule that nobody enforces is
a rule that a tired human or a confident AI will violate within months.

Architecture tests solve the second problem by making the rules executable.
They are the enforcement arm of the entire constitution.

## Decision

### 1. Architecture tests are mandatory

Every architectural rule is executable and runs in CI. **The test suite is the
rule registry** — there is no parallel markdown list of rules to drift out of
sync with the code.

Rules enforced at minimum:

- `Domain` references nothing (no EF Core, no ASP.NET, no Infrastructure).
- `Application` never references `Infrastructure` or PostgreSQL — only ports.
- `Api` never touches domain entities directly.
- Cross-module access only through a module's `Contracts` namespace (ADR-0014).
- No circular dependencies between modules.
- Handlers, entities, and other conventions follow their naming rules.
- Frontend (ESLint): `primeng/*` is not importable outside the UI Kit
  (ADR-0012); features do not import other features.

**A violation fails CI. A failing architecture test is fixed by changing the
code — never by changing the test.**

### 2. The Architecture Rule template

Every architectural rule is recorded with five parts. A rule missing any of them
is not a rule:

| Part | Meaning |
|---|---|
| **Reason** | Why the rule exists — the principle or ADR it protects |
| **ADR** | Where it was decided |
| **Test** | The automated test that enforces it |
| **CI** | The pipeline stage that runs it |
| **Exception** | How an exception is obtained: an ADR plus explicit owner approval — never a code comment, never a suppression |

**Architecture tests may never be weakened, disabled, or deleted without an ADR
and owner approval** (constitutional — `principles.md`).

### 3. Testing strategy — integration-first

| Layer | How it is tested | Why |
|---|---|---|
| **Domain** | Pure unit tests, no infrastructure | Invariants are genuinely unit-shaped; these are fast and permanent |
| **Application + Api** | **Integration tests against a real PostgreSQL in a container** (Testcontainers), through the real pipeline | Docker is already mandatory (ADR-0008); a mocked database proves nothing about EF Core, transactions, or constraints |
| **Infrastructure** | Covered by the integration tests that exercise it | Testing adapters in isolation tests the mock |
| **Frontend — UI Kit** | Component tests | The UI Kit is a contract; its stability is load-bearing |
| **Frontend — features** | Lighter smoke coverage of the critical paths | Screens change; behavior is proved server-side |

**The mock-everything unit-test pyramid is rejected.** Heavy mocking verifies
mocks, not behavior, and rots on every refactor.

### 4. End-to-end traceability instead of coverage targets

Every implemented business capability preserves this chain, end to end, in the
repository:

```
Business Rule  (BR-*)  →  Requirement  (REQ-*)  →  Acceptance  (AC-*)
      →  Scenario  (TS-*)  →  Implementation  →  Automated Test
```

**Every implemented `BR-*` has at least one test naming its ID:**

```
Product_cannot_be_deactivated_while_an_open_package_exists_BR_CAT_031
```

Every `BusinessRuleException` and every error code (ADR-0018) has an automated
test. Each link of the chain is therefore navigable in both directions: from a
rule the owner approved to the test that proves it, and from a failing test back
to the business rule it defends. Checked by the commit gate (ADR-0017).

**Coverage-percentage gates are rejected.** They optimize the metric, not
confidence. Traceability asks the question that matters: *is every rule the
owner approved actually enforced, and proven?*

### 5. CI performance budget — enforcement must stay fast

Architecture quality must never destroy developer productivity. A gate that
takes too long stops being run honestly and starts being worked around.

| CI stage | Budget (proposed — owner approval pending) |
|---|---|
| Build + analyzers | < 2 min |
| Architecture tests | **< 30 s** |
| Unit tests (domain) | < 1 min |
| Integration tests (containers) | < 5 min |
| **Full pipeline, commit to green** | **< 10 min** |

- Every **Mandatory** standard declares its enforcement **Cost** (compilation ·
  runtime · CI · architecture test · review). A rule whose enforcement is
  expensive must earn it.
- **A rule that significantly slows CI requires owner approval** before it is
  added.
- Budget breaches are investigated like any other performance regression
  (principle 14): measure first, then fix the stage the measurement names.
- Architecture tests are reflection-based and run without I/O; if they ever
  approach their budget, the fix is to make the tests cheaper — **never** to run
  them less often.

### 6. Mutation testing — allowed later, never a replacement

Mutation testing may be introduced later, when there is a reason to ask whether
the existing tests actually assert anything. It is a **quality check on tests**,
not a source of confidence in the system.

**It never replaces architecture tests, and it never replaces integration
tests.** Nothing may be traded away for it.

### 7. Test naming and organization

- Test projects mirror the source layers; test folders mirror the modules.
- Test names are sentences describing behavior, ending in the `BR-*` ID where
  one applies. No `Test1`, no `Should_Work`.
- One assertion concept per test.
- Detail lives in `standards/backend-standards.md` and
  `standards/frontend-standards.md`.

## Alternatives Considered

- **Prose-only architecture rules:** free to write, worthless under pressure.
- **Classic unit-test pyramid with mocked repositories:** faster tests, weaker
  guarantees; the failures VetFlow fears (transaction, constraint, mapping)
  live exactly where mocks hide them.
- **Coverage gate (e.g. 80%):** easy to measure, easy to game, silent about
  whether the *right* things are tested.

## Consequences

- CI enforces the constitution; an AI or a human cannot silently cross a
  boundary — the build says no.
- Integration tests need containers in CI, and are slower than mocked unit
  tests. Accepted: correctness of a records system outranks test-suite speed
  (principle 9).
- Approved test tooling: xUnit, Testcontainers, and an architecture-test library
  (NetArchTest.Rules — chosen for a simple fluent API and no runtime baggage).
  FluentAssertions is rejected (commercial licensing, 2025); assertions use the
  built-in library or Shouldly.
