# Creates the Google Play upload keystore once, OUTSIDE the repo: D:\Cloclo\Keys\vacuum-android.keystore with a
# random password saved next to it (vacuum-android.pass). tools\build-android.ps1 reads both. With Play App Signing
# this is only the upload key (Google keeps the app signing key), but back the two files up anyway.
# Uses the JDK that ships with the Android module, or keytool from the PATH.
# Usage:  powershell -File tools\make-keystore.ps1
. "$PSScriptRoot\common.ps1"

$dir = "D:\Cloclo\Keys"
$keystore = Join-Path $dir "vacuum-android.keystore"
$passFile = Join-Path $dir "vacuum-android.pass"
if (Test-Path $keystore) { Write-Host "Keystore already there: $keystore"; exit 0 }
New-Item -ItemType Directory -Force $dir | Out-Null

$keytool = Join-Path $UnityRoot "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
if (-not (Test-Path $keytool)) { $keytool = "keytool" }

$bytes = New-Object byte[] 24
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$pass = ([Convert]::ToBase64String($bytes)) -replace '[^A-Za-z0-9]', 'x'
Set-Content -Path $passFile -Value $pass -Encoding ascii -NoNewline

& $keytool -genkeypair -v -keystore $keystore -alias vacuum -keyalg RSA -keysize 2048 -validity 10000 `
    -storepass $pass -keypass $pass -dname "CN=Cosnuau, O=Cosnuau, C=CH" 2>&1 | Select-Object -Last 3
if (Test-Path $keystore) {
    Write-Host "Keystore created: $keystore (alias vacuum, password in $passFile). Back both up."
    exit 0
}
Write-Host "keytool failed"
exit 1
