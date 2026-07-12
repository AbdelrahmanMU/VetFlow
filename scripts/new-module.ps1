# Scaffolds docs/modules/<Name>/ from docs/modules/_TEMPLATE/.
# Usage: .\scripts\new-module.ps1 -Name vaccinations
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z][a-z0-9-]*$')]  # kebab-case, per .claude/rules/naming.md
    [string]$Name
)

$root = Split-Path $PSScriptRoot -Parent
$template = Join-Path $root 'docs/modules/_TEMPLATE'
$dest = Join-Path $root "docs/modules/$Name"
if (Test-Path $dest) { Write-Error "Module '$Name' already exists at $dest"; exit 1 }

New-Item -ItemType Directory -Path $dest | Out-Null

# Title-case the kebab name for headings (e.g. audit-log -> Audit-Log)
$title = ($Name -split '-' | ForEach-Object { $_.Substring(0,1).ToUpper() + $_.Substring(1) }) -join '-'

Get-ChildItem $template -File | ForEach-Object {
    (Get-Content $_.FullName -Raw -Encoding utf8) -replace '<Module Name>', $title |
        Out-File (Join-Path $dest $_.Name) -Encoding utf8 -NoNewline
}

Write-Host "Created docs/modules/$Name/ (standard set per docs/modules/_TEMPLATE/)."
Write-Host "Next: fill the Arabic name in overview.md and add a row to docs/modules/_INDEX.md."
