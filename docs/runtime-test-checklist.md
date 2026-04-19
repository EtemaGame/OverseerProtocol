# Runtime Test Checklist

## Startup

- BepInEx loads `com.overseerprotocol.core.cfg`.
- `StartOfRound.Start` triggers startup once.
- Snapshot captures vanilla state.
- Optional exports go to `overseer-data/exports/`.
- `.cfg` gains per-entity sections like `[Items.Shovel]` and `[Moons.ExperimentationLevel]`.
- No user-editable JSON tuning files are created.

## Item Tuning

```ini
[Items.Shovel]
Enabled = true
Value = 45
Weight = 1.05
InStore = true
StorePrice = 45
MinScrapValue = 0
MaxScrapValue = 0
```

Expected:

- `value`, `weight`, `minValue`, and `maxValue` apply. Remember the final scanned scrap value can also be affected by the moon total scrap value budget.
- `InStore=true` adds the item to the terminal store; `StorePrice` changes its price.

## Moon Tuning

```ini
[Moons.ExperimentationLevel]
Enabled = true
RoutePrice = 125
Tier = B
RiskLevel = 3
Description = Testing route
MinScrap = 8
MaxScrap = 14
```

Expected:

- route price applies when route node exists;
- risk label applies from `tier`/`riskLabel`;
- description, minScrap, and maxScrap apply.

## Spawn Tuning

```ini
[Moons.ExperimentationLevel]
InsideEnemiesEnabled = true
InsideEnemies = Centipede:50, Flowerman:20
```

Expected:

- pool is replaced;
- invalid enemy IDs warn and are skipped.

## Overseer Panel

Press the in-game `Open Overseer Panel` keybind from the controls menu. Default keyboard binding: `P`.

Expected:

- multiplayer status is visible without typing terminal commands;
- local player progression is visible to the local player;
- ship perk/progression information is visible only to the host;
- perk appliers are clearly marked as inactive until gameplay effects exist.

## LethalConfig Bridge

With LethalConfig installed and `[Utility].EnableLethalConfigBridge=true`:

- OverseerProtocol appears in LethalConfig;
- dynamic sections like `Items.<ItemId>`, `Moons.<MoonId>`, and `Interiors.<InteriorId>` are visible after hosting/loading into a runtime where catalogs exist;
- editing values and pressing `Overseer / Apply current config` reapplies runtime tuning;
- unsupported entries such as keybinds remain editable through Gale/BepInEx config.

## Commands

Terminal commands are intentionally minimal:

- `op help`
- `op reload`
