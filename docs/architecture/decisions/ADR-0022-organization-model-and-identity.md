# ADR-0022: Organization Model, Multi-Tenancy and Identity Foundation

- **Status:** **Accepted** <!-- owner ruling 2026-08-02: ADR and both modules accepted; OQ-IDN-1..4 ruled -->
- **Date:** 2026-08-02
- **Owner ruling:** 2026-08-02 — after the commissioned Organization Architecture
  Design Review, the owner ruled that VetFlow becomes a SaaS-ready system
  **before the first real Pilot entry**: a shared database with a tenant
  discriminator, a `Tenant → Branch → Membership → User` hierarchy, phone +
  password authentication, and scope-owned document numbering. The Pilot starts
  at a login screen, with **no temporary bypass and no fixed tenant identifier
  in code**. This ADR records that decision and its consequences. **It does not
  relitigate it.**

## Context

VetFlow was built as a single-clinic system. The review verified — against the
code, not against summaries — that the single-clinic assumption is embedded in
seventeen mapped tables, five database-global sequences, seven globally unique
indexes, one static connection string, and a singleton clinic clock. No
`TenantId`, `ClinicId` or `OrganizationId` exists anywhere, and no tenancy
ruling existed in any document.

Two facts make this the moment to decide.

**The window is open and closing.** ADR-0020 defines the Pilot's start as the
first real operational entry, which has not happened; the database is verified
empty and all five sequences are verified never-called. Every change below is
close to free today. After the first real entry each one becomes an
owner-approved migration on live clinical and financial data — a primary-key
rewrite on `product_on_hands`, a back-fill of an append-only movement ledger,
and a change to accounting series a bookkeeper can see. The binding cost is not
the migration itself: it is that **the second customer cannot be onboarded until
the retrofit lands**, so it would land under commercial pressure, on live data.

**The branch axis makes the retrofit unavoidable in every model.** The review's
decisive finding is that a branch cannot be a database boundary — stock
transfers, consolidated reporting and one login spanning branches all forbid it.
BD-PRD-005 records the owner's decision that future capabilities, explicitly
including فروع, are added **دون إعادة هيكلة**. A physical-scope column is
therefore going onto inventory and documents regardless of the tenancy ruling,
which means the marginal cost of a tenant discriminator beside it is one column
and one predicate. The earlier recommendation of database-per-clinic rested on a
"zero retrofit" argument that does not survive this; it is withdrawn here on the
record, together with the claim that shared databases break document numbering —
that was an argument against *global sequences*, which must be replaced in any
model.

## Decision

### 1. Organization hierarchy

```
Tenant  (المنشأة — the commercial customer; subscription, security and data-ownership boundary)
  ├── Branch  (الفرع — documents, numbering, staffing)
  └── User ──── Membership (User × Tenant × Role, optionally scoped to branches)
```

**Users belong to the tenant, not to a branch.** Access is expressed by a
membership row carrying the role. A single `User.BranchId` foreign key is
rejected: the owner works at every branch and a locum may work at two, so a 1:1
link forces duplicate accounts, duplicate credentials and split attribution.

**Two levels only.** The review proposed an additional `StockLocation` level; the
owner ruled two levels with no warehouse management. That ruling stays free
**only under the constraint in §11.1**, which this ADR records as binding.

### 2. Tenancy strategy — one shared database with a tenant discriminator

One PostgreSQL database serves all tenants. Every business row carries
`TenantId`. Rejected alternatives are recorded in *Alternatives considered*.

### 3. Tenant context abstraction

The Application layer never reads a tenant identifier from configuration, from a
request header, from a route parameter or from a constant. It depends on a
**tenant context abstraction** exposing the current tenant, the current branch
and the current user, and Infrastructure resolves it **from the authenticated
principal's claims** (§7). A client-supplied tenant identifier combined with one
missing filter is the leak path this rule exists to close.

This follows ADR-0010's mandate that the Application layer never couples to an
authentication provider.

### 4. Scope columns and composite foreign keys

`TenantId` and `BranchId` are added to every business table, **including child
tables** (line items, return lines, product units) even though they inherit
scope through their parent. The redundancy is deliberate and is made
unforgeable: **each child's foreign key becomes composite** — `(TenantId,
ParentId)` referencing `(TenantId, Id)` — so a cross-tenant child is physically
impossible rather than merely forbidden.

Denormalising the discriminator also lets row-level security apply without
joins, lets the composite unique indexes exist locally, and makes the
architecture test of §8 a simple reflection check.

**Scope assignment:**

| Scope | Tables |
|---|---|
| Platform-global (no discriminator) | `units`, `product_natures` — shared vocabulary, seeded by migration |
| Tenant | `products`, `product_units`, `categories`, `manufacturers` |
| Tenant + Branch | `purchase_invoices`, `purchase_line_items`, `purchase_returns`, `purchase_return_lines`, `sales_invoices`, `sales_line_items`, `sales_returns`, `sales_return_lines`, `inventory_batches`, `inventory_movements`, `product_on_hands` |

`product_on_hands` changes its primary key from `ProductId` to
`(TenantId, BranchId, ProductId)`. This is the single most expensive structure
to change later and the principal reason this decision cannot wait.

### 5. Uniqueness scope

All seven existing unique indexes are rescoped. None remains global.

| Index | Today | Becomes |
|---|---|---|
| `ix_products_internal_code` | `internal_code` | `(tenant_id, internal_code)` |
| `ix_categories_name_unique` | `search_text` | `(tenant_id, search_text)` |
| `ix_manufacturers_name_unique` | `search_text` | `(tenant_id, search_text)` |
| `ix_purchase_invoices_number` | `number` | `(tenant_id, branch_id, number)` |
| `ix_sales_invoices_number` | `number` | `(tenant_id, branch_id, number)` |
| `ix_purchase_returns_number` | `number` | `(tenant_id, branch_id, number)` |
| `ix_sales_returns_number` | `number` | `(tenant_id, branch_id, number)` |

Manufacturers stay tenant-scoped. A centrally curated manufacturer list is a
product feature, not an architectural requirement; if it is ever wanted it is
added as a platform catalogue that tenants **import from**, which avoids both
cross-tenant writes and a later cross-tenant deduplication.

The GIN trigram indexes on `categories.search_text` and
`manufacturers.search_text` must be rescoped alongside their unique B-tree
counterparts, or search results cross tenants at the index level.

### 6. Document numbering — scope-owned, gapless, format preserved

The five database-global sequences are **dropped**. Numbers are allocated from a
counter row per `(tenant, branch, series)` with `UPDATE … RETURNING`, **inside
the transaction that inserts the document**.

| Series | Scope | Why |
|---|---|---|
| `PRD-` | Tenant | A product is a catalog item shared across branches |
| `PUR-` `SAL-` `PRT-` `SRT-` | Branch | These record events that happen at a place |

**Format is preserved exactly** — same prefixes, same six-digit zero padding.
With one seeded branch the output is byte-identical to today. No branch-code
segment is introduced: there is one branch, and because the counter is already
per-branch, adding a display segment when a second branch opens is a
forward-going generation rule, **not a data migration**.

**Numbering becomes gapless (owner ruling, 2026-08-02).** `nextval` does not roll
back, so today a failed save burns a number permanently. A counter row inside the
transaction does roll back. This is achievable here because no document has a
delete or cancel path (DEC-INV-037). The cost — brief serialization of
simultaneous creations within one series — is accepted as irrelevant at clinic
volume.

### 7. Identity foundation — phone + password, JWT, minimal by ruling

Authentication is **phone number + password only**. Explicitly excluded by owner
ruling: email, username, OTP, MFA, self-registration, forgot/reset password,
invitations, user-management UI, permissions UI, refresh tokens, remember-me and
session-management UI.

- **Phone numbers are globally unique across the entire system** (owner ruling,
  2026-08-02, OQ-IDN-4). Identity resolution must not require knowing the tenant
  first, and one person may hold memberships in two clinics. See §12.14.
- **Access tokens live 12 hours** (owner ruling, OQ-IDN-1): one clinic working
  day, expiring between days. With no refresh token and no "remember me",
  expiry means signing in again.
- **No account lockout during the Pilot** (owner ruling, OQ-IDN-2): with one
  user, no reset path and no user administration, a lockout would be a lock
  with no key. **This is a Pilot-scoped ruling, not a permanent invariant** —
  §12.15.
- **Tokens:** a single JWT access token, no refresh rotation. This is the
  specification ADR-0010's Consequences deferred to "engineering documentation
  for owner review", and it amends ADR-0010 accordingly.
- **Password hashing** uses ASP.NET Core Identity's `PasswordHasher<T>` behind
  the ADR-0010 abstraction. ADR-0010 §1 already permits Identity in the MVP; the
  Application layer sees only provider-neutral concepts. Plain-text storage is
  prohibited.
- **Roles** are carried by the membership, not the user: `Owner` and `Cashier`
  per BD-PRD-003. Only the Owner is seeded; no administration surface exists.
- **Claims** carry the user, the tenant and the branch; the tenant context of §3
  is resolved from them and from nowhere else.

### 8. Enforcement — the mitigations are mandatory, not best-effort

A shared database is only defensible with the leak path closed by construction.
All four are required, and the recommendation of §2 is void without them:

1. **EF Core global query filters** on every tenant-scoped entity. There are
   currently none.
2. **PostgreSQL row-level security** as a database-level second net.
3. **An architecture test** asserting that every business entity declares the
   discriminator and every tenant-scoped entity has a filter — written in the
   existing reflection-driven idiom of `tests/VetFlow.ArchitectureTests/`.
4. **Composite foreign keys** per §4.

This is affordable here because the codebase has **no raw SQL reads at all**:
the only raw SQL is sequence allocation, which §6 removes. Every read goes
through EF, so a global filter genuinely covers the read surface.

### 9. The clinic clock becomes tenant-resolved

`ClinicClock` is a singleton resolving one configured time zone once at
construction, and expiry safety decisions depend on it (BR-INV-059/060). Under
multi-tenancy it is correct for at most one tenant, and it contradicts ADR-0007's
requirement that tenant-specific localization remain possible. The clock becomes
**tenant-resolved**; the time zone becomes a tenant attribute rather than a
deployment-wide configuration value. BR-INV-060's prohibition on deriving the
date from UTC, server time or the user's device is **unchanged** — only its
source moves.

### 10. Seed — one real clinic, not a fixture

Tenant **Happy Pets Clinic**, branch **Main Branch**, owner user **Clinic Owner**
with phone `01001127204`. The seeded owner belongs to the seeded tenant and
branch. Onboarding any future clinic is: create tenant → create branch → create
owner.

### 11. Constraints this ADR makes binding

**11.1 — A future warehouse is a Branch, not a new level.** The owner's
two-level hierarchy is additive *only* under this constraint. Introducing a
stock-location level below Branch would reinstate the primary-key change on a
live `product_on_hands`, which is precisely the migration this ADR exists to
avoid. Any future design proposing one must amend this ADR first.

**11.2 — No fixed tenant identifier in business logic**, in any layer, at any
phase, including the Pilot.

## 12. Future SaaS Constraints — invariants that must never be violated

*(Added by owner ruling, 2026-08-02.)*

These are **invariants, not guidelines**. Each one is cheap to hold today and
expensive-to-impossible to restore after real tenant data exists. Violating any
of them silently converts VetFlow back into a single-clinic system with extra
columns. **A change that breaks one of these amends this ADR first, or it does
not land.**

Each entry states the invariant, why it exists, and **what breaks if it is
violated** — because an invariant whose failure mode is not written down is not
enforceable.

### Data ownership

**12.1 — Every business row belongs to exactly one tenant.** The only exceptions
are the platform-global reference tables named in §4 (`units`,
`product_natures`). Adding a business table without a discriminator is a defect,
not a shortcut.
*Breaks:* the row becomes unattributable and invisible to per-tenant export,
deletion and restore. There is no correct back-fill for it later.

**12.2 — A child row can never belong to a different tenant than its parent, and
this is enforced by the database, not by code.** Composite foreign keys per §4.
*Breaks:* one wrong join or one wrong identifier in a request body silently
attaches another tenant's line item to this tenant's invoice. Code review does
not catch this reliably; a composite key makes it impossible.

**12.3 — Uniqueness is never global for tenant-owned data.** Every unique
constraint on a tenant-scoped table leads with the tenant (§5).
*Breaks:* tenant B cannot use a name tenant A used. This surfaces as an
inexplicable validation failure during onboarding, and it **blocks the second
customer** rather than degrading gracefully.

**12.4 — Reference data is platform-global and never written by a tenant.** If
tenant-specific units are ever needed, the discriminator is added as *nullable*
where NULL means global — an addition, never a split.
*Breaks:* one tenant edits shared vocabulary and changes another tenant's data.

### Tenant resolution and isolation

**12.5 — The tenant is resolved from authenticated claims and from nowhere
else.** Never configuration, header, route parameter, query string or request
body — at any phase, including Pilot and local development.
*Breaks:* a client-controlled tenant identifier combined with one missing filter
is a complete cross-tenant read. This is the single highest-severity failure
mode in the system.

**12.6 — No fixed tenant identifier exists in any layer.** No constant, no
default, no "pilot tenant" fallback, no environment variable.
*Breaks:* the fallback becomes load-bearing, and the first real second tenant
silently writes into the first one's data.

**12.7 — The four isolation mitigations may never be weakened, disabled or
skipped:** EF global query filters · PostgreSQL row-level security · the
architecture test asserting discriminator and filter coverage · composite
foreign keys. **§2's choice of a shared database is void without all four** — if
one is removed, the correct response is to revisit §2, not to proceed.
*Breaks:* the shared-database decision loses the basis on which it was made.

**12.8 — No response ever aggregates across tenants.** Platform-level analytics,
if ever built, is a separate, explicitly-authorized read path — never a business
endpoint that happens to omit a filter.
*Breaks:* a leak that looks like a feature and therefore survives review.

### Structure

**12.9 — A future warehouse is a Branch, not a new level below it.** Restates
§11.1 as a permanent invariant.
*Breaks:* the primary-key change on a live `product_on_hands` returns — the most
expensive migration in the system, and the reason this ADR exists.

**12.10 — Access is derived from membership, never from a field on the user.**
*Breaks:* a person working at two clinics needs two accounts, which splits
attribution, credentials and audit history irreversibly.

**12.11 — Inventory is branch-scoped and the movement ledger stays
append-only.** No process rewrites historical movement rows to add scope.
*Breaks:* ADR-0020's guarantee, and every historical location becomes a
fabrication.

### Numbering

**12.12 — Document numbers are never allocated from a global database
sequence.** Counters are scope-owned per §6.
*Breaks:* tenant B's first invoice is not number one — commercially
indefensible and, for invoices, an audit-trail problem.

**12.13 — Numbering scope may deepen but never widen.** Branch → a finer scope
is additive; branch → tenant is a live-data migration of accounting series.
*Breaks:* a bookkeeper sees the series change origin mid-book.

### Identity

**12.14 — Phone numbers are globally unique across the entire system** (owner
ruling, OQ-IDN-4). Uniqueness is **not** scoped to the tenant.
*Breaks:* sign-in would have to ask which clinic before knowing who the user is,
which either re-introduces a client-supplied tenant identifier at the least
authenticated moment in the system (violating 12.5) or forces one human to hold
several accounts (violating 12.10).

**12.15 — Pilot-scoped rulings are labelled as such and expire on review.** No
lockout (OQ-IDN-2), the seeded password equalling the phone number, and the
absence of a password-reset path are **accepted for a single-user Pilot**. They
are **not** invariants, and each must be revisited before a second tenant is
onboarded.
*Breaks:* a temporary concession silently becomes the permanent security
posture — the most common way a pilot's compromises reach production.

### Operations

**12.16 — Onboarding stays a database transaction:** create tenant → create
branch → create owner. It never becomes infrastructure provisioning.
*Breaks:* onboarding acquires partial-failure states and needs its own
monitoring — the cost that ruled out database-per-clinic in the first place.

**12.17 — The schema is identical whether a tenant is pooled or runs in a
dedicated database.** This — and only this — keeps the hybrid escape hatch free.
*Breaks:* moving a demanding customer to their own database becomes a redesign
instead of a deployment change, and the alternative preserved in
*Alternatives considered* is lost.

## Consequences

### Accepted, with eyes open

- **A cross-tenant data leak becomes a possible class of defect** that would not
  exist under database-per-clinic. It is accepted because §8 closes it by
  construction and makes it build-failing. If §8 is ever weakened, this decision
  should be revisited rather than quietly carried.
- **Single-tenant restore becomes harder.** Restoring one clinic from a shared
  database means restoring to a Neon branch and copying that tenant's rows back.
  `backup-restore-runbook.md`, already flagged as wrong for the cloud path, must
  be rewritten before any real data exists.
- **The seeded password equals the phone number.** Accepted for a single-user
  Pilot by owner ruling; it should be changed before a second clinic exists.
- **This is an Epic, not a column.** It touches all seventeen tables, five
  numbering sites, seven unique indexes, every route's contract semantics, the
  DbContext registration and the clinic clock.

### Mechanical consequences

- The Pilot begins at a login screen. There is no anonymous access path.
- `ActorName` (BR-INV-066, DEC-INV-030) is superseded as the attribution
  mechanism: every operation now belongs to an authenticated user. Existing
  free-text values remain readable.
- ADR-0021's phase ordering changes: authentication now precedes the Pilot
  rather than following Phase 1. Its topology — one Render service, one Neon
  database — is **confirmed** by §2 rather than amended.
- Onboarding a clinic is one transaction, not an infrastructure workflow.

### Unchanged

- Every business rule governing catalog, purchasing, sales and inventory
  behaviour, including FEFO, expiry, the write kernel and the movement ledger.
  With one seeded tenant and one seeded branch, every scope predicate is a
  no-op and behaviour is byte-identical.
- GUID primary keys, which are already correct for future synchronization.
- ADR-0020's bar and ADR-0019's platform choice.

## Alternatives considered

- **Database per clinic.** Strongest isolation, and it was the earlier
  recommendation. Rejected: it does not avoid the branch retrofit; onboarding
  becomes a distributed provisioning workflow instead of a transaction; startup
  migration becomes a serial loop over N databases that the current bootstrap
  cannot express and that grows with customers; one service must hold N
  connection pools; partial migration failure leaves tenants on divergent schema
  versions running one binary; and cross-tenant metering, support and analytics
  become fan-outs. Its costs grow with commercial success. Its genuine advantage
  is recoverable per-customer through the hybrid path below.
- **Schema per tenant.** Rejected outright: N × DDL per release plus catalog
  bloat, with none of the isolation benefit that would justify it.
- **Pooled by default with a dedicated database for a specific tenant.** Not
  rejected — **preserved**. It remains available precisely because §4 puts
  `TenantId` in the schema unconditionally, so a dedicated deployment runs the
  identical schema. Choosing database-per-clinic today would have foreclosed the
  shared model permanently; this ordering does not.
- **Deferring the decision until after the Pilot.** Rejected: it converts a free
  change into a migration on live financial data, and blocks the second customer
  behind it.

## Follow-ups this ADR creates

1. Two new modules with the full document set: `organization/` and `identity/`.
2. Amendments: **BR-INV-066** (users and authentication now exist — amended in
   place with the superseded wording preserved), **ADR-0010** (token mechanism
   specified), **ADR-0021** (phase ordering).
3. GLOSSARY rows for every new term.
4. `backup-restore-runbook.md` rewritten for per-tenant restore before real data
   exists.
5. Forced password change before a second clinic is onboarded.
