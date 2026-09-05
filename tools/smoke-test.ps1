# Runs the last build in its automated smoke mode ("-smoke <dir>", see SmokeRunner.cs): the game screenshots
# its own title screen, starts a run, drives around, screenshots the game, logs a result line and quits.
# Nothing is typed or clicked, so it is safe to run while someone is using the PC.
# Exit code 1 if the run did not finish, logged an exception, or produced no screenshots.
# Usage:  powershell -File tools\smoke-test.ps1 [-TimeoutSeconds 60]
param(
    [int]$TimeoutSeconds = 60
)

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
$out = Join-Path $RepoRoot "Builds"
$log = Join-Path $out "smoke-player.log"
if (-not (Test-Path $exe)) { Write-Host "No build at $exe"; exit 2 }
Remove-Item $log -ErrorAction SilentlyContinue
Remove-Item (Join-Path $out "smoke-*.png") -ErrorAction SilentlyContinue

$gameArgs = @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-smoke", "`"$out`"")
$p = Start-Process -FilePath $exe -ArgumentList $gameArgs -PassThru
Write-Host "Started pid $($p.Id); waiting up to $TimeoutSeconds s for the smoke run to finish..."
$finished = $p.WaitForExit($TimeoutSeconds * 1000)
if (-not $finished) {
    Write-Host "Timed out, killing the game."
    Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

$text = @(Get-Content $log -ErrorAction SilentlyContinue)
$markers = $text | Where-Object { $_ -match "\[VCS\]" }
$benign = "d3d12: failed to query info queue interface"
$errors = $text | Where-Object { $_ -match "Exception|error CS|NullReference|IndexOutOfRange|MissingReference|ArgumentException|Failed to|Crash" -and $_ -notmatch $benign }
$done = ($text | Where-Object { $_ -match "\[VCS\] Smoke test finished" }).Count -gt 0
$shots = Get-ChildItem (Join-Path $out "smoke-*.png") -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "--- markers ---"
$markers | ForEach-Object { Write-Host $_ }
Write-Host "--- screenshots ---"
$shots | ForEach-Object { Write-Host ("  " + $_.Name + "  " + [int]($_.Length / 1KB) + " KB") }
Write-Host "--- errors ---"
if ($errors) { $errors | Select-Object -First 20 | ForEach-Object { Write-Host $_ } } else { Write-Host "(none)" }
Write-Host ""

$ok = $done -and (-not $errors) -and ($shots.Count -ge 2)
if ($ok) { Write-Host "SMOKE TEST OK"; exit 0 }
if (-not $done) { Write-Host "SMOKE TEST FAILED: run did not finish" }
elseif ($errors) { Write-Host "SMOKE TEST FAILED: errors in the player log" }
else { Write-Host "SMOKE TEST FAILED: screenshots missing" }
exit 1
