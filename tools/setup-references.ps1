param(
    [string]$GamePath = "",
    [switch]$DownloadBepInEx
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ReferencesRoot = Join-Path $ProjectRoot "references"
$BepInExRefs = Join-Path $ReferencesRoot "bepinex"
$GameRefs = Join-Path $ReferencesRoot "game"
$Downloads = Join-Path $ProjectRoot ".downloads"

New-Item -ItemType Directory -Force -Path $BepInExRefs | Out-Null
New-Item -ItemType Directory -Force -Path $GameRefs | Out-Null

function Copy-IfExists {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (Test-Path $Source) {
        Copy-Item -Force -Path $Source -Destination $Destination
        Write-Host "[OK] Copied $(Split-Path -Leaf $Source)" -ForegroundColor Green
        return $true
    }

    Write-Host "[MISS] $Source" -ForegroundColor Yellow
    return $false
}

function Resolve-GameManagedPath {
    param([string]$Root)

    if ([string]::IsNullOrWhiteSpace($Root)) {
        return ""
    }

    $candidates = @(
        (Join-Path $Root "Lethal Company_Data\Managed"),
        (Join-Path $Root "LethalCompany_Data\Managed"),
        (Join-Path $Root "Managed")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate "Assembly-CSharp.dll")) {
            return $candidate
        }
    }

    return ""
}

if ($DownloadBepInEx) {
    New-Item -ItemType Directory -Force -Path $Downloads | Out-Null
    $zipPath = Join-Path $Downloads "BepInEx_x64_5.4.22.0.zip"
    $extractPath = Join-Path $Downloads "BepInEx_x64_5.4.22.0"
    $url = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.22/BepInEx_x64_5.4.22.0.zip"

    if (!(Test-Path $zipPath)) {
        Write-Host "[INFO] Downloading BepInEx from official GitHub release..."
        Invoke-WebRequest -Uri $url -OutFile $zipPath
    }

    if (!(Test-Path $extractPath)) {
        Expand-Archive -Force -Path $zipPath -DestinationPath $extractPath
    }

    $corePath = Join-Path $extractPath "BepInEx\core"
    Copy-IfExists (Join-Path $corePath "BepInEx.dll") $BepInExRefs | Out-Null
    Copy-IfExists (Join-Path $corePath "0Harmony.dll") $BepInExRefs | Out-Null
}

$managedPath = Resolve-GameManagedPath $GamePath
if ([string]::IsNullOrWhiteSpace($managedPath)) {
    Write-Host "[WARN] GamePath was not provided or Assembly-CSharp.dll was not found." -ForegroundColor Yellow
    Write-Host "[WARN] Provide a local Lethal Company install path, for example:" -ForegroundColor Yellow
    Write-Host "       pwsh tools/setup-references.ps1 -GamePath 'C:\Program Files (x86)\Steam\steamapps\common\Lethal Company' -DownloadBepInEx"
} else {
    Write-Host "[INFO] Copying game references from $managedPath"
    Copy-IfExists (Join-Path $managedPath "Assembly-CSharp.dll") $GameRefs | Out-Null
    Copy-IfExists (Join-Path $managedPath "Unity.Netcode.Runtime.dll") $GameRefs | Out-Null
    Copy-IfExists (Join-Path $managedPath "UnityEngine.dll") $GameRefs | Out-Null
    Copy-IfExists (Join-Path $managedPath "UnityEngine.CoreModule.dll") $GameRefs | Out-Null
}

Write-Host "[DONE] References setup completed."
