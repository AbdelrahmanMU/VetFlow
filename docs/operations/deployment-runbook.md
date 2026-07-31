# Deployment Runbook — Pilot (D1 — PRS WS1)

> Status: Approved scope (PRS, owner 2026-07-31) — validated by execution; the
> validation record is at the bottom.
> Target (owner ruling PRS-Q-01): a standard **Windows 11** workstation with
> **Docker Desktop**, an **SSD**, and **at least 16 GB RAM**.

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
