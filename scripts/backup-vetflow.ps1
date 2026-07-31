# VetFlow pilot database backup (PRS WS2, owner ruling PRS-Q-02:
# daily + a manual backup before every deployment, retain the last 7).
# Runs pg_dump inside the pilot db container; writes a timestamped custom-format
# archive; prunes to the newest $Keep files. Exit code 0 = backup verified on disk.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File scripts\backup-vetflow.ps1
#   powershell ... -File scripts\backup-vetflow.ps1 -BackupDir "E:\VetFlowBackups"
param(
    [string]$ComposeFile,
    [string]$BackupDir = (Join-Path $env:USERPROFILE "VetFlowBackups"),
    [int]$Keep = 7
)

$ErrorActionPreference = "Stop"

# Resolved in the body: $PSScriptRoot is not reliably available in param
# defaults on every invocation path (found by the WS2 rehearsal).
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ComposeFile) { $ComposeFile = Join-Path $scriptDir "..\docker-compose.pilot.yml" }

# The database credentials come from the same .env the stack itself uses.
$repoRoot = Split-Path $scriptDir -Parent
$envFile = Join-Path $repoRoot ".env"
if (-not (Test-Path $envFile)) { throw "Missing .env beside the compose file -- the stack cannot run without it either." }
$dbUser = (Select-String -Path $envFile -Pattern '^VETFLOW_DB_USER=(.+)$').Matches[0].Groups[1].Value.Trim()
if (-not $dbUser) { throw "VETFLOW_DB_USER not found in .env." }

if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$target = Join-Path $BackupDir "vetflow-$stamp.dump"

# Custom format (-Fc): compressed, restorable selectively with pg_restore.
# The dump is written INSIDE the container and copied out with `docker cp`:
# piping binary output through the PowerShell pipeline re-encodes it as text
# and silently corrupts the archive (defect found by the WS2 verification --
# the corrupted file passed the size check and failed pg_restore).
docker compose -f $ComposeFile exec -T db pg_dump -U $dbUser -Fc -f /tmp/vetflow-backup.dump vetflow
if ($LASTEXITCODE -ne 0) { throw "pg_dump failed (exit $LASTEXITCODE)." }

# Validate the archive before trusting it (a listable archive is a restorable one).
docker compose -f $ComposeFile exec -T db pg_restore --list /tmp/vetflow-backup.dump | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Backup archive failed validation (pg_restore --list, exit $LASTEXITCODE)." }

docker compose -f $ComposeFile cp db:/tmp/vetflow-backup.dump $target
if ($LASTEXITCODE -ne 0) { throw "Could not copy the backup out of the container (exit $LASTEXITCODE)." }
docker compose -f $ComposeFile exec -T db rm /tmp/vetflow-backup.dump

$size = (Get-Item $target).Length
if ($size -lt 1kb) { Remove-Item $target; throw "Backup file suspiciously small ($size bytes) -- treated as failure." }

# Retention: newest $Keep stay, the rest go (owner ruling: keep the last 7).
Get-ChildItem $BackupDir -Filter "vetflow-*.dump" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -Skip $Keep |
    Remove-Item -Force

Write-Output "OK backup written: $target ($([math]::Round($size / 1kb)) KB)"
