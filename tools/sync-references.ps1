param(
    [string]$GamePath = "",
    [string]$BepInExCorePath = ""
)

$ErrorActionPreference = "Stop"

$ProjectDir = Split-Path -Parent $PSScriptRoot
$RefBepInEx = Join-Path $ProjectDir "references\bepinex"
$RefGame = Join-Path $ProjectDir "references\game"

New-Item -ItemType Directory -Force -Path $RefBepInEx | Out-Null
New-Item -ItemType Directory -Force -Path $RefGame | Out-Null

function Resolve-ManagedPath {
    param([string]$Root)

    if ([string]::IsNullOrWhiteSpace($Root)) {
        return ""
    }

    $candidates = @(
        (Join-Path $Root "Lethal Company_Data\Managed"),
        (Join-Path $Root "LethalCompany_Data\Managed"),
        (Join-Path $Root "Managed"),
        $Root
    )

    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate "Assembly-CSharp.dll")) {
            return $candidate
        }
    }

    return ""
}

function Copy-Dlls {
    param(
        [string]$SourceDir,
        [string]$DestinationDir,
        [string[]]$Dlls,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($SourceDir) -or !(Test-Path $SourceDir)) {
        Write-Host "[WARN] $Label source not found: $SourceDir" -ForegroundColor Yellow
        return
    }

    Write-Host "Syncing $Label references from $SourceDir..."
    foreach ($dll in $Dlls) {
        $source = Join-Path $SourceDir $dll
        if (Test-Path $source) {
            Copy-Item $source (Join-Path $DestinationDir $dll) -Force
            Write-Host "  [OK] $dll" -ForegroundColor Green
        } else {
            Write-Host "  [MISS] $dll" -ForegroundColor Yellow
        }
    }
}

$BepInExDlls = @(
    "0Harmony.dll",
    "BepInEx.dll"
)

$GameDlls = @(
    "Assembly-CSharp.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "Unity.Netcode.Runtime.dll"
)

Copy-Dlls $BepInExCorePath $RefBepInEx $BepInExDlls "BepInEx"

$managedPath = Resolve-ManagedPath $GamePath
Copy-Dlls $managedPath $RefGame $GameDlls "game"

Write-Host "Sync process finished."
