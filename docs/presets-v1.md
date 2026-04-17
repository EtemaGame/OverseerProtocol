# Presets V1

Preset support packages safe profile defaults without guessing modpack-specific IDs.

On startup, OverseerProtocol seeds these built-in presets:

- `vanilla-plus`: Light spawn pressure increase.
- `hardcore`: Higher item burden and enemy spawn pressure.
- `economy-chaos`: Empty economy tuning shell for moon route price experiments.
- `outside-nightmare`: Empty spawn tuning shell with a modest global spawn multiplier.
- `scrap-heaven`: Empty item tuning shell with a light item weight reduction.

They are written to:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-id>/preset.json
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
  "schemaVersion": 1,
  "id": "hardcore",
  "displayName": "Hardcore",
  "author": "OverseerProtocol",
  "minimumOverseerVersion": "0.1.0",
  "itemWeightMultiplier": 1.1,
  "spawnRarityMultiplier": 1.35,
  "routePriceMultiplier": 1.1
}
```

If the `.cfg` multiplier is still `1`, the preset multiplier is used. If the `.cfg` multiplier is changed to another value, the user value wins. This applies to item weight, spawn rarity, and route price multipliers.

## User Tuning

Detailed entity tuning is intentionally kept in the main profile files:

```text
overseer-data/items.json
overseer-data/moons/<MoonId>.json
overseer-data/utility-catalog.json
```

V1 presets intentionally avoid bundled item/enemy IDs so they remain safe across vanilla updates and modded catalogs.

Built-in presets seed manifests if missing. Existing user-edited manifests and user tuning files are never overwritten blindly; observed fields are regenerated while editable fields are preserved.
