# LethalConfig Bridge V1

OverseerProtocol can optionally register its BepInEx config entries in LethalConfig's in-game menu.

LethalConfig is not required. If it is not installed, OverseerProtocol logs that the bridge was skipped and continues normally.

## Config

```ini
[Utility]
EnableLethalConfigBridge = true
```

## Behavior

- The bridge uses a soft BepInEx dependency on `ainavt.lc.lethalconfig`.
- Runtime-discovered sections such as `Items.<ItemId>`, `Moons.<MoonId>`, and `Interiors.<InteriorId>` are registered after OverseerProtocol has discovered the game's runtime catalogs.
- Supported field types are registered as LethalConfig controls: `bool`, `int`, `float`, `string`, and enums.
- Overseer panel keybinds are not LethalConfig entries. They are registered through LethalCompanyInputUtils and edited in the game's controls menu.
- The bridge adds an `Overseer / Apply current config` button that runs OverseerProtocol's runtime reload flow after editing values.

## Notes

The bridge talks to LethalConfig by reflection. This keeps OverseerProtocol from requiring LethalConfig at build time and avoids copying LethalConfig code into this project.
