param(
    [string]$Configuration = "Debug"
)

$ProjectDir  = "D:\LethalMod\OverseerProtocol"
$GaleProfile = "D:\GaleGames\lethal-company\profiles\OverseerProtocol-Dev"
$PluginDest  = "$GaleProfile\BepInEx\plugins\OverseerProtocol"

# Build output for the plugin entry-point project.
# All dependent mod assemblies are copied here by MSBuild when the project
# references them without <Private>False</Private>.
$PluginSource = "$ProjectDir\src\OverseerProtocol.Plugin\bin\$Configuration\netstandard2.1"
$RuntimeDev   = "$ProjectDir\runtime\dev-plugin"

# ── Ensure destination folder exists ──────────────────────────────────────────
New-Item -ItemType Directory -Force -Path $PluginDest | Out-Null

Write-Host ""
Write-Host "=== OverseerProtocol Deploy ($Configuration) ===" -ForegroundColor Cyan
Write-Host "Source : $PluginSource"
Write-Host "Dest   : $PluginDest"
Write-Host ""

# ── Required mod assemblies ───────────────────────────────────────────────────
# These are the DLLs the CLR must find next to OverseerProtocol.dll at runtime.
# If any is missing the plugin will fail with FileNotFoundException on load.
$requiredDlls = @(
    "OverseerProtocol.dll",
    "OverseerProtocol.Core.dll",
    "OverseerProtocol.Data.dll",
    "OverseerProtocol.GameAbstractions.dll"
)

$copied  = 0
$missing = 0

foreach ($dll in $requiredDlls) {
    $src = Join-Path $PluginSource $dll
    if (Test-Path $src) {
        try {
            Copy-Item $src $PluginDest -Force -ErrorAction Stop
            Write-Host "  [OK]      $dll" -ForegroundColor Green
            $copied++
        } catch {
            Write-Host "  [LOCKED]  $dll - close Gale/Lethal Company before deploying." -ForegroundColor Red
            throw
        }
    } else {
        Write-Warning "  [MISSING] $src"
        $missing++
    }
}

# ── Debug symbols (optional, all OverseerProtocol*.pdb) ───────────────────────
Get-ChildItem $PluginSource -Filter "OverseerProtocol*.pdb" -ErrorAction SilentlyContinue |
    ForEach-Object {
        try {
            Copy-Item $_.FullName $PluginDest -Force -ErrorAction Stop
            Write-Host "  [PDB]     $($_.Name)" -ForegroundColor DarkGray
        } catch {
            Write-Host "  [LOCKED]  $($_.Name) - close Gale/Lethal Company before deploying." -ForegroundColor Red
            throw
        }
    }

# ── Runtime assets (optional) ─────────────────────────────────────────────────
if (Test-Path $RuntimeDev) {
    Copy-Item "$RuntimeDev\*" $PluginDest -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  [ASSETS]  runtime/dev-plugin copied" -ForegroundColor DarkGray
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
if ($missing -gt 0) {
    Write-Host "Deploy finished with WARNINGS: $copied copied, $missing missing." -ForegroundColor Yellow
    Write-Host "Run 'dotnet build' or build in Visual Studio before deploying." -ForegroundColor Yellow
} else {
    Write-Host "Deploy complete: $copied assemblies copied successfully." -ForegroundColor Green
}
Write-Host ""
