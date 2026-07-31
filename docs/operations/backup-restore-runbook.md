# Backup & Restore Runbook (D2 — PRS WS2)

> Status: Approved scope (PRS, owner 2026-07-31) — content verified by execution;
> see the verification record at the bottom.
> This is the operational runbook ADR-0019 deferred to "before the first
> production deployment". It covers the **script-based** procedure the owner
> ruled sufficient for the Pilot (PRS-Q-03); the in-application Backup module
> remains unbuilt MVP scope, untouched by this document.

## The rules (owner ruling PRS-Q-02, 2026-07-31)

- **Daily backup**, plus a **manual backup before every deployment**.
- **Retain the last 7 backups** — the script enforces this automatically.

## Taking a backup

From the repository folder, in PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\backup-vetflow.ps1
```

- Writes `vetflow-<date>-<time>.dump` into `%USERPROFILE%\VetFlowBackups`
  (change with `-BackupDir "E:\VetFlowBackups"` to target an external drive).
- Prunes to the newest 7 automatically.
- Prints `OK backup written: …` on success. **Anything else is a failure — do
  not assume a backup exists unless you saw the OK line.**

### Scheduling the daily backup (once, as the machine's user)

```powershell
$action = New-ScheduledTaskAction -Execute "powershell.exe" `
  -Argument "-ExecutionPolicy Bypass -File D:\Projects\VetFlow\scripts\backup-vetflow.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 20:00
Register-ScheduledTask -TaskName "VetFlow Daily Backup" -Action $action -Trigger $trigger
```

Pick a time when the clinic machine is on (20:00 shown; adjust to closing time).
Task Scheduler runs it daily; verify the newest file in the backup folder the
next morning after enabling it.

## Restoring a backup

**The restore replaces the current database with the backup.** Take a fresh
backup first, always — even when the database looks broken:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\backup-vetflow.ps1
powershell -ExecutionPolicy Bypass -File scripts\restore-vetflow.ps1 `
  -BackupFile "$env:USERPROFILE\VetFlowBackups\vetflow-<date>-<time>.dump"
```

The script stops the API for the duration (nobody can write mid-restore),
recreates the database, restores in a single transaction (a failed restore
leaves nothing half-written), and starts the API again. It prints
`OK restore completed` on success.

## What was verified (PRS-AC-03/04) — 2026-07-31

Executed against the live pilot stack (WS2):

- A backup of the populated pilot database restored onto a **fresh, separate**
  PostgreSQL instance (different role) reproduced it exactly: **identical row
  counts in every table** and **byte-identical Arabic text** after the round
  trip. Restore took **0.4 s** on the fresh instance; the full scripted
  restore of the pilot stack (stop API → drop → create → restore → start API)
  took **3.3 s**, and the application served the restored invoices afterwards.
- The retention rule was exercised for real: after an eighth backup, exactly
  **7** remained and the oldest was pruned.
- The BR-INV-005 invariant is re-verified on post-smoke data in the WS4 record
  (at WS2 time the database held documents but no stock rows yet).

**Three defects were found by this verification and fixed in the scripts —
none would have surfaced without actually restoring:**

1. **PowerShell `>` redirection corrupted the binary archive** — the dump
   "succeeded", passed the size check, and was **not a valid archive**
   (`pg_restore: input file does not appear to be a valid archive`). The
   scripts now keep bytes out of the PowerShell pipeline entirely
   (`pg_dump -f` inside the container + `docker compose cp`), and every backup
   is validated with `pg_restore --list` before it is trusted.
2. **Ownership statements broke cross-role restore** — restoring under a
   different role failed on `OWNER TO`. Restores now run `--no-owner
   --no-privileges`, so a replacement machine restores cleanly.
3. **`$PSScriptRoot` was empty on one invocation path**, breaking the scripts'
   path resolution — now resolved defensively in the body.

## Limits, stated plainly

- Backups protect **the database**. The repository (code + docs) is protected by
  git — and pushing to the remote is still blocked by Git Credential Manager,
  which only the owner can clear (`! git push origin main`).
- A backup on the same disk as the database does not survive that disk failing.
  Pointing `-BackupDir` at an external or second drive is the owner's call
  (PRS-Q-02 fixed cadence and retention; the medium stays configurable).
