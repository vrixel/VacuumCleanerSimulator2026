# Builds the Android player (APK by default, AAB for Google Play with -Aab) into Builds\Android with the editor in
# batch mode. Needs the Android module installed by tools\install-android.py. The upload keystore is optional:
# tools\make-keystore.ps1 creates D:\Cloclo\Keys\vacuum-android.keystore and its password file, and this script
# hands them to the editor through the environment (never through the repo).
# Usage:  powershell -File tools\build-android.ps1 [-Aab]
param(
    [switch]$Aab
)

. "$PSScriptRoot\common.ps1"

$player = Join-Path $UnityRoot "Data\PlaybackEngines\AndroidPlayer"
if (-not (Test-Path (Join-Path $player "SDK\platform-tools\adb.exe"))) {
    Write-Host "Android module not installed (no $player\SDK). Run: python tools\install-android.py"
    exit 2
}

$keystore = "D:\Cloclo\Keys\vacuum-android.keystore"
$passFile = "D:\Cloclo\Keys\vacuum-android.pass"
if ((Test-Path $keystore) -and (Test-Path $passFile)) {
    $env:VCS_KEYSTORE = $keystore
    $env:VCS_KEYSTORE_PASS = (Get-Content $passFile -Raw).Trim()
    $env:VCS_KEYALIAS = "vacuum"
    $env:VCS_KEYALIAS_PASS = $env:VCS_KEYSTORE_PASS
    Write-Host "Signing with $keystore"
} else {
    Write-Host "No upload keystore at $keystore (debug signature; run tools\make-keystore.ps1 for Play)"
}

$buildDir = Join-Path $RepoRoot "Builds"
$log = Join-Path $buildDir "build-android.log"
New-Item -ItemType Directory -Force $buildDir | Out-Null
$method = if ($Aab) { "VCS.Editor.BuildScript.BuildAndroidAab" } else { "VCS.Editor.BuildScript.BuildAndroidApk" }

Write-Host "Building Android ($(if ($Aab) { 'AAB' } else { 'APK' })) with $UnityExe"
Write-Host "Log: $log"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$unityArgs = @("-batchmode", "-nographics", "-quit", "-timestamps", "-projectPath", "`"$RepoRoot`"", "-buildTarget", "Android", "-executeMethod", $method, "-logFile", "`"$log`"")
$proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru
$proc.WaitForExit()
$code = $proc.ExitCode
$sw.Stop()
Remove-Item Env:\VCS_KEYSTORE_PASS -ErrorAction SilentlyContinue
Remove-Item Env:\VCS_KEYALIAS_PASS -ErrorAction SilentlyContinue

if ($code -ne 0) {
    Write-Host "Build FAILED (exit $code) after $([int]$sw.Elapsed.TotalSeconds) s. Errors from the log:"
    Get-Content $log | Select-String -Pattern "error CS|Error building|\[VCS\]|Exception|FAILURE|error:" | Select-Object -First 40
    exit $code
}

$out = Join-Path $RepoRoot ("Builds\Android\VacuumCleanerSimulator2026." + $(if ($Aab) { "aab" } else { "apk" }))
Write-Host "Build OK in $([int]$sw.Elapsed.TotalSeconds) s -> $out ($([math]::Round((Get-Item $out).Length / 1MB)) MB)"
Get-Content $log | Select-String -Pattern "\[VCS\] Build" | Select-Object -Last 1
