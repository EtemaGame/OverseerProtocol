param()

$ErrorActionPreference = "Stop"

$ProjectDir = "D:\LethalMod\OverseerProtocol"
$GaleProfile = "D:\GaleGames\lethal-company\profiles\OverseerProtocol-Dev"
$BepInExDir = "$GaleProfile\BepInEx\core"
$RefBepInEx = "$ProjectDir\references\bepinex"
$RefGame = "$ProjectDir\references\game"

function Check-Path {
    param($Path, $Name)
    if (Test-Path $Path) {
        Write-Host "[OK] $Name exists: $Path" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] $Name not found: $Path" -ForegroundColor Red
        return $false
    }
    return $true
}

Write-Host "Verifying OverseerProtocol Dev Environment..."
Write-Host "------------------------------------------------"

$allOk = $true
$allOk = Check-Path $ProjectDir "Project directory" -and $allOk
$allOk = Check-Path $GaleProfile "Gale Profile directory" -and $allOk
$allOk = Check-Path $BepInExDir "BepInEx core directory" -and $allOk
$allOk = Check-Path $RefBepInEx "Local BepInEx references" -and $allOk
$allOk = Check-Path $RefGame "Local Game references" -and $allOk

$allOk = Check-Path "$RefBepInEx\BepInEx.dll" "BepInEx.dll" -and $allOk
$allOk = Check-Path "$RefBepInEx\0Harmony.dll" "0Harmony.dll" -and $allOk
$allOk = Check-Path "$RefGame\Assembly-CSharp.dll" "Assembly-CSharp.dll" -and $allOk

Write-Host "------------------------------------------------"
if ($allOk) {
    Write-Host "Verification SUMMARY: OK" -ForegroundColor Green
} else {
    Write-Host "Verification SUMMARY: ERROR" -ForegroundColor Red
}
