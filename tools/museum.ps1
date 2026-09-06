# Orientation diagnostics of the museum pieces with the last build: each imported model as the game builds it, a red
# ball at the nozzle point and a yellow bar along +z (driving direction), two views, bounds in the log; then a sheet.
# Usage: powershell -File tools\museum.ps1   -> Builds\museum\museum-*.png, docs\screenshots\museum-orientation.png
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
$out = Join-Path $RepoRoot "Builds\museum"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null
$log = Join-Path $out "player.log"
$p = Start-Process -FilePath $exe -ArgumentList @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-museum", "`"$out`"") -PassThru
$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if ((Test-Path $log) -and (Select-String -Path $log -Pattern "Museum diagnostics done" -Quiet)) { break }
}
if (-not $p.HasExited) { Start-Sleep -Seconds 2; if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
Select-String -Path $log -Pattern "\[VCS\] Museum " | ForEach-Object { $_.Line }
python (Join-Path $PSScriptRoot "museum_sheet.py")
