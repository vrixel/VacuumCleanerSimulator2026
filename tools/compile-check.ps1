# Compiles the game scripts against the installed Unity assemblies with the .NET SDK.
# Catches compile errors in seconds without opening the editor (no license needed).
# Usage:  powershell -File tools\compile-check.ps1
# Output: tools\.check\ (gitignored)

. "$PSScriptRoot\common.ps1"

$managed = Join-Path $UnityRoot "Data\Managed"
$engineDir = Join-Path $managed "UnityEngine"
$check = Join-Path $RepoRoot "tools\.check"
New-Item -ItemType Directory -Force $check | Out-Null

if (-not (Test-Path $engineDir)) {
    Write-Host "UnityEngine assemblies not found under $engineDir"
    exit 2
}

# uGUI ships as a built-in package with the editor; its runtime sources are compiled alongside our scripts.
$uguiRoot = Join-Path $UnityRoot "Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui"
if (-not (Test-Path $uguiRoot)) {
    $cached = Get-ChildItem (Join-Path $RepoRoot "Library\PackageCache") -Directory -Filter "com.unity.ugui@*" -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($cached) { $uguiRoot = $cached.FullName }
}
$uguiRuntime = Join-Path $uguiRoot "Runtime\UGUI"
if (-not (Test-Path $uguiRuntime)) { $uguiRuntime = Join-Path $uguiRoot "Runtime" }
Write-Host "uGUI sources: $uguiRuntime"

function New-Csproj {
    param($Path, $AssemblyName, [string[]]$CompileGlobs, [string[]]$ReferenceDirs, [string[]]$ReferenceFiles)
    $refs = @()
    foreach ($dir in $ReferenceDirs) {
        if (Test-Path $dir) {
            foreach ($dll in Get-ChildItem $dir -Filter "*.dll") {
                $refs += "    <Reference Include=`"$($dll.BaseName)`"><HintPath>$($dll.FullName)</HintPath><Private>false</Private></Reference>"
            }
        }
    }
    foreach ($f in $ReferenceFiles) {
        if (Test-Path $f) {
            $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
            $refs += "    <Reference Include=`"$name`"><HintPath>$f</HintPath><Private>false</Private></Reference>"
        }
    }
    $compiles = @()
    foreach ($g in $CompileGlobs) { $compiles += "    <Compile Include=`"$g`" />" }
    $xml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AssemblyName>$AssemblyName</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <EnableDefaultItems>false</EnableDefaultItems>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningLevel>2</WarningLevel>
    <NoWarn>CS0618;CS0649;CS0108;CS0414;CS0169;CS1591;CS0109;CS0162;CS0219;CS8632;MSB3277;MSB3270</NoWarn>
    <DefineConstants>UNITY_5_3_OR_NEWER;UNITY_2017_1_OR_NEWER;UNITY_2018_1_OR_NEWER;UNITY_2019_1_OR_NEWER;UNITY_2020_1_OR_NEWER;UNITY_2021_1_OR_NEWER;UNITY_2022_1_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;UNITY_STANDALONE;UNITY_STANDALONE_WIN;ENABLE_LEGACY_INPUT_MANAGER;UNITY_UGUI</DefineConstants>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <OutputType>Library</OutputType>
  </PropertyGroup>
  <ItemGroup>
$($compiles -join "`n")
  </ItemGroup>
  <ItemGroup>
$($refs -join "`n")
  </ItemGroup>
</Project>
"@
    Set-Content -Path $Path -Value $xml -Encoding UTF8
}

$editorRefs = @()
$editorDll = Join-Path $managed "UnityEditor.dll"
if (Test-Path $editorDll) { $editorRefs += $editorDll }
$editorDir = Join-Path $managed "UnityEditor"

New-Csproj -Path (Join-Path $check "Runtime.csproj") -AssemblyName "Assembly-CSharp" `
    -CompileGlobs @("$RepoRoot\Assets\Scripts\**\*.cs", "$uguiRuntime\**\*.cs") `
    -ReferenceDirs @($engineDir) -ReferenceFiles @()

New-Csproj -Path (Join-Path $check "Editor.csproj") -AssemblyName "Assembly-CSharp-Editor" `
    -CompileGlobs @("$RepoRoot\Assets\Editor\**\*.cs") `
    -ReferenceDirs @($engineDir, $editorDir) -ReferenceFiles $editorRefs

$failed = $false
foreach ($proj in @("Runtime.csproj", "Editor.csproj")) {
    Write-Host ""
    Write-Host "=== $proj ==="
    & dotnet build (Join-Path $check $proj) -nologo -v q -p:UseSharedCompilation=false 2>&1 | Where-Object { $_ -notmatch "warning" }
    if ($LASTEXITCODE -ne 0) { $failed = $true }
}
Write-Host ""
if ($failed) { Write-Host "COMPILE CHECK FAILED"; exit 1 }
Write-Host "COMPILE CHECK OK"
