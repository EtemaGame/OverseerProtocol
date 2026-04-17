# User Tuning V1

OverseerProtocol usa una sola fuente de verdad editable:

```text
BepInEx/config/com.overseerprotocol.core.cfg
```

Los JSON bajo `overseer-data/exports/` son diagnostico no autoritativo.

## Items

`[Items]` lista los items detectados como entidades completas:

```ini
[Items]
Shovel = enabled=false; displayName=Shovel; value=30; weight=1.13; scrap=false; store=false; storePrice=-1; battery=false; minValue=0; maxValue=0
GoldBar = enabled=false; displayName=Gold bar; value=150; weight=1.77; scrap=true; store=false; storePrice=-1; battery=false; minValue=100; maxValue=180
```

Activo hoy cuando `enabled=true`:

- `value`: modifica `creditsWorth`.
- `weight`: modifica `weight`.
- `store`: agrega el item al catalogo de compra de la terminal.
- `storePrice`: cambia el precio usado por tienda/creditsWorth.
- `minValue`: modifica el minimo de valor cuando el item aparece como scrap.
- `maxValue`: modifica el maximo de valor cuando el item aparece como scrap.

Visible pero reservado hasta tener hooks seguros:

- `battery`

## Moons

`[Moons]` lista las lunas detectadas:

```ini
[Moons]
AdamanceLevel = enabled=false; displayName=20 Adamance; price=0; tier=B; riskLevel=3; description=; minScrap=8; maxScrap=12; interior=reserved
```

Activo hoy cuando `enabled=true`:

- `price`: modifica precio de ruta.
- `tier` o `riskLabel`: modifica label de riesgo.
- `riskLevel`: acepta `0..5` y se mapea a `None`, `D`, `C`, `B`, `A`, `S`.
- `description`: modifica la descripcion interna de la luna.
- `minScrap`: modifica el minimo de scrap del nivel.
- `maxScrap`: modifica el maximo de scrap del nivel.

Visible pero reservado:

- `interior`

## Enemigos Por Luna

Cada pool lista la composicion observada por luna:

```ini
[Moons.InsideEnemies]
ExperimentationLevel = enabled=false; entries=Centipede:50, HoarderBug:20

[Moons.OutsideEnemies]
ExperimentationLevel = enabled=false; entries=MouthDog:10

[Moons.DaytimeEnemies]
ExperimentationLevel = enabled=false; entries=RedLocustBees:20
```

Cuando `enabled=true`, el pool se reemplaza por `entries`. Si `entries` esta vacio, el pool queda vacio.

## Multipliers Y Runtime Rules

```ini
[Multipliers]
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1
RoutePriceMultiplier = 1
TravelDiscountMultiplier = 1
AggressionProfile = Balanced

[Moons.RouteMultiplier]
AdamanceLevel = enabled=false; multiplier=1
```

## Secciones Planeadas

`Interiors` y `Utility` no se generan todavia porque no tienen appliers runtime verificados. `Perks` solo contiene flags reales de persistencia/catalogo hasta que existan perks aplicables.

## Precedencia Runtime

```text
snapshot vanilla -> built-in preset -> enabled .cfg entity overrides -> .cfg multipliers/toggles -> .cfg runtime rules
```

`reload` relee el `.cfg`, reconstruye overrides, restaura snapshot y reaplica. `reset` solo restaura snapshot.
