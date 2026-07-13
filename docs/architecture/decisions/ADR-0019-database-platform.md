# ADR-0019: Database Platform — PostgreSQL

- **Status:** Proposed
- **Date:** 2026-07-13
- **Owner ruling:** final. Study: `docs/architecture/database-platform-study.md`.

## Context

The database platform was deliberately left open through the technology-decision
topics and decided only after a dedicated engineering study comparing PostgreSQL
and SQL Server across nineteen criteria (EF Core maturity, performance,
licensing, total cost of ownership, Docker, cloud/Windows/Linux hosting, Arabic
collation, full-text search, backup and restore, migrations, monitoring,
community, Microsoft ecosystem, SaaS and multi-tenancy readiness, AI ecosystem,
five-year scalability).

## Decision

**PostgreSQL is the database platform for VetFlow.** This decision is final.

Reasoning (owner):

- Cloud-first architecture and SaaS-first vision
- Multi-tenant future
- Docker-first deployment (ADR-0008)
- Linux-friendly
- Lower long-term operational cost
- Excellent EF Core support (Npgsql)
- Better cloud ecosystem
- No licensing limitations
- Better long-term scalability

### The constraint that matters more than the choice

**Domain and Application must never depend on PostgreSQL.** The architecture
remains **provider-independent**: PostgreSQL is the selected provider, not an
assumption baked into the system.

- **SQL belongs exclusively to Infrastructure.** No SQL execution — raw SQL,
  interpolated SQL, stored-procedure calls, provider-specific EF extensions — is
  allowed in **Domain** or **Application**. Those layers speak in ports and
  aggregates; they do not speak SQL.
- **Migrations belong exclusively to Infrastructure.** The migrations assembly,
  the schema, and every schema-evolution artifact live there and nowhere else.
- Database-specific knowledge — the provider, the SQL dialect, PostgreSQL types,
  extensions (`pgvector`, `pg_trgm`), collation configuration, connection
  handling — lives **only in Infrastructure** (ADR-0014, principle 7).
- `Application` declares ports; `Infrastructure` implements them against
  PostgreSQL.
- **Replacing the provider must remain possible without modifying Domain or
  Application.**

Enforced by architecture tests (ADR-0016): no PostgreSQL, Npgsql, or EF Core
reference may appear outside Infrastructure; no SQL execution outside
Infrastructure.

The choice is committed; the coupling is not.

## Alternatives Considered

- **SQL Server:** strongest inside the Microsoft ecosystem — native Windows
  hosting, out-of-the-box `Arabic_*` collations, mature linguistic full-text
  search, SSMS-grade backup and monitoring tooling. Rejected on the five-year
  path: the Express edition caps (10 GB per database) sit directly on the SaaS
  trajectory, and beyond them licensing becomes a per-core cost that scales with
  tenants; its container story is heavier (large, x86-64-only images) against a
  Docker-first mandate.
- **SQLite:** zero operations, but weak concurrent writes and limited schema
  evolution — a dead end for the portal/mobile/multi-tenant future.
- **MySQL/MariaDB:** free and familiar, but a community-maintained EF Core
  provider and no advantage over PostgreSQL for this stack.

## Consequences

- Npgsql is the EF Core provider; PostgreSQL runs in Docker in **every**
  environment, including development (ADR-0008).
- Database naming conventions (snake_case) and migration specifics are settled
  in `standards/backend-standards.md`.
- **Arabic search requires an explicit normalization layer** — write-time
  normalized search columns (diacritics stripped, alef/teh-marbuta forms
  unified) plus `pg_trgm` trigram indexes for typo-tolerant autocomplete. The
  study established that a premium Arabic search experience needs this layer on
  *either* engine; it is not a PostgreSQL workaround.
- `pgvector` is available if the AI-agent clients (ADR-0006) ever need embedding
  storage. Not adopted now (principle 6).
- Backup, restore, and point-in-time recovery are assembled from PostgreSQL
  tooling rather than a single vendor GUI; the operational runbook is written
  before the first production deployment.
