# Presets V1

Presets are built into OverseerProtocol. They do not live in `preset.json` and they are not seeded to disk.

Available preset names:

- `default`
- `vanilla-plus`
- `hardcore`
- `economy-chaos`
- `outside-nightmare`
- `scrap-heaven`

Select one in the BepInEx config:

```ini
[General]
ActivePreset = hardcore
```

Presets provide base multipliers only:

- item weight;
- spawn rarity;
- route price.

If a multiplier in `.cfg` is still `1`, the preset value can supply the base. If the user sets a multiplier to another value, the `.cfg` value wins.

Detailed item, moon, and spawn tuning is always in `BepInEx/config/com.overseerprotocol.core.cfg`.
