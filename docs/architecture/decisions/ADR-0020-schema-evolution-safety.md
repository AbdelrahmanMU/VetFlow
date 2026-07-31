# ADR-0020: Schema Evolution Safety — No Destructive Migrations Once Real Data Exists

- **Status:** Accepted <!-- owner acceptance, 2026-07-31 -->
- **Date:** 2026-07-31
- **Owner ruling:** 2026-07-31 — proposed and accepted on the same day. The rule
  text in §Decision, the Pilot definition in §When the Pilot begins, and the
  checklist in §Pilot Transition Checklist are **the owner's own wording**. This
  ADR records an owner ruling; it does not propose one.

## Context

Epic 2 capability C1 (Movement Ledger) absorbed `InventoryConsumption` into the
unified ledger. Its migration `20260731003637_InventoryMovementLedger` therefore
**drops the `inventory_consumptions` table**, discarding every row it held.

The owner approved C1 and accepted that migration **on the explicit ground that
no real data existed yet** — the only rows lost were created by this project's
own browser verification, so no clinic data was at risk.

The owner then ruled a standing constraint for everything after that point. The
governing goal already recorded for Epic 2 is **pilot readiness (جاهزية التجربة
الأولى)** — `docs/modules/inventory/decisions.md` §«قرارات Epic 2» — so the
pilot was already a named phase in this repository, but **the moment it begins
had never been defined**. This ADR defines it.

Existing migration standards (`STD-BE-041`/`042`, sourced to ADR-0019) govern
*how* a schema change ships and *where* migrations live. **Neither constrains
what a migration is permitted to destroy.** That gap is what this ADR closes.

## Decision

> **No destructive migrations are permitted once real pilot or production data
> exists, unless an explicit owner-approved migration plan has been approved.**

The rule turns on **the existence of real data**, not on a release event. Before
real data exists, a destructive migration is permitted — the C1 migration was
accepted under exactly that clause, and is recorded as a spent, one-off
exception in `docs/modules/inventory/decisions.md`.

Once real data exists, the default is the **non-destructive path**: add the new
shape, migrate the values, retire the old shape only once nothing reads it.

**The escape hatch is the owner's, and only the owner's.** A destructive
migration may still ship, but only behind an **explicit owner-approved migration
plan** — approved *before* the migration ships, not reconstructed afterwards.
The plan is what the owner approves; this ADR does not prescribe its form, and
no AI contributor may approve one, waive one, or infer one from silence.

### What "destructive" means

A migration is destructive when applying it **discards data that exists in the
database and is not recoverable from what the migration itself writes** —
dropping a table, dropping a column, narrowing a type or length such that values
are truncated, or deleting rows.

Renames and widening type changes are **not** destructive: the values survive.

### When the Pilot begins

> **The Pilot officially begins when the first real operational clinic data is
> intentionally entered for business use.**

This is **the transition point between development data and operational data**.

Two consequences follow from the wording, and both are deliberate:

- **It is an act, not a date.** No release, deployment, or version marks it —
  entering real clinic data for real business use does.
- **"Intentionally … for business use"** excludes seed data, demo data, and the
  rows created by browser or performance verification, however realistic they
  look. Those remain development data no matter when they were written.

### Pilot Transition Checklist

Completed at the transition, before real operational data is entered:

- [ ] All migrations applied.
- [ ] Database backup completed.
- [ ] Seed data finalized.
- [ ] No destructive migrations pending.
- [ ] Current schema tagged.

### What this decision does not do

- It does not weaken or replace `STD-BE-041`/`042`; it constrains migration
  *content*, which those rules never addressed.
- It does not mandate a backup, restore, or recovery *procedure*. The checklist
  requires a backup **at the transition**; ADR-0019 still defers the operational
  runbook to "before the first production deployment", and this ADR neither
  anticipates nor pre-empts it.
- It creates no retroactive obligation. Migrations already in the repository —
  including `20260731003637_InventoryMovementLedger` — are unaffected.

## Alternatives Considered

These are engineering alternatives to the recorded ruling, stated for the record.
**The owner did not rule on them**; they were not put to the owner.

- **An absolute prohibition with no escape hatch.** This was the first draft of
  this ADR, and the owner replaced it: an unconditional ban would eventually
  force either a rule violation or a permanently deformed schema. The
  owner-approved plan keeps the decision with the person who owns the data.
- **Permit destructive migrations with a mandatory pre-migration backup and no
  approval step.** Cheaper, but it substitutes a procedural safeguard for a
  judgement call, and the restore procedure it would depend on does not exist yet
  (ADR-0019 defers the runbook).
- **Mandate expand-and-contract as a named required pattern.** This is the usual
  way to satisfy the rule, but naming one pattern as mandatory would decide more
  than the owner ruled. The rule constrains the outcome (no data loss); the
  pattern stays an implementation choice.

## Consequences

- **A new standard row, `STD-BE-051`**, carries this rule into
  `standards/backend-standards.md` beside the other migration rules, and a row in
  `architecture/overview.md` puts it on the map of what is decided where.
- **The rule is dormant until real data exists, and is therefore enforced by
  review, not by a test.** No architecture test or CI script is added now:
  automating a check for a condition that has not occurred would put a
  permanently inert — or falsely red — gate into the sweep. **When the Pilot
  Transition Checklist is executed, `STD-BE-051` becomes Semi-Automatic**: a
  `DropTable`/`DropColumn`/narrowing-`AlterColumn` scan over the migrations
  folder, failing unless an owner-approved plan is recorded. This consequence is
  where that obligation lives so it is not lost.
- **"No destructive migrations pending" in the checklist is a real gate, not a
  formality.** Anything destructive still unshipped at the transition must land
  before it — or wait for a plan afterwards.
- **Epic 2's remaining capabilities (C2–C6) are unaffected.** They are additive
  over the ledger by construction — C2 is a projection, and C3–C6 add tables and
  columns. No destructive change is currently foreseen in the Epic.
- Any future absorption of the kind C1 performed — collapsing an entity into
  another — must, once real data exists, carry its data across or ship behind an
  approved plan.
