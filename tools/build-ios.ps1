# Exports the iOS Xcode project with the editor in batch mode (Builds\iOS: IL2CPP sources, data, Unity-iPhone.xcodeproj),
# zips it, and with -Upload hands it to the GitHub macOS runner: the zip goes on the rolling pre-release "ios-source"
# and the "iOS TestFlight" workflow is dispatched with the build number (= the version code of bundleVersion).
# Xcode, the signing and the TestFlight upload all happen on the runner (.github/workflows/ios-testflight.yml);
# the Apple signing material lives in AWS Secrets Manager, never here.
# Needs the iOS Build Support module: python tools\install-android.py --target ios
# Usage:  powershell -File tools\build-ios.ps1 [-Upload] [-SkipBuild]
param(
    [switch]$Upload,
    [switch]$SkipBuild
)

. "$PSScriptRoot\common.ps1"

$support = Join-Path $UnityRoot "Data\PlaybackEngines\iOSSupport"
if (-not (Test-Path $support)) {
    Write-Host "iOS module not installed (no $support). Run: python tools\install-android.py --target ios"
    exit 2
}

$buildDir = Join-Path $RepoRoot "Builds"
$out = Join-Path $buildDir "iOS"
$log = Join-Path $buildDir "build-ios.log"
$zip = Join-Path $buildDir "ios-xcode.zip"
New-Item -ItemType Directory -Force $buildDir | Out-Null

if (-not $SkipBuild) {
    if (Test-Path $out) { Remove-Item -Recurse -Force $out }
    Write-Host "Exporting the iOS Xcode project with $UnityExe"
    Write-Host "Log: $log"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $unityArgs = @("-batchmode", "-nographics", "-quit", "-timestamps", "-projectPath", "`"$RepoRoot`"", "-buildTarget", "iOS", "-executeMethod", "VCS.Editor.BuildScript.BuildIos", "-logFile", "`"$log`"")
    $proc = Start-Process -FilePath $UnityExe -ArgumentList $unityArgs -PassThru
    $proc.WaitForExit()
    $code = $proc.ExitCode
    $sw.Stop()
    if ($code -ne 0) {
        Write-Host "Export FAILED (exit $code) after $([int]$sw.Elapsed.TotalSeconds) s. Errors from the log:"
        Get-Content $log | Select-String -Pattern "error CS|Error building|\[VCS\]|Exception|FAILURE|error:" | Select-Object -First 40
        exit $code
    }
    Write-Host "Export OK in $([int]$sw.Elapsed.TotalSeconds) s -> $out"
    Get-Content $log | Select-String -Pattern "\[VCS\] Build" | Select-Object -Last 1

    # Zip: 7-Zip when present (Compress-Archive chokes on the thousands of IL2CPP files), Compress-Archive otherwise.
    if (Test-Path $zip) { Remove-Item -Force $zip }
    $sevenZip = "D:\DevTools\7-Zip\7z.exe"
    if (Test-Path $sevenZip) {
        & $sevenZip a -tzip -mx=5 -bso0 -bsp0 $zip (Join-Path $out "*") | Out-Null
    } else {
        Compress-Archive -Path (Join-Path $out "*") -DestinationPath $zip -CompressionLevel Optimal
    }
    Write-Host ("Zip: {0:N1} MB -> {1}" -f ((Get-Item $zip).Length / 1MB), $zip)
}

if ($Upload) {
    if (-not (Test-Path $zip)) { Write-Host "No $zip to upload"; exit 3 }
    $version = (Select-String -Path (Join-Path $RepoRoot "Assets\Editor\ProjectSetup.cs") -Pattern 'bundleVersion = "([^"]+)"').Matches[0].Groups[1].Value
    $parts = $version.Split('.')
    $buildNumber = [int]$parts[0] * 10000 + [int]$parts[1] * 100 + [int]$parts[2]
    $tag = "ios-source"
    & gh release view $tag 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Creating the rolling pre-release $tag"
        & gh release create $tag --prerelease --target main --title "iOS Xcode export (rolling)" --notes "The Xcode project exported by tools/build-ios.ps1 for the iOS TestFlight workflow. Replaced on every upload; not a game release."
    }
    Write-Host "Uploading $zip to the $tag pre-release..."
    & gh release upload $tag $zip --clobber
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host "Dispatching the iOS TestFlight workflow (version $version, build $buildNumber)..."
    & gh workflow run ios-testflight.yml -f build_number=$buildNumber -f version=$version -f source_tag=$tag
    Start-Sleep -Seconds 5
    & gh run list --workflow ios-testflight.yml --limit 1
}
