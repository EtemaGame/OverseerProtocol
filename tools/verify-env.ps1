param(
    [string]$ProjectDir = "",
    [string]$GamePath = "",
    [string]$ProfilePath = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectDir)) {
    $ProjectDir = Split-Path -Parent $PSScriptRoot
}

$RefBepInEx = Join-Path $ProjectDir "references\bepinex"
$RefGame = Join-Path $ProjectDir "references\game"

function Check-Path {
    param([string]$Path, [string]$Name, [bool]$Required = $true)

    if (Test-Path $Path) {
        Write-Host "[OK] $Name exists: $Path" -ForegroundColor Green
        return $true
    }

    $label = if ($Required) { "ERROR" } else { "WARN" }
    $color = if ($Required) { "Red" } else { "Yellow" }
    Write-Host "[$label] $Name not found: $Path" -ForegroundColor $color
    return -not $Required
}

Write-Host "Verifying OverseerProtocol Dev Environment..."
Write-Host "------------------------------------------------"

$allOk = $true
$allOk = (Check-Path $ProjectDir "Project directory") -and $allOk
$allOk = (Check-Path $RefBepInEx "Local BepInEx references") -and $allOk
$allOk = (Check-Path $RefGame "Local game references") -and $allOk

$allOk = (Check-Path (Join-Path $RefBepInEx "BepInEx.dll") "BepInEx.dll") -and $allOk
$allOk = (Check-Path (Join-Path $RefBepInEx "0Harmony.dll") "0Harmony.dll") -and $allOk
$allOk = (Check-Path (Join-Path $RefGame "Assembly-CSharp.dll") "Assembly-CSharp.dll") -and $allOk
$allOk = (Check-Path (Join-Path $RefGame "Unity.Netcode.Runtime.dll") "Unity.Netcode.Runtime.dll") -and $allOk
$allOk = (Check-Path (Join-Path $RefGame "UnityEngine.dll") "UnityEngine.dll") -and $allOk
$allOk = (Check-Path (Join-Path $RefGame "UnityEngine.CoreModule.dll") "UnityEngine.CoreModule.dll") -and $allOk
$allOk = (Check-Path (Join-Path $RefGame "UnityEngine.UIModule.dll") "UnityEngine.UIModule.dll") -and $allOk

if (![string]::IsNullOrWhiteSpace($GamePath)) {
    Check-Path $GamePath "Local Lethal Company install" $false | Out-Null
}

if (![string]::IsNullOrWhiteSpace($ProfilePath)) {
    Check-Path $ProfilePath "BepInEx/Gale dev profile" $false | Out-Null
}

Write-Host "------------------------------------------------"
if ($allOk) {
    Write-Host "Verification SUMMARY: OK" -ForegroundColor Green
} else {
    Write-Host "Verification SUMMARY: MISSING REQUIRED REFERENCES" -ForegroundColor Red
    Write-Host "Run tools/setup-references.ps1 with a local Lethal Company -GamePath to copy game assemblies." -ForegroundColor Yellow
}
