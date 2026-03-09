# Auto-bump patch version — run before git push
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

git add $file
git commit --amend --no-edit --no-verify

Write-Host "Version bumped: $current -> $newVersion"
