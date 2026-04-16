# Hybrid Configuration V1

OverseerProtocol now has two configuration layers:

- `.cfg`: Simple user-facing switches, preset selection, and broad multipliers.
- JSON: Detailed entity-level overrides for items and spawns.

## BepInEx Config

BepInEx generates the config at:

```text
BepInEx/config/com.overseerprotocol.core.cfg
```

Current entries:

```ini
[General]
EnableDataExport = true
ActivePreset = default

[Overrides]
EnableItemOverrides = true
EnableMoonOverrides = true
EnableSpawnOverrides = true

[Validation]
StrictValidation = false
DryRunOverrides = false

[Multipliers]
EnableRuntimeMultipliers = true
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1
RoutePriceMultiplier = 1

[SemanticDifficulty]
AggressionProfile = Balanced

[Progression]
EnableProgressionStorage = true
EnablePerkCatalog = true

[Lobby]
EnableLobbyRulesLoading = true

[RuntimeRules]
EnableRuntimeRulesLoading = true
```

## Preset Resolution

`ActivePreset = default` reads JSON overrides from:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/overrides/
```

Any other preset name reads JSON overrides from:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-name>/overrides/
```

For example:

```ini
[General]
ActivePreset = hardcore
```

Reads:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/hardcore/overrides/items.override.json
BepInEx/plugins/OverseerProtocol/overseer-data/presets/hardcore/overrides/moons.override.json
BepInEx/plugins/OverseerProtocol/overseer-data/presets/hardcore/overrides/spawns.override.json
```

Preset names are sanitized before becoming folder names.

## Runtime Multipliers

Runtime multipliers run after JSON overrides.

- `ItemWeightMultiplier`: Multiplies every runtime item `weight`.
- `SpawnRarityMultiplier`: Multiplies every runtime spawn rarity in inside, outside, and daytime pools.
- `RoutePriceMultiplier`: Multiplies every runtime terminal route price.
- Spawn rarities are clamped to `0..1000`.
- Multiplier values are clamped by config to `0..10`.

Exports still happen before overrides and multipliers when `EnableDataExport = true`, so exported catalogs remain the vanilla snapshot.

## Validation Controls

- `StrictValidation`: when enabled, any validation warning aborts the affected override flow. Validation errors always abort.
- `DryRunOverrides`: when enabled, the mod still exports and validates configuration, but it does not apply item overrides, spawn overrides, or runtime multipliers.
- `AbortOnInvalidOverrideBlock`: reserved strict policy flag for future validators that should abort instead of skipping invalid blocks.

Before export, OverseerProtocol captures a lightweight in-memory runtime snapshot for item values and moon spawn pools. The snapshot is restored after export and before overrides, keeping the exported catalogs and the mutation baseline aligned with vanilla runtime state.

## Progression Storage

`EnableProgressionStorage` creates and loads the V1 progression save file at:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/saves/progression.json
```

This is data-only for now. Runtime perks are intentionally deferred until the override and admin tooling layers are stable.

## Lobby Rules

`EnableLobbyRulesLoading` creates and loads the V1 lobby rules file. This is also data-only for now; expanded lobby, late join, spectator mode, and handshake enforcement remain future runtime work.

## Runtime Rules

`EnableRuntimeRulesLoading` creates and loads the V1 runtime rules file for future quota, deadline, travel discount, ship timing, weather reward, and moon-specific balancing.
