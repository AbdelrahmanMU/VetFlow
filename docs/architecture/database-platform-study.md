# Architecture Decision Study: Database Platform — PostgreSQL vs SQL Server

> **Status: Draft — study only.** This document ends with a recommendation.
> It does **not** record a decision. The database platform remains an open
> decision until the owner rules; the outcome will then be recorded as an ADR.

- **Date:** 2026-07-13
- **Requested by:** Owner (Topic 3 review, decision 1)
- **Scope:** Engineering-focused comparison of PostgreSQL and Microsoft SQL
  Server as the primary operational database for VetFlow.

## Fixed context (approved constraints this study assumes)

- EF Core is the only data-access layer; no hybrid ORM (ADR-0004).
- Everything runs in containers from day one (ADR-0008).
- Product API with future mobile/portal/AI-agent/integration clients
  (ADR-0006); future multi-tenant SaaS must remain possible (ADR-0007).
- Solo owner + AI development; maintainability and low operational burden
  outrank raw capability (`principles.md`).
- MVP scale: one clinic, two users — either engine is orders of magnitude
  beyond MVP needs. The comparison therefore weighs **cost, operations, and
  the five-year path**, not MVP feasibility.

## Summary matrix

| # | Criterion | PostgreSQL | SQL Server | Edge |
|---|---|---|---|---|
| 1 | EF Core maturity | First-class (Npgsql) | First-class (reference provider) | Tie |
| 2 | Performance | Excellent at this scale | Excellent at this scale | Tie |
| 3 | Licensing | Free, permissive, all features | Free only within Express limits; paid beyond | **PostgreSQL** |
| 4 | Total cost of ownership | Hosting + admin time only | License and/or managed-tier premium | **PostgreSQL** |
| 5 | Docker experience | Small official images, x64+ARM | Large images, x86-64 only | **PostgreSQL** |
| 6 | Cloud hosting | Managed on every cloud + budget hosts | Excellent on Azure; costlier elsewhere | **PostgreSQL** (breadth) |
| 7 | Windows hosting | Supported, second-class culturally | Native, first-class | **SQL Server** |
| 8 | Linux hosting | Native, first-class | Supported since 2017, heavier | **PostgreSQL** |
| 9 | Arabic collation | ICU collations; needs deliberate setup | Rich built-in Arabic_* collations | **SQL Server** (convenience) |
| 10 | Full-text search | Snowball Arabic stemmer + pg_trgm fuzzy | Built-in Arabic word breakers/stemmers | Split (see §10) |
| 11 | Backup & restore | pg_dump / WAL PITR / pgBackRest | Polished native backups + SSMS GUI | **SQL Server** (self-hosted GUI) |
| 12 | Migration experience | EF migrations; transactional DDL | EF migrations; transactional DDL | Tie |
| 13 | Monitoring ecosystem | Strong OSS stack (exporters, pg_stat_statements) | Query Store + SSMS, very integrated | Slight **SQL Server** |
| 14 | Community | Largest OSS DB community, top survey rankings | Large but vendor-centric | **PostgreSQL** |
| 15 | Microsoft ecosystem | Good (Npgsql, Azure PG) | Same-vendor alignment with .NET/Azure | **SQL Server** |
| 16 | Future SaaS readiness | License-free scale-out economics | Viable, but cost scales with tenants | **PostgreSQL** |
| 17 | Multi-tenancy readiness | Schemas, RLS, per-tenant DBs at no cost | RLS, elastic pools (Azure) | Slight **PostgreSQL** |
| 18 | AI ecosystem compatibility | pgvector is the de-facto standard | Native vector type new in SQL Server 2025 | **PostgreSQL** (today) |
| 19 | Five-year scalability | Replicas/partitioning, no license wall | Same, but paid tiers on the path | **PostgreSQL** |

## Detailed analysis

### 1. EF Core maturity
The SQL Server provider is the EF team's reference implementation. The
PostgreSQL provider (Npgsql) is equally mature: releases track EF Core majors
from day one, full LINQ translation and migrations support, and it
additionally exposes PostgreSQL-specific capabilities (arrays, JSON columns,
range types) should we ever want them. Neither constrains ADR-0004. **Tie.**

### 2. Performance
At clinic-scale OLTP (thousands of rows per day, two concurrent users) both
engines are massively over-provisioned; performance will be decided by schema
and query design, not the engine. At larger scale both are proven at volumes
far beyond any realistic VetFlow future. **Tie.**

### 3. Licensing
PostgreSQL: permissive open-source license, every feature free forever, no
edition matrix. SQL Server: **Express** is free but capped (10 GB per
database, ~1.4 GB buffer-pool memory, ≤4 cores) — sufficient for MVP but a
hard ceiling on the SaaS path; beyond it, Standard/Enterprise licensing is
priced per core or server+CAL (list prices in the thousands of USD), with
real complexity around containers and virtualization. **PostgreSQL, decisively.**

### 4. Total cost of ownership
PostgreSQL's five-year cost is hosting plus administration time. SQL Server
adds either license fees, the operational risk of outgrowing Express, or a
managed-tier premium (Azure SQL). Administration effort for a small,
containerized, mostly-default installation is comparable for both.
**PostgreSQL.**

### 5. Docker experience
PostgreSQL's official image is small (tens of MB for alpine variants), starts
in seconds, runs natively on x86-64 and ARM64, and is the most battle-tested
database container in existence. SQL Server's Linux image is ~1.5 GB,
x86-64-only (ARM machines — e.g. Apple Silicon of a future developer — need
emulation or workarounds), slower to start, and requires EULA/SA-password
bootstrapping. With ADR-0008 making containers the daily developer
experience, this difference is felt every day. **PostgreSQL, strongly.**

### 6. Cloud hosting
PostgreSQL has first-party managed offerings on every major cloud (Azure
Database for PostgreSQL, AWS RDS/Aurora, GCP Cloud SQL) plus a deep budget
tier (DigitalOcean, Hetzner, Supabase, Neon) — valuable for a cost-sensitive
single-clinic deployment. SQL Server's managed story is excellent on Azure
(Azure SQL is arguably the best managed database product anywhere) but thin
and license-burdened elsewhere. If hosting is not yet committed to Azure,
PostgreSQL preserves the most freedom. **PostgreSQL for breadth; SQL Server
if all-in on Azure.**

### 7. Windows hosting
SQL Server on Windows is its native habitat with decades of tooling.
PostgreSQL runs supported on Windows but the ecosystem treats Linux as
primary. Note: under ADR-0008 the database runs in a container in every
environment, which largely neutralizes the host-OS question — a Windows
development machine runs either engine via Docker identically. **SQL Server,
but with reduced weight given ADR-0008.**

### 8. Linux hosting
The inverse: PostgreSQL is native; SQL Server on Linux is supported and solid
since 2017 but heavier and less common in production fleets. Linux is the
realistic deployment target for cost-effective hosting. **PostgreSQL.**

### 9. Arabic collation
SQL Server ships rich, mature `Arabic_*` collations (case/accent-sensitivity
variants selectable per column/database) with long production history in the
region — it simply works out of the box. PostgreSQL handles Arabic correctly
via ICU collations (`ar`, `ar-EG`), but *insensitive* matching
(case/diacritic-insensitive) requires nondeterministic ICU collations, which
historically restrict some pattern-matching operators; the standard
mitigation is write-time normalized search columns (strip diacritics, unify
alef/teh-marbuta forms). Important nuance: a premium Arabic search experience
(REQ-level duplicate matching, autocomplete) will need that explicit
normalization layer **on either engine** — relying on raw engine collation is
not sufficient for Arabic product search anyway. **SQL Server for
out-of-the-box convenience; near-parity in practice once the normalization
layer VetFlow needs regardless is built.**

### 10. Full-text search
SQL Server Full-Text Search includes Arabic word breakers and stemmers and is
available even in Express (with Advanced Services). PostgreSQL's built-in FTS
has an Arabic Snowball stemmer (basic morphology only), but adds `pg_trgm`
trigram indexes — the best fit among all these options for the catalog-style
need we actually documented (short Arabic product names, typo-tolerant
lookup, autocomplete), where linguistic stemming matters less than fuzzy
matching. **SQL Server wins linguistic FTS; PostgreSQL wins fuzzy/autocomplete
search. For VetFlow's documented needs, split — slight PostgreSQL.**

### 11. Backup & restore
SQL Server's native BACKUP/RESTORE plus SSMS point-and-click is the most
approachable self-hosted story, including point-in-time restore via log
backups. PostgreSQL offers pg_dump (logical), pg_basebackup + WAL archiving
(PITR), and mature tools (pgBackRest); all scriptable and
container-friendly, but assembled rather than integrated. On managed cloud
tiers both are automated equally. **SQL Server for self-hosted GUI
operations; tie on managed hosting.**

### 12. Migration experience
EF Core migrations are the single schema mechanism (ADR-0004) and are fully
supported on both providers. Both engines support transactional DDL, so a
failed migration rolls back cleanly (a real advantage both share over
MySQL-family engines). **Tie.**

### 13. Monitoring ecosystem
SQL Server: Query Store, DMVs, and SSMS give a solo operator an excellent
integrated view with zero assembly. PostgreSQL: `pg_stat_statements`,
pgAdmin, and a rich Prometheus/Grafana exporter ecosystem — more powerful in
aggregate, but assembled from parts. **Slight SQL Server for a solo
operator; PostgreSQL if a metrics stack is deployed anyway.**

### 14. Community
Both are enormous. PostgreSQL has topped developer-survey "most used /
most admired database" rankings for years, has the larger open-source
extension ecosystem, and every new data tool targets it first. SQL Server's
community is deep but orbits one vendor. **PostgreSQL.**

### 15. Microsoft ecosystem
SQL Server is the same-vendor default for ASP.NET Core: reference EF
provider, seamless Azure integration, unified documentation and support
story. PostgreSQL is a first-class citizen in .NET in practice (Npgsql is
partly maintained by Microsoft-employed engineers, Azure offers managed
PostgreSQL), but SQL Server is the path of least resistance inside the
Microsoft world. **SQL Server.**

### 16. Future SaaS readiness
The SaaS question is dominated by unit economics: with PostgreSQL, adding
tenants adds only hosting cost, and per-tenant databases/schemas are free to
multiply across any provider. With SQL Server, tenant growth eventually
crosses the Express ceiling onto per-core licensing or Azure SQL elastic
pools (a genuinely good SaaS product, but one that binds the business to
Azure pricing). **PostgreSQL.**

### 17. Multi-tenancy readiness
Technically close: both offer row-level security; PostgreSQL adds cheap
schema-per-tenant and database-per-tenant patterns plus scale-out options
(read replicas, Citus) without license cost; SQL Server's equivalent patterns
are strongest inside Azure SQL. **Slight PostgreSQL, driven by economics
rather than features.**

### 18. AI ecosystem compatibility
For the planned AI-agent clients (ADR-0006) and any future
embedding/semantic-search features: `pgvector` is the de-facto industry
standard for vector storage, supported by every managed PostgreSQL offering
and every major AI framework (Semantic Kernel, LangChain, etc.). SQL Server
gained a native vector type in SQL Server 2025 / Azure SQL — credible but
new, with a smaller integration surface today. General agent tooling (MCP
servers, schema introspection) supports both well, with PostgreSQL more
common in AI-adjacent stacks. **PostgreSQL today; gap likely to narrow.**

### 19. Five-year scalability
Either engine technically covers any realistic five-year VetFlow trajectory
(clinic workloads are small even multiplied by many tenants). The binding
constraints on the five-year path are cost, hosting freedom, and operational
simplicity — PostgreSQL scales without a license wall; SQL Server's clean
scaling path leads through paid editions or Azure SQL. **PostgreSQL.**

## Recommendation (not a decision)

**The study recommends PostgreSQL.**

Rationale in one paragraph: at MVP scale the engines are equivalent, so the
choice is decided by the five-year path — and there PostgreSQL's advantages
are structural (no licensing wall, free multi-tenant economics, hosting
freedom, the best Docker experience, the standard AI/vector ecosystem), while
SQL Server's advantages are conveniences (out-of-box Arabic collations, SSMS,
same-vendor alignment) that PostgreSQL can match with modest, one-time
engineering effort — effort that Arabic search quality would demand on either
engine.

**SQL Server would be the better choice if** the owner decides any of the
following outweigh the above: hosting will be committed to Azure/Windows
regardless of cost; SSMS-style integrated GUI administration is a priority
for self-hosted operations; or out-of-the-box Arabic linguistic full-text
search is a hard requirement rather than a nice-to-have.

**The final decision belongs to the owner.** Once ruled, the outcome will be
recorded as an ADR referencing this study.
