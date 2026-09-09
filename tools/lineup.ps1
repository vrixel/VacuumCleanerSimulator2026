# Renders every vacuum of the garage side by side, at one scale, on a 0.25 m grid, then stacks the sheets into
# docs\screenshots\models-lineup.png.  The picture that answers "are they all the same kind of size?".
# Usage: powershell -File tools\lineup.ps1
. (Join-Path $PSScriptRoot "common.ps1")

$out = Join-Path $RepoRoot "Builds\lineup"
$log = Join-Path $RepoRoot "Builds\lineup.log"
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
if (-not (Test-Path $exe)) { Write-Host "No build: run tools\build.ps1 first"; exit 1 }
if (Test-Path $log) { Remove-Item -Force $log }
if (Test-Path $out) { Remove-Item -Recurse -Force $out }

$p = Start-Process -FilePath $exe -ArgumentList @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-lineup", "`"$out`"") -PassThru
for ($i = 0; $i -lt 120; $i++) {
    Start-Sleep -Seconds 1
    if ((Test-Path $log) -and (Select-String -Path $log -Pattern "Lineup done" -Quiet)) { break }
    if ($p.HasExited) { break }
}
Start-Sleep -Seconds 2
if (-not $p.HasExited) { $p.Kill() }

Select-String -Path $log -Pattern "\[VCS\] Lineup " | ForEach-Object { $_.Line -replace '^.*\[VCS\] ', '' }
python (Join-Path $PSScriptRoot "lineup_sheet.py")
