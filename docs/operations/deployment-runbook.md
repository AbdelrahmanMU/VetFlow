# Deployment Runbook — Pilot (D1 — PRS WS1)

> Status: Approved scope (PRS, owner 2026-07-31) — validated by execution; the
> validation record is at the bottom.
> Target (owner ruling PRS-Q-01): a standard **Windows 11** workstation with
> **Docker Desktop**, an **SSD**, and **at least 16 GB RAM**.

> ## ⚠️ Two deployments exist as of 2026-08-01 — read this first
>
> **ADR-0021** (Proposed) moves the pilot to managed cloud (Neon + Render), in
> three phases. Until **Phase 3** completes:
>
> | | Where | Holds real clinic data? | Authoritative? |
> |---|---|---|---|
> | **On-premise** (this document, below) | clinic machine, loopback only | **Yes** | **Yes — this is the live pilot** |
> | **Cloud** ([§Cloud deployment](#cloud-deployment-adr-0021--phase-1)) | Render + Neon | **No — must stay empty of real data** | Not yet |
>
> **The cloud service has no authentication** (DEC-INV-030) and is publicly
> reachable. That is why Phase 1 carries no real data, and why **the data
> migration waits for authentication to ship**. Do not move clinic data to the
> cloud before then — see ADR-0021 §Decision.

## The shape of the pilot deployment — and why

One machine, two containers, one command:

- **`db`** — PostgreSQL 17 (the platform ADR-0019 chose), data in a named
  Docker volume, port bound to **loopback only** for backup tooling.
- **`api`** — the ASP.NET Core API in **Production** configuration, which also
  serves the **Angular production bundle same-origin** from its own process.
  This was the deliberate WS1 decision: the built-in static-file hosting adds
  **no new tool, no second server, and no CORS surface** — the alternatives (an
  nginx container, a second host process) would each have needed owner approval
  for a new tool and another moving part to operate. Development is untouched:
  `ng serve` + proxy still work exactly as before, because the API only serves
  the bundle when it finds one beside itself (it never does in development).
- The Angular bundle is **built inside the Docker image** (a `node:24-alpine`
  build stage), so deployment needs nothing installed on the machine except
  Docker Desktop and this repository.
- Migrations apply automatically at API startup — the same mechanism
  development always used (`Database__ApplyMigrationsAtStartup`), now verified
  against an empty volume. Development seed is **explicitly off**
  (`Database__SeedDevelopmentDataAtStartup: "false"` — PRS-Q-04: the pilot
  database receives reference data through the application, never seed rows).

## Prerequisites (once per machine)

1. Windows 11, SSD, ≥ 16 GB RAM (PRS-Q-01).
2. Docker Desktop installed and running.
3. This repository on disk.
4. A `.env` file beside `docker-compose.pilot.yml` (copy `.env.example`, set a
   real user and a strong password — the file stays untracked, STD-CS-030).

## Deploy / update

```powershell
# ALWAYS first when updating an existing deployment (owner ruling PRS-Q-02):
powershell -ExecutionPolicy Bypass -File scripts\backup-vetflow.ps1

docker compose -f docker-compose.pilot.yml up -d --build
```

Then open **http://localhost:8080** — the application, in Arabic, RTL.

The same command is the update path: it rebuilds the image from the current
repository state and replaces the running API; the database volume is never
touched by a redeploy.

## Verify a deployment (after every deploy)

```powershell
docker compose -f docker-compose.pilot.yml ps        # both services "running/healthy"
curl.exe -s http://localhost:8080/api/v1/products?pageSize=1   # JSON, not an error
```

And in the browser: the shell renders, the sales list opens at
http://localhost:8080/sales (a deep link — proves SPA fallback works).

## Stop / start

```powershell
docker compose -f docker-compose.pilot.yml stop     # stop (data stays)
docker compose -f docker-compose.pilot.yml start    # start again
```

Both containers carry `restart: unless-stopped`: after a machine reboot they
come back on their own once Docker Desktop starts.

## What must never be run casually

- `docker compose -f docker-compose.pilot.yml down -v` — **deletes the
  database volume**. Only ever with a verified backup in hand, and after the
  Pilot begins only with the owner's explicit instruction.

## Cloud deployment (ADR-0021) — Phase 1

> **Phase 1 only: the stack goes live with an empty database.** No clinic data
> until authentication ships (ADR-0021 §Decision). The on-premise deployment
> above remains the live pilot in the meantime.

### What runs where

- **Render** — one **web service**, built from the same
  `src/VetFlow.Api/Dockerfile` the on-premise pilot uses. The API serves the
  Angular bundle same-origin, so there is **one service, no second host, no
  CORS** — identical to on-premise.
- **Neon** — PostgreSQL. Replaces the `db` container; **nothing else changes in
  the application**.
- Configuration lives in [`render.yaml`](../../render.yaml) (a Render Blueprint).

### Prerequisites (once)

1. A **Neon** project with a database named `vetflow`, and its connection string.
2. A **Render** account connected to this repository.
3. **Paid plans on both.** Free tiers spin down and cold-start; a clinic cannot
   wait mid-sale (ADR-0021 §Consequences 7).

### Deploy

1. Render Dashboard → **New → Blueprint** → select this repository. Render reads
   `render.yaml`.
2. Set the one secret it asks for — **`Database__ConnectionString`**:

   ```
   Host=<project>.<region>.aws.neon.tech;Database=vetflow;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
   ```

   **Use Neon's DIRECT endpoint, not the `-pooler` host.** This process applies
   EF migrations at startup, and Neon's pooled endpoint runs PgBouncer in
   transaction mode — the wrong place to issue DDL. One instance serving one
   clinic does not need pooling.
3. Deploy. The schema is created automatically at first startup
   (`Database__ApplyMigrationsAtStartup`), exactly as on-premise.

### Verify (after every deploy)

1. Open the Render URL — the Arabic UI loads and the sidebar renders.
2. `GET /api/v1/purchase-invoices` returns `200` with an empty list.
3. Create a category, then reload — it persists (proves Neon is connected, not a
   container-local database).
4. In Neon, confirm the `__EFMigrationsHistory` table exists and is populated.
5. Confirm any date the UI shows matches the **clinic's** date, not UTC —
   `Clinic__TimeZone` is `Africa/Cairo` (BR-INV-060).

### What must never be done here

- **Do not raise `numInstances` above 1.** Migrations run at startup; two
  instances would race on the same schema (ADR-0021 §Consequences 4).
- **Do not switch the runtime image to Alpine.** `ClinicClock` throws when the
  time zone cannot be resolved — by design, because BR-INV-060 forbids falling
  back to UTC. The Debian base carries tzdata; Alpine does not.
- **Do not import clinic data.** Phase 3, after authentication (ADR-0021).
- **Do not set `Database__SeedDevelopmentDataAtStartup` to `true`** (PRS-Q-04).

### Not yet written

- **Backup and restore for Neon.** `backup-restore-runbook.md` still documents
  the local `backup-vetflow.ps1` against a container, which does not apply here.
  Neon provides point-in-time restore instead. **Must be rewritten before
  Phase 3** (ADR-0021 §Consequences 5).

---

## Validation record (PRS-AC-01/02) — 2026-07-31

Recorded by the WS1 execution; details in `STATUS.md` for the session:

- Bootstrapped **twice from nothing** (fresh volume each time) with the single
  documented command: both services healthy, all migrations applied to the
  empty database automatically, application reachable.
- The frontend served is the **production bundle** from the image (no `ng
  serve` anywhere); deep links (`/sales`, `/inventory`) return the app, while
  an unknown `/api/...` path still returns the canonical ProblemDetails 404.
- Arabic RTL rendering verified through the served bundle.
- Environment verified as `Production`; development seed verified absent
  (zero rows in every business table after first boot — the WS3 record).
