# Launches the last build, screenshots the title screen, presses Enter, screenshots the game, then closes it.
# Prints any exception or error found in the player log. Exit code 1 if the game did not start or logged errors.
# Usage:  powershell -File tools\smoke-test.ps1 [-SecondsTitle 12] [-SecondsGame 10]
param(
    [int]$SecondsTitle = 12,
    [int]$SecondsGame = 10
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
$log = Join-Path $RepoRoot "Builds\smoke-player.log"
if (-not (Test-Path $exe)) { Write-Host "No build at $exe"; exit 2 }
Remove-Item $log -ErrorAction SilentlyContinue

function Save-Screen($path) {
    $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
    $bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($b.Location, [System.Drawing.Point]::Empty, $b.Size)
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "Screenshot: $path"
}

$p = Start-Process -FilePath $exe -ArgumentList @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1600", "-screen-height", "900") -PassThru
Write-Host "Started pid $($p.Id), waiting $SecondsTitle s for the title screen..."
Start-Sleep -Seconds $SecondsTitle
if ($p.HasExited) { Write-Host "Game exited early with code $($p.ExitCode)"; Get-Content $log -Tail 30; exit 1 }
Save-Screen (Join-Path $RepoRoot "Builds\smoke-title.png")

# Bring the window to the front and start a run with Enter.
$sig = '[DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);'
$u32 = Add-Type -MemberDefinition $sig -Name U32 -Namespace Smoke -PassThru
$p.Refresh()
[void]$u32::SetForegroundWindow($p.MainWindowHandle)
Start-Sleep -Milliseconds 500
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Write-Host "Sent Enter, waiting $SecondsGame s in game..."
Start-Sleep -Seconds $SecondsGame
if (-not $p.HasExited) {
    [System.Windows.Forms.SendKeys]::SendWait("w")
    Save-Screen (Join-Path $RepoRoot "Builds\smoke-game.png")
}

if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force; Start-Sleep -Seconds 2 }

$text = Get-Content $log -ErrorAction SilentlyContinue
$markers = $text | Select-String -Pattern "\[VCS\]" | Select-Object -First 5
$errors = $text | Select-String -Pattern "Exception|error CS|NullReference|IndexOutOfRange|MissingReference|ArgumentException|Failed to" | Select-Object -First 20
Write-Host ""
Write-Host "--- markers ---"
$markers | ForEach-Object { Write-Host $_.Line }
Write-Host "--- errors ---"
if ($errors) { $errors | ForEach-Object { Write-Host $_.Line }; Write-Host "SMOKE TEST: ERRORS FOUND"; exit 1 }
Write-Host "(none)"
Write-Host "SMOKE TEST OK"
exit 0
