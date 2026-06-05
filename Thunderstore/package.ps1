<#
.SYNOPSIS
    Assemble an upload-ready Thunderstore package zip for VillageLife.

.DESCRIPTION
    Gathers the package assets in this folder (manifest.json, icon.png, README.md,
    CHANGELOG.md) plus a freshly built VillageLife.dll, verifies the version is in
    sync across manifest.json, Constants.cs and the .csproj, and writes
    dist/VillageLife-<version>.zip with the layout Thunderstore expects:

        manifest.json
        icon.png
        README.md
        CHANGELOG.md
        plugins/VillageLife.dll

    Build the DLL first (Release), then run this. Upload the resulting zip at
    https://thunderstore.io/ under your team. NOTE: a published version number is
    permanent and cannot be overwritten — bump the version for every release.

.EXAMPLE
    pwsh ./Thunderstore/package.ps1
    pwsh ./Thunderstore/package.ps1 -Configuration Debug
    pwsh ./Thunderstore/package.ps1 -DllPath "D:\build\VillageLife.dll"
#>
[CmdletBinding()]
param(
    # Build configuration to pull the DLL from when -DllPath isn't given.
    [string]$Configuration = "Release",
    # Explicit path to the built VillageLife.dll (overrides -Configuration lookup).
    [string]$DllPath,
    # Where to write the zip (defaults to <repo>/dist).
    [string]$OutputDir
)

$ErrorActionPreference = "Stop"

# Resolve paths relative to this script so it runs from anywhere. Thunderstore/ sits
# at the repo root and holds the canonical package assets.
$pkgDir   = $PSScriptRoot
$repoRoot = Split-Path -Parent $pkgDir
if (-not $OutputDir) { $OutputDir = Join-Path $repoRoot "dist" }

# --- Verify the package assets are all present ------------------------------
$manifestPath  = Join-Path $pkgDir "manifest.json"
$iconPath      = Join-Path $pkgDir "icon.png"
$readmePath    = Join-Path $pkgDir "README.md"
$changelogPath = Join-Path $pkgDir "CHANGELOG.md"
foreach ($p in @($manifestPath, $iconPath, $readmePath, $changelogPath)) {
    if (-not (Test-Path $p)) { throw "Missing package asset: $p" }
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$version  = $manifest.version_number
if ([string]::IsNullOrWhiteSpace($version)) { throw "manifest.json has no version_number." }
if ($version -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$') {
    throw "manifest version '$version' is not semver (Thunderstore requires major.minor.patch)."
}

# --- Version-sync guard: manifest must match Constants.cs and the .csproj ----
$constants = Get-Content (Join-Path $repoRoot "VillageLife/Util/Constants.cs") -Raw
$csproj    = Get-Content (Join-Path $repoRoot "VillageLife/VillageLife.csproj") -Raw
$constVer  = [regex]::Match($constants, 'PluginVersion\s*=\s*"([0-9]+\.[0-9]+\.[0-9]+)"').Groups[1].Value
$projVer   = [regex]::Match($csproj,    '<Version>([0-9]+\.[0-9]+\.[0-9]+)</Version>').Groups[1].Value
if ($version -ne $constVer -or $version -ne $projVer) {
    throw "Version mismatch — manifest=$version, Constants.cs=$constVer, csproj=$projVer. Bump all three before packaging."
}

# --- Locate the built DLL ----------------------------------------------------
if (-not $DllPath) {
    $DllPath = Join-Path $repoRoot "VillageLife/bin/$Configuration/net472/VillageLife.dll"
}
if (-not (Test-Path $DllPath)) {
    throw "Built DLL not found at '$DllPath'. Build VillageLife ($Configuration) first — see README > Building."
}

# --- Stage the exact zip layout and compress --------------------------------
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("vl_pkg_" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path (Join-Path $staging "plugins") -Force | Out-Null
try {
    Copy-Item $manifestPath  (Join-Path $staging "manifest.json")
    Copy-Item $iconPath      (Join-Path $staging "icon.png")
    Copy-Item $readmePath    (Join-Path $staging "README.md")
    Copy-Item $changelogPath (Join-Path $staging "CHANGELOG.md")
    Copy-Item $DllPath       (Join-Path $staging "plugins/VillageLife.dll")

    if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
    $zipPath = Join-Path $OutputDir "VillageLife-$version.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -Force

    Write-Host "Packaged $zipPath" -ForegroundColor Green
    Write-Host "Next: upload it at https://thunderstore.io/ under your team."
    Write-Host "Reminder: version $version is permanent once published — bump for the next release."
}
finally {
    Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
}
