# VetFlow pilot database restore (PRS WS2). Restores a backup taken by
# backup-vetflow.ps1 into the pilot database, stopping the API for the duration
# so nothing writes mid-restore. DESTRUCTIVE by nature: the current database is
# replaced by the backup -- the runbook requires taking a fresh backup first.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\restore-vetflow.ps1 -BackupFile "C:\Users\...\VetFlowBackups\vetflow-20260731-090000.dump"
param(
    [Parameter(Mandatory = $true)][string]$BackupFile,
    [string]$ComposeFile
)

$ErrorActionPreference = "Stop"

# Resolved in the body: $PSScriptRoot is not reliably available in param
# defaults on every invocation path (found by the WS2 rehearsal).
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ComposeFile) { $ComposeFile = Join-Path $scriptDir "..\docker-compose.pilot.yml" }

if (-not (Test-Path $BackupFile)) { throw "Backup file not found: $BackupFile" }

$repoRoot = Split-Path $scriptDir -Parent
$envFile = Join-Path $repoRoot ".env"
if (-not (Test-Path $envFile)) { throw "Missing .env beside the compose file." }
$dbUser = (Select-String -Path $envFile -Pattern '^VETFLOW_DB_USER=(.+)$').Matches[0].Groups[1].Value.Trim()
if (-not $dbUser) { throw "VETFLOW_DB_USER not found in .env." }

Write-Output "Stopping the API so nothing writes during the restore..."
docker compose -f $ComposeFile stop api
if ($LASTEXITCODE -ne 0) { throw "Could not stop the API (exit $LASTEXITCODE)." }

try {
    # Recreate the database, then restore into it. --if-exists keeps the drop quiet
    # on a fresh instance; single-transaction keeps a failed restore from leaving a
    # half-written database behind.
    docker compose -f $ComposeFile exec -T db dropdb -U $dbUser --if-exists vetflow
    if ($LASTEXITCODE -ne 0) { throw "dropdb failed (exit $LASTEXITCODE)." }
    docker compose -f $ComposeFile exec -T db createdb -U $dbUser vetflow
    if ($LASTEXITCODE -ne 0) { throw "createdb failed (exit $LASTEXITCODE)." }
    # Copy the archive INTO the container and restore from the file: piping bytes
    # through the PowerShell pipeline corrupts binary data (the WS2 finding).
    docker compose -f $ComposeFile cp $BackupFile db:/tmp/vetflow-restore.dump
    if ($LASTEXITCODE -ne 0) { throw "Could not copy the backup into the container (exit $LASTEXITCODE)." }
    # --no-owner/--no-privileges: the archive records the source role; a restore
    # must succeed under whatever role the target instance uses (found by the
    # WS2 fresh-instance rehearsal, where the roles differed).
    docker compose -f $ComposeFile exec -T db pg_restore -U $dbUser -d vetflow --single-transaction --no-owner --no-privileges /tmp/vetflow-restore.dump
    if ($LASTEXITCODE -ne 0) { throw "pg_restore failed (exit $LASTEXITCODE)." }
    docker compose -f $ComposeFile exec -T db rm /tmp/vetflow-restore.dump
}
finally {
    Write-Output "Starting the API again..."
    docker compose -f $ComposeFile start api
}

Write-Output "OK restore completed from: $BackupFile"
