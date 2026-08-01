# ADR-0021: Deployment Platform — Managed Cloud (Neon + Render)

- **Status:** Proposed <!-- owner decision recorded 2026-08-01; acceptance pending -->
- **Date:** 2026-08-01
- **Owner ruling:** 2026-08-01 — the owner chose to move the real pilot from the
  on-premise clinic machine to managed cloud hosting, **and to ship before
  authentication exists**, with authentication to follow "as soon as possible".
  This ADR records that decision and the risk accepted with it. **It does not
  relitigate the decision**; it makes its consequences explicit and orders the
  work so the accepted risk is bounded rather than open-ended.

## Context

The pilot runs today on **one clinic machine**, via
`docker-compose.pilot.yml`: PostgreSQL 17 in a container with a named volume,
the API serving the Angular bundle **same-origin**, both restarting with the
machine, and — decisively — **both ports bound to loopback only**:

```yaml
ports:
  - "127.0.0.1:8080:8080"   # never exposed to the network
```

That single line is what makes the current design safe, because **VetFlow has
no authentication of any kind**: no users, no login, no permissions
(**DEC-INV-030**). Nothing on the network can reach the application, so "anyone
at the machine can do anything" is a physical-access control, not a network one.

Two further facts constrain any move:

- **ADR-0020** forbids destructive migrations once **real data exists**. The
  pilot has been running and holds real clinic data.
- The clinic **sells at a counter**. The on-premise deployment keeps working
  when the internet drops; a cloud deployment does not.

No ADR has ever covered *where* VetFlow runs. ADR-0019 chose PostgreSQL as the
database engine; it says nothing about hosting. This ADR closes that gap.

## Decision

> **VetFlow's pilot will be hosted on managed cloud infrastructure: the
> application on Render (single Docker web service) and PostgreSQL on Neon.**

The application ships **before authentication exists**, by owner decision.

**Because the exposure comes from real clinic data on a publicly reachable URL —
not from the deployment itself — the work is ordered so those two facts do not
overlap:**

| Phase | What ships | State of clinic data |
|---|---|---|
| **1 — Stack live** | Render service + Neon database, empty schema | **No real data.** Entered by hand for verification, or left empty |
| **2 — Authentication** | Login and access control (new capability — needs its own module documentation and owner approval under Documentation-First) | Still no real data |
| **3 — Data migration** | Live pilot data moved from the clinic machine to Neon, under an ADR-0020 migration plan | Real data, behind authentication |

**Phase 3 does not start before Phase 2 lands.** This is the boundary that keeps
the accepted risk bounded: shipping without auth is accepted; **publishing real
clinic data without auth is not part of this decision.**

## Consequences

### Accepted, with eyes open

1. **The application is publicly reachable with no authentication during Phase 1.**
   Anyone with the URL can read every screen and perform every write the UI
   allows — create and commit sales, delete draft lines, adjust and write off
   stock, commit returns. Phase 1 therefore carries **no real clinic data**.
2. **Offline operation at the counter is lost.** When the clinic's connection
   drops, the cashier cannot sell, look up stock, or read a price. The
   on-premise deployment survived this; the cloud one does not. This is a real
   operational downgrade and is recorded as such.
3. **The clinic's data leaves its premises.** Where it is stored, and under whose
   terms, becomes Neon's and Render's — not the clinic's.

### Mechanical consequences

4. **Migrations at startup require exactly one instance.**
   `Database__ApplyMigrationsAtStartup` is `true`. Two Render instances starting
   together would race on the same schema. **Instance count stays at 1** until
   migrations move to a release step.
5. **`backup-restore-runbook.md` becomes wrong.** It documents
   `backup-vetflow.ps1` against a local container. Neon provides point-in-time
   restore instead. The runbook must be rewritten before Phase 3.
6. **`deployment-runbook.md` becomes wrong.** The one-command compose flow no
   longer describes how the pilot is deployed.
7. **Free tiers are unsuitable.** Render's free instances spin down and cold-start;
   Neon autosuspends. A clinic cannot wait on a cold start mid-sale. Paid
   instances and Neon's **pooled** connection string are required.
8. **The runtime image must keep tzdata.** `Clinic:TimeZone` is `Africa/Cairo`,
   and `ClinicClock` **throws** when the zone cannot be resolved — deliberately,
   because BR-INV-060 forbids falling back to UTC. The current base image
   (`mcr.microsoft.com/dotnet/aspnet:10.0`, Debian) carries tzdata. **Switching
   to an Alpine base would break the clinic date at startup.**
9. **Port binding.** The .NET container listens on **8080**; Render expects the
   service to listen on `$PORT` (default 10000). `PORT` is pinned to `8080`.

### Unchanged

10. **One service, not two.** The API already serves the Angular bundle
    same-origin (`Program.cs` pilot hosting block), so there is no second
    service and **CORS stays moot** — `Cors:AllowedOrigins` remains empty.
11. **No new library, framework or runtime** is introduced by this decision.
    Render and Neon are hosting; the application is unchanged.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| **Stay on-premise** | Rejected by the owner. It keeps offline operation and needs no authentication to be safe, but leaves the client unable to reach the system remotely |
| **Cloud, but authentication first** | Recommended by the AI contributor; **the owner chose to ship first and add authentication next**. The phasing in §Decision is the compromise that bounds the resulting risk |
| **Cloud VM (self-managed Postgres + Docker)** | Closest to today's setup and avoids Neon's autosuspend, but moves backup, patching and monitoring onto the clinic with no operator |
| **Edge gate in front of Render** (e.g. Cloudflare Access) | **Not rejected — deferred.** It would give Phase 1 a real access control with no application change, and remains available if the owner wants Phase 3 sooner. Recorded here so the option is not lost |

## Follow-ups this ADR creates

- **Authentication capability** — new module documentation, owner approval, then
  implementation. **Blocks Phase 3.**
- **Migration plan for live pilot data** — required by ADR-0020. **Blocks Phase 3.**
- **Rewrite** `backup-restore-runbook.md` and `deployment-runbook.md`.
- **Decide instance-count / migration strategy** if the service ever needs to
  scale beyond one instance.
