# Exploration (branch explore/hot-chrome): hot-pink car-ad renders of the sled -> Builds\hotchrome
# Usage: powershell -File tools\hotchrome.ps1
. (Join-Path $PSScriptRoot "common.ps1")

$out = Join-Path $RepoRoot "Builds\hotchrome"
$log = Join-Path $RepoRoot "Builds\hotchrome.log"
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
if (-not (Test-Path $exe)) { Write-Host "No build: run tools\build.ps1 first"; exit 1 }
if (Test-Path $log) { Remove-Item -Force $log }
if (Test-Path $out) { Remove-Item -Recurse -Force $out }

$p = Start-Process -FilePath $exe -ArgumentList @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-hotchrome", "`"$out`"") -PassThru
for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Seconds 1
    if ((Test-Path $log) -and (Select-String -Path $log -Pattern "HotChrome done" -Quiet)) { break }
    if ($p.HasExited) { break }
}
Start-Sleep -Seconds 2
if (-not $p.HasExited) { $p.Kill() }

Select-String -Path $log -Pattern "\[VCS\] HotChrome " | ForEach-Object { $_.Line -replace '^.*\[VCS\] ', '' }
