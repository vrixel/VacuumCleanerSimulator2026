# Builds the Windows player, zips it and publishes a GitHub release with the zip attached.
# Usage:  powershell -File tools\release.ps1 -Version 0.1.0 [-Notes "..."] [-Draft]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Notes = "",
    [switch]$Draft,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $RepoRoot

if (-not $SkipBuild) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "build.ps1")
    if ($LASTEXITCODE -ne 0) { Write-Host "Build failed, no release."; exit 1 }
}

$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
if (-not (Test-Path $exe)) { Write-Host "No build at $exe"; exit 1 }

$tag = "v$Version"
$zipName = "VacuumCleanerSimulator2026-$tag-win64.zip"
$zip = Join-Path $RepoRoot "Builds\$zipName"
if (Test-Path $zip) { Remove-Item $zip -Force }
Write-Host "Zipping Builds\Win64 -> $zipName"
Compress-Archive -Path (Join-Path $RepoRoot "Builds\Win64\*") -DestinationPath $zip -CompressionLevel Optimal
$size = [math]::Round((Get-Item $zip).Length / 1MB, 1)
Write-Host "Zip: $size MB"

if ([string]::IsNullOrWhiteSpace($Notes)) {
    $Notes = @"
Windows 64-bit build. Unzip anywhere and run VacuumCleanerSimulator2026.exe.

Keyboard: WASD drive, Space hop, Shift turbo, E blow, F empty the bag at the bin, R rewind the cord, Esc pause.
Xbox controller: left stick, A, RB, B, X, Y, Start. Pick your vacuum on the title screen with A / D or LB / RB.

Family friendly. Saves best score, achievements and garage choice locally.
"@
}

$args = @("release", "create", $tag, $zip, "--title", "Vacuum Cleaner Simulator 2026 $tag", "--notes", $Notes)
if ($Draft) { $args += "--draft" }
Write-Host "Publishing release $tag..."
& gh @args
if ($LASTEXITCODE -ne 0) { Write-Host "gh release create failed."; exit 1 }
& gh release view $tag --json url --jq .url
