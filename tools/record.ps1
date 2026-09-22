# Runs the last build in its self-recording mode ("-record <dir>", see SmokeRunner.RecordRun): the game locks its
# clock to 30 steps per second, plays a scripted tour (title, garage, drive, turbo, cat, powder, cord, bin) and
# writes every frame as a JPEG plus marks.txt (chapter name + first frame). tools\store_video.py turns that into
# the store trailer. Nothing is typed or clicked, so it is safe while someone is using the PC; a screen recorder
# is not (the session is a locked RDP desktop). The window is captured at -Super times its size, like the store
# screenshots: 960 x 540 at 2 gives 1920 x 1080 frames.
# Usage:  powershell -File tools\record.ps1 [-Width 960 -Height 540 -Super 2] [-TimeoutSeconds 400]
param(
    [int]$TimeoutSeconds = 400,   # about 35 s of game time, but every frame is encoded to disk
    [int]$Width = 960,
    [int]$Height = 540,
    [int]$Super = 2
)

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
$out = Join-Path $RepoRoot "Builds\record"
$log = Join-Path $RepoRoot "Builds\record-player.log"
if (-not (Test-Path $exe)) { Write-Host "No build at $exe"; exit 2 }
New-Item -ItemType Directory -Force $out | Out-Null
Remove-Item $log -ErrorAction SilentlyContinue

$gameArgs = @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "$Width", "-screen-height", "$Height", "-record", "`"$out`"")
if ($Super -gt 1) { $gameArgs += @("-super", "$Super") }
$p = Start-Process -FilePath $exe -ArgumentList $gameArgs -PassThru
Write-Host "Started pid $($p.Id); waiting up to $TimeoutSeconds s for the recording to finish..."
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
$done = ($text | Where-Object { $_ -match "\[VCS\] Record finished" }).Count -gt 0
$frames = @(Get-ChildItem (Join-Path $out "frame_*.jpg") -ErrorAction SilentlyContinue)

Write-Host ""
Write-Host "--- markers ---"
$markers | ForEach-Object { Write-Host $_ }
Write-Host "--- frames ---"
Write-Host ("  " + $frames.Count + " frames, " + [int](($frames | Measure-Object Length -Sum).Sum / 1MB) + " MB")
Write-Host "--- errors ---"
if ($errors) { $errors | Select-Object -First 20 | ForEach-Object { Write-Host $_ } } else { Write-Host "(none)" }
Write-Host ""

$ok = $done -and (-not $errors) -and ($frames.Count -ge 300)
if ($ok) { Write-Host "RECORD OK"; exit 0 }
if (-not $done) { Write-Host "RECORD FAILED: run did not finish" }
elseif ($errors) { Write-Host "RECORD FAILED: errors in the player log" }
else { Write-Host "RECORD FAILED: too few frames" }
exit 1
