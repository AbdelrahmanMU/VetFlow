# Engineering Principles — The VetFlow Constitution

> **Status: Draft** — pending owner review (Wave 1 of the governance
> foundation).
>
> This is the engineering constitution: the reasoning that settles arguments.
> It is not a coding standard. When a standard, a playbook, or a code review
> disagrees with this document, this document wins — and the disagreement is
> fixed at its source.

## Team & development model

VetFlow is primarily developed and maintained by **the owner with AI
assistance**. Additional developers may join in the future; the codebase must
be ready to receive them.

The engineering foundation prioritizes, in this order:

1. **Maintainability** — every choice is judged by its cost five years out.
2. **Readability** — code is written for the next reader, not the author.
3. **Consistency** — one way to do each thing; conventions over cleverness.
4. **Low cognitive complexity** — small units, shallow nesting, explicit flow.
5. **Clear module boundaries** — modules own their data and rules; coupling is
   deliberate and documented.

**Development speed is important, but never more important than long-term
maintainability.**

## The thirteen principles

### 1. Business first

Technology serves documented business rules, never the reverse. Business
decisions belong to the owner and are final. Business requirements are never
invented — when information is missing, the work stops and the owner is asked.

### 2. The domain owns business rules

Every business rule lives in the domain layer, once. A rule enforced in a
controller, a query, a UI component, or a database constraint *instead of* the
domain is a defect — not a shortcut. There are no hidden business rules, and no
business logic is duplicated: if two places need a rule, they call the same
rule.

### 3. Module boundaries are sacred — enforced, not promised

Modules mirror the documented business modules (`docs/modules/`). A module
owns its data and its rules. Cross-module access happens only through
sanctioned contracts, never by reaching into another module's internals. This
boundary is enforced by automated architecture tests, because a boundary that
depends on memory is a boundary that is already broken.

### 4. Explicit over implicit

The code says what it does. No runtime magic, no convention-based dispatch, no
reflection-driven behavior where a direct call would do. A reader — human or
AI — must be able to follow the path from request to rule by reading, not by
guessing. This principle is why VetFlow rejects mediator and object-mapping
libraries: indirection that hides the call target is a cost, not a feature.

### 5. Simplicity over cleverness

The boring solution wins by default. Cleverness must justify its existence in
an ADR. Complexity is a permanent tax paid by every future reader; it is
borrowed, never free.

### 6. No speculation

Features come from approved documentation. Abstractions come from demonstrated
need. Optimizations come from measurements. All three are the same discipline
in three tenses: **build what is known to be needed, now.** Deliberate
future-proofing (the mandatory abstractions in principle 7) is an owner
decision, never a habit.

### 7. The periphery is replaceable

Every external technology sits behind an abstraction the application owns:

- The identity provider is replaceable (ADR-0010).
- The cache is invisible to Domain and Application (ADR-0011).
- The UI component library is invisible to feature modules — the application
  depends only on the VetFlow UI Kit (ADR-0012).
- **The database is replaceable.** PostgreSQL is the chosen platform, but
  database-specific knowledge lives only in Infrastructure. Domain and
  Application never depend on PostgreSQL.

The core knows *what* it needs, never *who* provides it.

### 8. Fail fast

Invalid configuration refuses to boot. Broken invariants throw. Business rule
violations raise typed exceptions carrying stable error codes. Nothing fails
silently, nothing is swallowed, and no operation continues in a half-applied
state. Silence is the enemy: a loud failure is a bug report, a silent one is a
corrupted record.

### 9. Integrity over convenience

VetFlow holds medical and financial records. Correctness of the record outranks
user convenience and developer convenience alike. Destructive operations are
soft and auditable; nothing is silently lost; history is not rewritten to make
a screen simpler or a query faster.

### 10. Consistency over preference

The documented way beats a better-but-different way. A developer's or an AI's
personal preference is not a reason to deviate. Improving the documented way is
welcome — by changing the document, not by ignoring it.

### 11. ADR before irreversible change

If reversing a decision would cost more than a day, it gets an ADR first.
Architecture is never changed silently, and never as a side effect of
implementing a feature.

### 12. The repository is the source of truth

**No architectural or business decision exists unless it is documented in this
repository.** Conversation history is never authoritative. A decision made in a
chat and not written down did not happen. Every future session — human or AI —
relies exclusively on repository documentation, never on what a previous
conversation remembers.

This is the principle that makes the others durable.

### 13. Stability over novelty

**No technology is adopted because it is new.** A technology is adopted only
when it solves a **measured** problem better than the current solution — and
only when it passes the Simplicity Budget (ADR-0014): it must address a verified
current problem *and* reduce overall system complexity.

Novelty is not evidence. Popularity is not evidence. A benchmark on someone
else's workload is not evidence. The current solution holds its ground until
something demonstrably beats it on VetFlow's own problem.

The corollary is the version policy already in force: stable LTS and stable
releases only, never previews (ADR-0003, ADR-0005). A system that lives for
years is built from things that have already survived a few.

## The authority hierarchy

When two documents disagree, the higher authority wins — always, without
debate:

```
Constitutional Principles   (this document)
        ▲  override
      ADRs                  (docs/architecture/decisions/)
        ▲  override
    Standards               (docs/architecture/standards/)
        ▲  override
    Playbooks               (.claude/playbooks/)
```

**Implementation must always conform to the highest applicable authority.** A
lower document may add detail, never contradict. A contradiction is not a
judgment call for the implementer: the lower document is wrong and is fixed at
its source — see *Repository evolution* below.

## Mandatory engineering standards

These are constitutional, not stylistic:

- **UI independence.** Feature modules never depend on PrimeNG or any future UI
  library — only on the VetFlow UI Kit (ADR-0012).
- **Strict typing.** TypeScript strict mode is on; `any` is prohibited outside
  documented exceptions. C# nullable reference types are on; analyzer warnings
  are errors.
- **Smart / Presentation separation.** Business logic belongs to Smart
  Components; reusable UI belongs to Presentation Components (ADR-0013).
- **Architecture tests are mandatory.** Every architectural rule carries a
  Reason, an ADR, an automated test, CI enforcement, and an exception process.
  **An architecture test may never be weakened, disabled, or deleted without an
  ADR and explicit owner approval.** A failing architecture test is fixed by
  changing the code — never by changing the test.
- **Quality gates are never bypassed.** The commit, push, and repository
  integrity gates in `.claude/rules/ai-governance.md` are mandatory.
  Repository integrity always outranks completing a task.

## Corollaries

- Premature optimization is prohibited; performance work is evidence-driven
  (ADR-0004). Performance budgets are engineering targets, not optimization
  licences.
- Stable LTS / stable releases only; no preview versions (ADR-0003, ADR-0005).
- Documentation-First is in force: no implementation before the relevant
  documentation is Approved (`.claude/rules/workflow.md`).

## Repository evolution

This constitution governs its own amendment:

1. **Principles** change only by owner decision, recorded in this document with
   the date and the reason.
2. **ADRs are never edited into a new meaning and never renumbered.** A
   decision that changes is *superseded* by a new ADR; the old one stays,
   marked `Superseded by ADR-NNNN`, so the reasoning trail survives.
3. **Standards** (`docs/architecture/standards/`) change through an
   owner-approved change that cites its reason. A standard that no code follows
   is deleted, not left to rot.
4. **Architecture tests** weaken only via ADR + owner approval (see above).
5. **Playbooks** are versioned with the standards they cite; when a standard
   changes, every playbook that references it is checked in the same change.
6. **Documentation drift is a defect.** When implementation and documentation
   disagree, the work stops and the conflict is resolved explicitly — the
   repository is never left holding two contradictory truths.
7. **Governance grows only by subtraction pressure.** A new governance document
   must justify its existence against merging into an existing one. If it does
   not permanently reduce engineering ambiguity, it is not created.
