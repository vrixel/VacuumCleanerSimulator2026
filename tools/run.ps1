# Launches the last local build. The player log lands in Builds\player.log.
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
if (-not (Test-Path $exe)) {
    Write-Host "No build found at $exe. Run tools\build.ps1 first."
    exit 2
}
$log = Join-Path $RepoRoot "Builds\player.log"
Start-Process -FilePath $exe -ArgumentList @("-logFile", "`"$log`"")
Write-Host "Started $exe (log: $log)"
