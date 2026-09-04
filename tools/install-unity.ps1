# Installs the Unity editor silently under D:\Program Files\Unity\Hub\Editor without a UAC prompt.
# The installer manifest asks for elevation, but D:\Program Files is writable by the user, so it is run
# with __COMPAT_LAYER=RunAsInvoker (Windows then keeps the current token). Falls back to a real UAC
# prompt only if that produced no editor.
# Usage:  powershell -File tools\install-unity.ps1 -Installer <path to UnitySetup64-*.exe> [-Version 6000.3.23f1]
param(
    [Parameter(Mandatory = $true)][string]$Installer,
    [string]$Version = "6000.3.23f1"
)

$dest = "D:\Program Files\Unity\Hub\Editor\$Version"
$exe = Join-Path $dest "Editor\Unity.exe"
if (-not (Test-Path $Installer)) {
    Write-Host "Installer not found: $Installer"
    Write-Host "Download it from https://unity.com/releases/editor/archive (Windows, $Version)."
    exit 2
}
New-Item -ItemType Directory -Force $dest | Out-Null

Write-Host "Installing $Version into $dest (silent, no elevation)..."
$env:__COMPAT_LAYER = "RunAsInvoker"
$p = Start-Process -FilePath $Installer -ArgumentList "/S /D=$dest" -PassThru -Wait
Remove-Item Env:\__COMPAT_LAYER -ErrorAction SilentlyContinue

if (-not (Test-Path $exe)) {
    Write-Host "No editor after the unelevated run (exit $($p.ExitCode)). Retrying with a UAC prompt: click Yes."
    $p = Start-Process -FilePath $Installer -ArgumentList "/S /D=$dest" -Verb RunAs -PassThru -Wait
}

if (Test-Path $exe) {
    Write-Host "Installed: $exe"
    Write-Host "Registering the editor in Unity Hub..."
    & "D:\Program Files\Unity Hub\Unity Hub.exe" -- --headless editors --add "$exe" 2>$null
    exit 0
}
Write-Host "Install failed (exit code $($p.ExitCode))."
exit 1
