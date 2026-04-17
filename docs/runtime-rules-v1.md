# Runtime Rules V1

Runtime rules se configuran en BepInEx `.cfg`, no en JSON.

```ini
[RuntimeRules]
EnableRuntimeRulesLoading = true

[RuntimeRules.Economy]
TravelDiscountMultiplier = 1

[Moons.RouteMultiplier]
ExperimentationLevel = 1
```

## Estado Actual

Activo hoy:

- `RuntimeRules.Economy.TravelDiscountMultiplier` multiplica precios de rutas de Terminal.
- `Moons.RouteMultiplier.<MoonId>` aplica un multiplier per-moon a rutas de Terminal.
- `Multipliers.RoutePriceMultiplier` sigue siendo el multiplier global simple y se aplica antes de runtime rules.

Reservado/experimental:

- quota/deadline;
- scrap value;
- preserve ship loot;
- ship timing;
- scanner/battery;
- weather rewards;
- per-moon scrap/spawn/weather rule composition.

Los modelos internos siguen agrupando estas ideas para mantener la arquitectura conceptual, pero el usuario no edita `runtime-rules.json`.
