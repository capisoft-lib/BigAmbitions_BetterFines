# Copies bundled runtime DLLs into Assets/Mods/BetterFines/Dependencies so Unity Mod Builder
# packages them (official CopyDependencies reads that folder). Does not modify SDK ModBuilder code.
param(
    [string] $SdkRoot
)

$ErrorActionPreference = "Stop"

$modRoot = (Get-Item $PSScriptRoot).Parent.FullName
$depsDir = Join-Path $modRoot "Dependencies"
New-Item -ItemType Directory -Path $depsDir -Force | Out-Null

if (-not $SdkRoot) {
    $SdkRoot = (Resolve-Path (Join-Path $modRoot "..\..\..")).Path
}

function Resolve-SourceDll {
    param(
        [Parameter(Mandatory = $true)][string] $FileName,
        [Parameter(Mandatory = $true)][string[]] $RelativeCandidates
    )

    foreach ($rel in $RelativeCandidates) {
        $path = Join-Path $SdkRoot ($rel -replace '/', '\')
        if (Test-Path $path) {
            return $path
        }
    }

    $modsLocal = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal"
    $fromModsLocal = Join-Path $modsLocal ($FileName -replace '\.dll$', '')
    $fromModsLocal = Join-Path $fromModsLocal $FileName
    if (Test-Path $fromModsLocal) {
        return $fromModsLocal
    }

    return $null
}

$uiSource = Resolve-SourceDll "LIB_BaUnifiedUI.dll" @(
    "Output\LIB_BaUnifiedUI\LIB_BaUnifiedUI.dll",
    "Assets\Mods\LIB_BaUnifiedUI\LIB_BaUnifiedUI.dll"
)
if (-not $uiSource) {
    throw "LIB_BaUnifiedUI.dll not found. Build LIB_BaUnifiedUI first (Unity Mod Builder or compile-install-lib-ba-unified-ui.ps1), then run tools/sync-dependencies.ps1."
}

$uiTarget = Join-Path $depsDir "LIB_BaUnifiedUI.dll"
Copy-Item $uiSource $uiTarget -Force
Write-Host "Copied LIB_BaUnifiedUI.dll"
Write-Host "  from: $uiSource"
Write-Host "  to:   $uiTarget"
Write-Host "Unity Mod Builder will include Dependencies DLLs in Output/BetterFines and ModsLocal."
