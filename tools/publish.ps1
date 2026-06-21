# Builds both shareable pixory packages and gathers them under dist/release:
#
#   pixory.exe       self-contained (~68 MB) — runs without installing .NET
#   pixory-lite.exe  framework-dependent (~0.4 MB) — needs the .NET 8 Desktop
#                      Runtime (Windows prompts to install it on first run if it
#                      is missing)
$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'pixory\pixory.csproj'
$selfContainedDir = Join-Path $root 'dist\win-x64'
$liteDir = Join-Path $root 'dist\win-x64-fxdep'
$releaseDir = Join-Path $root 'dist\release'

# Self-contained: bundles the .NET + WPF runtime so it runs on any Windows box.
dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $selfContainedDir

# Framework-dependent: tiny, relies on an installed .NET 8 Desktop Runtime.
dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true `
    -o $liteDir

# Collect both under dist/release with clear, distinct names for the upload.
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Copy-Item (Join-Path $selfContainedDir 'pixory.exe') (Join-Path $releaseDir 'pixory.exe') -Force
Copy-Item (Join-Path $liteDir 'pixory.exe') (Join-Path $releaseDir 'pixory-lite.exe') -Force

Write-Output ''
Write-Output 'Release assets (dist/release):'
Get-ChildItem $releaseDir -Filter *.exe | ForEach-Object {
    Write-Output ('  {0,-20} {1,6:N1} MB' -f $_.Name, ($_.Length / 1MB))
}
