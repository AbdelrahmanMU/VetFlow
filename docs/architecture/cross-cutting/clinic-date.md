# Cross-Cutting: The Clinic Local Date

> Status: **Approved (2026-08-03)** — owner ruling **OQ-DSH-2**: «Move the
> clinic-local "today" definition to a shared cross-cutting location so every
> module uses the same single source of truth.»
> **This document is now the home of that definition for the whole product.**

---

## 1 · The rule

**Every business decision that depends on "what day is it" uses the *clinic
local date* — one value, from one source, for the whole request.**

- **The reference date is the clinic's own local date**, never a UTC date.
- **Prohibited as sources, explicitly:** UTC · the server's or the operating
  system's local time · **the user's browser or device time** · any value that
  varies by machine, session or caller.
- **The zone comes from the tenant** (`Tenant.TimeZone`) — **DEC-ORG-007**,
  **AC-ORG-009**, ADR-0022 §9.
- **An unknown zone is not silence:** the system **must not run** with an
  unresolvable zone, and **a silent fallback to UTC is forbidden**. A newly
  seeded clinic starts from the configured `Clinic:TimeZone` value (ADR-0022
  §10), which still refuses to boot when absent or unresolvable.

**Why it is a business rule and not a formatting detail:** whether a medicine
may be sold **must not depend on which server answered the request**
(**DEC-INV-021** made expiry a safety decision, not a display filter).

## 2 · Where this rule came from, and what moved

**Nothing about the rule changed. Only its home did.**

The definition was ruled by the owner on **2026-07-30** and recorded as
**BR-INV-059** (the reference date) and **BR-INV-060** (the single source),
inside `docs/modules/inventory/business-rules.md`. That was correct while
inventory owned every date decision in the system.

It stopped being correct when the **Dashboard** needed «today» for a **Sales**
figure (`REQ-DSH-007`). BR-INV-059 **scopes itself by its own text** to «كل
مقارنات التواريخ **في وحدة المخزون**» and enumerates the rules it governs — and
a Sales figure is in neither that list nor that module. That left two bad
options and one good one:

| Option | Why it was rejected |
|---|---|
| Say the Dashboard "adopts" BR-INV-059 | **Silently widens an Approved rule's scope** — a change of its meaning, which `ai-governance.md` forbids outright |
| Write a second date rule for the Dashboard | **Creates a second definition of "today"** — exactly what BR-INV-060's «مصدر واحد لا غير» exists to prevent |
| **Lift the definition to a cross-cutting home** | **The owner's ruling (OQ-DSH-2).** One definition, one source, every module reads it |

**Recorded consequence:** `IClinicClock` was **already** product-wide
infrastructure rather than Inventory-owned code — its own documentation called
itself "the single reference date for **every** date decision in the system".
**This ruling makes the documentation match what the code already did.**

## 3 · What is unchanged — read this before assuming otherwise

- **BR-INV-059 and BR-INV-060 keep their identifiers.** They are **not
  annulled, not renumbered, and not reused.** Their text is **preserved in
  place**, now pointing here as the authoritative home (the BR-CAT-020 /
  BR-INV-066 precedent).
- **Every rule they govern is unaffected in meaning:** BR-INV-013
  (expiring-soon horizon) · BR-INV-022 (batch-viewer expiry filter) ·
  BR-INV-033/036 (expiry monitoring) · BR-INV-050 (saleable batch).
- **`ExpiryDate` is still the last saleable day** — a batch expiring **today**
  is **still sellable** (BR-INV-059 §1/§2). Unchanged.
- **No code behaviour changes with this ruling.** No migration, no new field,
  no new abstraction.

## 4 · Who uses it

| Module | Use | Rule |
|---|---|---|
| **Inventory** | expired / expiring-soon / saleable-batch decisions | BR-INV-013 · BR-INV-022 · BR-INV-033/036 · BR-INV-050 |
| **Dashboard** | «today» in today's sales, and the expiry counts it composes | BR-DSH-003 · REQ-DSH-007 |
| **Sales** | the dashboard-facing today's-sales read | BR-SAL-020 · REQ-SAL-006 |

**Any future module that needs "today" reads it from here** — and adds a row
above rather than writing its own rule.

## 5 · Implementation note (not a business rule)

The single source is **`IClinicClock.Today`** (`VetFlow.Application.Common`),
implemented over `TimeProvider` and the tenant's zone
(`VetFlow.Infrastructure.Common.ClinicClock`, `TenantTimeZones`). **The clock is
a singleton that resolves the tenant per access** — a scoped registration would
pin one request's tenant into the object graph.

**Storage and configuration of the value belong to implementation and are not
ruled here** (BR-INV-060's own wording). **No settings screen is designed** —
the Settings module remains undocumented, and may surface the value later
**without changing this rule**.
