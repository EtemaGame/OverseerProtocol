param()

$ProjectDir = "D:\LethalMod\OverseerProtocol"
$GaleProfile = "D:\GaleGames\lethal-company\profiles\OverseerProtocol-Dev"
$BepInExDir = "$GaleProfile\BepInEx\core"
$GameDir = "D:\Steam\steamapps\common\Lethal Company\Lethal Company_Data\Managed" # Cambiar si es diferente

$RefBepInEx = "$ProjectDir\references\bepinex"
$RefGame = "$ProjectDir\references\game"

if (-not (Test-Path $RefBepInEx)) { New-Item -ItemType Directory -Path $RefBepInEx -Force | Out-Null }
if (-not (Test-Path $RefGame)) { New-Item -ItemType Directory -Path $RefGame -Force | Out-Null }

$BepInExDlls = @(
    "0Harmony.dll",
    "BepInEx.dll",
    "BepInEx.Harmony.dll",
    "BepInEx.Preloader.dll",
    "HarmonyXInterop.dll",
    "MonoMod.RuntimeDetour.dll",
    "MonoMod.Utils.dll",
    "Mono.Cecil.dll",
    "Mono.Cecil.Mdb.dll",
    "Mono.Cecil.Pdb.dll",
    "Mono.Cecil.Rocks.dll"
)

$GameDlls = @(
    "Assembly-CSharp.dll",
    "Assembly-CSharp-firstpass.dll",
    "UnityEngine.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.InputModule.dll",
    "UnityEngine.UI.dll",
    "UnityEngine.TextRenderingModule.dll",
    "Unity.TextMeshPro.dll",
    "Unity.InputSystem.dll",
    "Unity.Netcode.Runtime.dll",
    "Unity.Netcode.Components.dll",
    "Unity.Collections.dll",
    "Unity.Mathematics.dll",
    "Unity.Networking.Transport.dll",
    "ClientNetworkTransform.dll",
    "Facepunch.Steamworks.Win64.dll",
    "Facepunch Transport for Netcode for GameObjects.dll",
    "DissonanceVoip.dll",
    "Newtonsoft.Json.dll"
)

Write-Host "Syncing BepInEx references..."
foreach ($dll in $BepInExDlls) {
    if (Test-Path "$BepInExDir\$dll") {
        Copy-Item "$BepInExDir\$dll" "$RefBepInEx\$dll" -Force
        Write-Host "  Copied $dll"
    } else {
        Write-Host "  [MISSING] $dll from BepInEx" -ForegroundColor Yellow
    }
}

if (-not (Test-Path $GameDir)) {
    Write-Host "WARNING: Game Managed directory not found at $GameDir. Game references could not be copied." -ForegroundColor Yellow
} else {
    Write-Host "Syncing Game references..."
    foreach ($dll in $GameDlls) {
        if (Test-Path "$GameDir\$dll") {
            Copy-Item "$GameDir\$dll" "$RefGame\$dll" -Force
            Write-Host "  Copied $dll"
        } else {
            Write-Host "  [MISSING] $dll from Game" -ForegroundColor Yellow
        }
    }
}

Write-Host "Sync process finished."
