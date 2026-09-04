# Builds the Windows 64-bit player into Builds\Win64 with the Unity editor in batch mode.
# Usage:  powershell -File tools\build.ps1 [-Run]
param(
    [switch]$Run
)

. "$PSScriptRoot\common.ps1"

$buildDir = Join-Path $RepoRoot "Builds"
$log = Join-Path $buildDir "build.log"
New-Item -ItemType Directory -Force $buildDir | Out-Null

Write-Host "Building with $UnityExe"
Write-Host "Log: $log"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& $UnityExe -batchmode -nographics -quit -projectPath "$RepoRoot" -executeMethod VCS.Editor.BuildScript.BuildWindows64 -logFile "$log"
$code = $LASTEXITCODE
$sw.Stop()

if ($code -ne 0) {
    Write-Host "Build FAILED (exit $code) after $([int]$sw.Elapsed.TotalSeconds) s. Errors from the log:"
    Get-Content $log | Select-String -Pattern "error CS|Error building|\[VCS\]|Exception" | Select-Object -First 40
    exit $code
}

$exe = Join-Path $RepoRoot "Builds\Win64\VacuumCleanerSimulator2026.exe"
Write-Host "Build OK in $([int]$sw.Elapsed.TotalSeconds) s -> $exe"
Get-Content $log | Select-String -Pattern "\[VCS\] Build" | Select-Object -Last 1
if ($Run) { & "$PSScriptRoot\run.ps1" }
