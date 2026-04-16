# Presets V1

Preset support packages safe profile defaults without guessing modpack-specific IDs.

On startup, OverseerProtocol seeds these built-in presets:

- `vanilla-plus`: Light spawn pressure increase.
- `hardcore`: Higher item burden and enemy spawn pressure.

They are written to:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-id>/preset.json
```

Each preset also gets an `overrides` folder for detailed JSON overrides:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-id>/overrides/
```

## Selecting A Preset

Set `ActivePreset` in `BepInEx/config/com.overseerprotocol.core.cfg`:

```ini
[General]
ActivePreset = hardcore
```

When selected, a preset can provide default runtime multipliers:

```json
{
  "id": "hardcore",
  "displayName": "Hardcore",
  "itemWeightMultiplier": 1.1,
  "spawnRarityMultiplier": 1.35
}
```

If the `.cfg` multiplier is still `1`, the preset multiplier is used. If the `.cfg` multiplier is changed to another value, the user value wins.

## Advanced Overrides

Detailed JSON still uses the active preset folder:

```text
presets/hardcore/overrides/items.override.json
presets/hardcore/overrides/spawns.override.json
```

V1 presets intentionally avoid bundled item/enemy IDs so they remain safe across vanilla updates and modded catalogs.
