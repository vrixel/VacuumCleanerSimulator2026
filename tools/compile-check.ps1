# Compiles the game scripts against the installed Unity assemblies with the .NET SDK.
# Catches compile errors in seconds without opening the editor (no license needed).
# Usage:  powershell -File tools\compile-check.ps1
# Output: tools\.check\ (gitignored)
#
# References: Editor\Data\Managed\UnityEngine\UnityEngine*.dll (runtime) plus UnityEditor*.dll from the same
# folder (editor scripts), and the precompiled UnityEngine.UI.dll that Unity keeps in its project-template cache.

. "$PSScriptRoot\common.ps1"

$managedEngine = Join-Path $UnityRoot "Data\Managed\UnityEngine"
$check = Join-Path $RepoRoot "tools\.check"
New-Item -ItemType Directory -Force $check | Out-Null

if (-not (Test-Path $managedEngine)) {
    Write-Host "UnityEngine assemblies not found under $managedEngine"
    exit 2
}

$uiDll = Get-ChildItem (Join-Path $UnityRoot "Data\Resources\PackageManager\ProjectTemplates\libcache") -Recurse -Filter "UnityEngine.UI.dll" -ErrorAction SilentlyContinue |
    Select-Object -First 1 -ExpandProperty FullName
if (-not $uiDll) {
    $uiDll = Get-ChildItem (Join-Path $RepoRoot "Library\ScriptAssemblies") -Filter "UnityEngine.UI.dll" -ErrorAction SilentlyContinue |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $uiDll) {
    Write-Host "UnityEngine.UI.dll not found (template cache or Library\ScriptAssemblies)."
    exit 2
}
Write-Host "UnityEngine.UI: $uiDll"

$engineRefs = Get-ChildItem $managedEngine -Filter "UnityEngine*.dll" | Select-Object -ExpandProperty FullName
$editorRefs = Get-ChildItem $managedEngine -Filter "UnityEditor*.dll" | Select-Object -ExpandProperty FullName

function New-Csproj {
    param($Path, $AssemblyName, [string[]]$CompileGlobs, [string[]]$References, [string]$ExtraDefines, [string]$Tfm)
    $refs = @()
    foreach ($f in $References) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($f)
        $refs += "    <Reference Include=`"$name`"><HintPath>$f</HintPath><Private>false</Private></Reference>"
    }
    $compiles = @()
    foreach ($g in $CompileGlobs) { $compiles += "    <Compile Include=`"$g`" />" }
    $defines = "UNITY_5_3_OR_NEWER;UNITY_2017_1_OR_NEWER;UNITY_2018_1_OR_NEWER;UNITY_2019_1_OR_NEWER;UNITY_2020_1_OR_NEWER;UNITY_2021_1_OR_NEWER;UNITY_2022_1_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_6000_0_OR_NEWER;UNITY_STANDALONE;UNITY_STANDALONE_WIN;ENABLE_LEGACY_INPUT_MANAGER;UNITY_UGUI"
    if ($ExtraDefines) { $defines += ";" + $ExtraDefines }
    $xml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$Tfm</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <AssemblyName>$AssemblyName</AssemblyName>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <EnableDefaultItems>false</EnableDefaultItems>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <NoWarn>CS0618;CS0649;CS0108;CS0414;CS0169;CS1591;CS0109;CS0162;CS0219;CS8632;MSB3277;MSB3270</NoWarn>
    <DefineConstants>$defines</DefineConstants>
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

New-Csproj -Path (Join-Path $check "Runtime.csproj") -AssemblyName "Assembly-CSharp" `
    -CompileGlobs @("$RepoRoot\Assets\Scripts\**\*.cs") `
    -References ($engineRefs + @($uiDll)) -ExtraDefines "" -Tfm "netstandard2.1"

# Engine modules target .NET Standard 2.1; the editor modules are .NET Framework, so the editor project stays on net48.
New-Csproj -Path (Join-Path $check "Editor.csproj") -AssemblyName "Assembly-CSharp-Editor" `
    -CompileGlobs @("$RepoRoot\Assets\Editor\**\*.cs") `
    -References ($engineRefs + $editorRefs + @($uiDll)) -ExtraDefines "UNITY_EDITOR;UNITY_EDITOR_WIN;UNITY_EDITOR_64" -Tfm "net48"

$failed = $false
foreach ($proj in @("Runtime.csproj", "Editor.csproj")) {
    Write-Host ""
    Write-Host "=== $proj ==="
    $out = & dotnet build (Join-Path $check $proj) -nologo -v q -p:UseSharedCompilation=false 2>&1
    $code = $LASTEXITCODE
    $out | Where-Object { $_ -match "error|Error\(s\)|Warning\(s\)" } | Select-Object -Unique | ForEach-Object { Write-Host $_ }
    if ($code -ne 0) { $failed = $true; Write-Host "FAILED (exit $code)" } else { Write-Host "ok" }
}
Write-Host ""
if ($failed) { Write-Host "COMPILE CHECK FAILED"; exit 1 }
Write-Host "COMPILE CHECK OK"
