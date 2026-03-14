# Auto-bump patch version — runs as pre-commit hook
# Usage: powershell -File .githooks/bump-version.ps1

$file = Join-Path $PSScriptRoot "..\package.json"
if (-not (Test-Path $file)) { exit 0 }

$json = Get-Content $file -Raw | ConvertFrom-Json
$current = $json.version
$parts = $current.Split('.')
$parts[2] = [int]$parts[2] + 1
$newVersion = $parts -join '.'

$content = Get-Content $file -Raw
$content = $content -replace "`"version`": `"$current`"", "`"version`": `"$newVersion`""
Set-Content $file $content -NoNewline

# Stage the bumped file (no amend!)
git add $file

Write-Host "Version bumped: $current -> $newVersion"
