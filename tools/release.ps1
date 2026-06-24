# Builds a complete pixory release — the portable self-contained exe and the
# Windows installer — both named with the version read from pixory.csproj.
#
#   .\tools\release.ps1                   -> artifacts land in dist\release
#   .\tools\release.ps1 -OutDir <folder>  -> also copied into <folder>
#
# The -OutDir hook lets the outer workspace collect the build into builds\pixory
# without this (public) script having to know that private path.
param(
    [string]$OutDir
)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'pixory\pixory.csproj'

# Single source of truth for the version: <Version> in the csproj.
[xml]$csproj = Get-Content $project
$version = $csproj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "Could not read <Version> from $project" }
Write-Output "Building pixory v$version"

# 1) Self-contained single-file exe (~68 MB) — runs without .NET installed.
$selfContainedDir = Join-Path $root 'dist\win-x64'
dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $selfContainedDir

# 2) Installer that wraps the exe above, compiled with Inno Setup.
$iscc = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
if (-not (Test-Path $iscc)) { $iscc = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe' }
if (-not (Test-Path $iscc)) {
    throw "ISCC.exe (Inno Setup) not found. Install it: winget install JRSoftware.InnoSetup"
}
& $iscc "/DAppVersion=$version" (Join-Path $PSScriptRoot 'installer.iss')
if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }

# 3) Gather both, version-named, under dist\release.
$releaseDir = Join-Path $root 'dist\release'
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
$portable = Join-Path $releaseDir "pixory-v$version.exe"
$setup    = Join-Path $releaseDir "pixory-setup-v$version.exe"
Copy-Item (Join-Path $selfContainedDir 'pixory.exe') $portable -Force
Copy-Item (Join-Path $root "dist\installer\pixory-setup-v$version.exe") $setup -Force

# 4) Optionally drop the same two files into an external collection folder.
if ($OutDir) {
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    Copy-Item $portable $OutDir -Force
    Copy-Item $setup $OutDir -Force
}

Write-Output ''
Write-Output 'Release artifacts (dist\release):'
Get-ChildItem $releaseDir -Filter "*v$version.exe" | ForEach-Object {
    Write-Output ('  {0,-34} {1,6:N1} MB' -f $_.Name, ($_.Length / 1MB))
}
if ($OutDir) { Write-Output "Also copied to: $OutDir" }
