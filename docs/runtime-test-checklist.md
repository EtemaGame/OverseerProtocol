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

- `value`, `weight`, `minValue`, and `maxValue` apply.
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

## Planned Sections

`Interiors` and `Utility` should not be generated until runtime appliers exist. `Perks` should only expose real persistence/perk flags, not placeholder rows.

## Commands

With admin commands enabled:

- `op paths`
- `op reload`
- `op reset`
- `op fingerprint`
