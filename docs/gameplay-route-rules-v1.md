# Gameplay Route Rules V1

Route economy tuning is configured in BepInEx `.cfg`; there is no separate user-facing JSON rules file.

```ini
[Gameplay]
EnableMultipliers = true
RoutePriceMultiplier = 1
TravelDiscountMultiplier = 1

[Moons.ExperimentationLevel]
RouteMultiplierEnabled = true
RouteMultiplier = 1
```

## Active Fields

- `[Gameplay].RoutePriceMultiplier` applies a global route price multiplier.
- `[Gameplay].TravelDiscountMultiplier` applies a global travel discount multiplier.
- `Moons.<MoonId>.RouteMultiplier` applies a per-moon route multiplier when `RouteMultiplierEnabled=true`.

The implementation still uses normalized internal rule objects so reset/reload and fingerprints can share the same pipeline, but the player edits only `.cfg`.
