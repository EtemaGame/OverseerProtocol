# Runtime Test Checklist

This branch intentionally advanced features without runtime testing. Use this checklist when a real Lethal Company/BepInEx environment is available.

## 1. Build And Deploy

- Build the solution.
- Deploy the plugin.
- Confirm BepInEx loads `com.overseerprotocol.core`.
- Confirm no loader errors for new classes or missing references.

Optional JSON sanity check from the repo root:

```text
.\node-v24.14.1-win-x64\node.exe tools\validate-json.mjs data\sample-overrides\spawns.override.json data\sample-overrides\moons.override.json data\sample-overrides\runtime-rules.json
```

Reference sanity check:

```text
powershell -ExecutionPolicy Bypass -File tools\verify-env.ps1
```

Before copying local game assemblies, this should report BepInEx references as OK and game references as missing.

## 2. First Startup

Expected generated folders:

- `overseer-data/exports/`
- `overseer-data/overrides/`
- `overseer-data/presets/`
- `overseer-data/saves/`
- `overseer-data/rules/`

Expected generated files:

- `saves/progression.json`
- `definitions/perks.json`
- `rules/lobby-rules.json` or preset-specific `rules/lobby-rules.json`
- `rules/runtime-rules.json` or preset-specific `rules/runtime-rules.json`
- built-in preset manifests and empty override templates.

## 3. Export Baseline

With `EnableDataExport = true`, confirm exports are generated:

- `items/items.json`
- `moons/moons.json`
- `enemies/enemies.json`
- `spawns/moon-spawn-profiles.json`
- `economy/moon-economy.json`

Check that `moons.json` includes `levelIndex`, `routePrice`, and `riskLevel`.

## 4. Dry Run

Set:

```ini
[Validation]
DryRunOverrides = true
StrictValidation = false
```

Expected behavior:

- JSON files load.
- Validation logs appear.
- Item, spawn, moon, and route multiplier runtime mutations are skipped.

## 5. Item Overrides

Use one known item ID from `exports/items/items.json`.

Test:

- Valid `creditsWorth`.
- Valid `weight`.
- Unknown ID logs a warning and skips.
- Negative values clamp where expected.

## 6. Spawn Overrides

Use known moon and enemy IDs from exports.

Test:

- Omitted pool keeps vanilla.
- Empty pool replaces with empty.
- Unknown enemy skips only that entry.
- Unknown moon skips the whole moon override.

## 7. Moon Overrides

Use one known moon ID from `exports/moons/moons.json`.

Test:

- `riskLevel` changes terminal/level risk label.
- `riskLabel` wins over `riskLevel`.
- `routePrice` changes terminal route cost.
- Missing route node logs a warning and does not crash.

## 8. Presets

Set:

```ini
[General]
ActivePreset = hardcore
```

Expected:

- Preset manifest loads.
- Preset override folder is used.
- `.cfg` multiplier values other than `1` override preset multiplier defaults.

## 9. Runtime Multipliers

Test independently:

- `ItemWeightMultiplier`
- `SpawnRarityMultiplier`
- `RoutePriceMultiplier`
- `AggressionProfile`

Confirm multipliers run after JSON overrides.

## 10. Advanced Contracts

These are data-only for now:

- `progression.json`
- perk catalog models
- `lobby-rules.json`
- `runtime-rules.json`
- protocol handshake models
- `AdminCommandService`

Do not expect perks, expanded lobby, late join, spectator mode, or terminal UI hooks to work until their runtime integrations are implemented.

## 11. Admin Command Service

Once terminal hook testing is available, verify:

- `op export`
- `op reload`
- `op reset`
- `op fingerprint`
- `op rules`
- `op perks`
- `op progression`
- `op progression grant ship 100`
- `op progression reset ship`
- `op handshake`

## 12. Experimental Admin Terminal Hook

Set:

```ini
[Admin]
EnableAdminTerminalCommands = true
AdminCommandPrefix = op
```

Expected:

- `op help` renders an OverseerProtocol response.
- Vanilla terminal commands still work.
- Disabling `EnableAdminTerminalCommands` returns to vanilla behavior.
