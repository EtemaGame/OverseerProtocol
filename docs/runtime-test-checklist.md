# Runtime Test Checklist

## Startup

- BepInEx loads `com.overseerprotocol.core.cfg`.
- `StartOfRound.Start` triggers startup once.
- Snapshot captures vanilla state.
- Optional exports go to `overseer-data/exports/`.
- `.cfg` gains `[Items]`, `[Moons]`, `[Moons.*Enemies]`, and `[Moons.RouteMultiplier]`.
- No user-editable JSON tuning files are created.

## Item Tuning

```ini
[Items]
Shovel = enabled=true; displayName=Shovel; value=45; weight=1.05; scrap=false; store=false; storePrice=-1; battery=false; minValue=0; maxValue=0
```

Expected:

- `value`, `weight`, `minValue`, and `maxValue` apply.
- `store=true` adds the item to the terminal store; `storePrice` changes its price.

## Moon Tuning

```ini
[Moons]
ExperimentationLevel = enabled=true; displayName=41 Experimentation; price=125; tier=B; riskLevel=3; description=Testing route; minScrap=8; maxScrap=14; interior=reserved
```

Expected:

- route price applies when route node exists;
- risk label applies from `tier`/`riskLabel`;
- description, minScrap, and maxScrap apply.

## Spawn Tuning

```ini
[Moons.InsideEnemies]
ExperimentationLevel = enabled=true; entries=Centipede:50, Flowerman:20
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
