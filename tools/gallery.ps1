# Renders every vacuum in both looks (before: cartoon with eyes, after: realistic) to PNG stills with the last build,
# then composes docs/screenshots/models-before-after.png.  Usage: powershell -File tools\gallery.ps1
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
$out = Join-Path $RepoRoot "Builds\gallery"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null
$log = Join-Path $out "player.log"
$p = Start-Process -FilePath $exe -ArgumentList @("-logFile", "`"$log`"", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-gallery", "`"$out`"") -PassThru
$deadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if ((Test-Path $log) -and (Select-String -Path $log -Pattern "Gallery done" -Quiet)) { break }
}
if (-not $p.HasExited) { Start-Sleep -Seconds 2; if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }
Get-ChildItem $out -Filter "*.png" | ForEach-Object { "  $($_.Name)  $([int]($_.Length/1KB)) KB" }
python (Join-Path $PSScriptRoot "gallery_sheet.py")
