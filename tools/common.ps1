# Shared paths for the tools scripts. Dot-source this file.
# Override the editor location with the VCS_UNITY environment variable.

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$UnityVersion = "6000.3.23f1"
if ($env:VCS_UNITY) {
    $UnityExe = $env:VCS_UNITY
} else {
    $UnityExe = "D:\Program Files\Unity\Hub\Editor\$UnityVersion\Editor\Unity.exe"
}
$UnityRoot = Split-Path $UnityExe -Parent

if (-not (Test-Path $UnityExe)) {
    Write-Host "Unity editor not found at: $UnityExe"
    Write-Host "Install Unity $UnityVersion there, or set VCS_UNITY to the Unity.exe path."
    exit 2
}
