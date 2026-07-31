# STD-BE-051 gate (ADR-0020 SConsequences, activated by the Pilot Transition --
# PRS WS6): scans every migration's Up() for destructive operations. Exit 0 =
# pass. Semi-Automatic by design, exactly as the ADR words it:
#   - DropTable / DropColumn / DeleteData  -> HARD FAIL unless the migration is
#     listed in the approved plans file with an owner-approved plan.
#   - AlterColumn                          -> listed for human review (narrowing
#     cannot be decided from one file); reviewer confirms no type/length shrink.
#
# Approved plans live in docs/operations/approved-destructive-migrations.txt --
# one migration file name per line, '#' comments allowed. Adding a line there is
# recording an OWNER-approved migration plan (ADR-0020's escape hatch); no AI
# contributor may add one on its own authority.
param(
    [string]$MigrationsDir = (Join-Path $PSScriptRoot "..\src\VetFlow.Infrastructure\Persistence\Migrations"),
    [string]$ApprovedFile = (Join-Path $PSScriptRoot "..\docs\operations\approved-destructive-migrations.txt")
)

$ErrorActionPreference = "Stop"

$approved = @()
if (Test-Path $ApprovedFile) {
    $approved = Get-Content $ApprovedFile |
        Where-Object { $_ -and -not $_.Trim().StartsWith('#') } |
        ForEach-Object { $_.Trim() }
}

$failures = @()
$reviews = @()

# Designer/snapshot files describe the model, not operations -- only real
# migration files carry an Up() to scan.
$files = Get-ChildItem $MigrationsDir -Filter "*.cs" |
    Where-Object { $_.Name -notlike "*.Designer.cs" -and $_.Name -ne "VetFlowDbContextModelSnapshot.cs" }

foreach ($file in $files) {
    $text = Get-Content $file.FullName -Raw
    # Everything between Up( and Down( -- the destructive rule governs what Up applies.
    $upStart = $text.IndexOf("void Up(")
    $downStart = $text.IndexOf("void Down(")
    if ($upStart -lt 0) { continue }
    $up = if ($downStart -gt $upStart) { $text.Substring($upStart, $downStart - $upStart) } else { $text.Substring($upStart) }

    $destructive = [regex]::Matches($up, 'migrationBuilder\.(DropTable|DropColumn|DeleteData)\b') |
        ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
    if ($destructive.Count -gt 0) {
        if ($approved -contains $file.Name) {
            Write-Output "APPROVED  $($file.Name): $($destructive -join ', ') (owner-approved plan on record)"
        } else {
            $failures += "$($file.Name): $($destructive -join ', ')"
        }
    }

    if ($up -match 'migrationBuilder\.AlterColumn') {
        $reviews += $file.Name
    }
}

if ($reviews.Count -gt 0) {
    Write-Output "REVIEW (AlterColumn present -- confirm no narrowing):"
    $reviews | ForEach-Object { Write-Output "  $_" }
}

if ($failures.Count -gt 0) {
    Write-Output "FAIL -- destructive operations without an owner-approved plan (STD-BE-051 / ADR-0020):"
    $failures | ForEach-Object { Write-Output "  $_" }
    exit 1
}

Write-Output "PASS -- no unapproved destructive migration operations."
