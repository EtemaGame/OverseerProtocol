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
EnableSpawnOverrides = true

[Multipliers]
EnableRuntimeMultipliers = true
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1

[SemanticDifficulty]
AggressionProfile = Balanced
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
BepInEx/plugins/OverseerProtocol/overseer-data/presets/hardcore/overrides/spawns.override.json
```

Preset names are sanitized before becoming folder names.

## Runtime Multipliers

Runtime multipliers run after JSON overrides.

- `ItemWeightMultiplier`: Multiplies every runtime item `weight`.
- `SpawnRarityMultiplier`: Multiplies every runtime spawn rarity in inside, outside, and daytime pools.
- Spawn rarities are clamped to `0..1000`.
- Multiplier values are clamped by config to `0..10`.

Exports still happen before overrides and multipliers when `EnableDataExport = true`, so exported catalogs remain the vanilla snapshot.
