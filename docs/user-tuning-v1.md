# User Tuning V1

OverseerProtocol usa una sola fuente de verdad editable:

```text
BepInEx/config/com.overseerprotocol.core.cfg
```

Los JSON bajo `overseer-data/exports/` son diagnostico no autoritativo.

## Items

Cada item detectado genera su propia seccion `Items.<ItemId>` para que Gale muestre casillas separadas:

```ini
[Items.Shovel]
Enabled = false
DisplayName = Shovel
Value = 30
Weight = 1.13
IsScrap = false
InStore = true
StorePrice = 30
RequiresBattery = false
MinScrapValue = 0
MaxScrapValue = 0
```

Activo hoy cuando `Enabled=true`:

- `Value`: modifica `creditsWorth`.
- `Weight`: modifica `weight`.
- `IsScrap`: modifica si el item cuenta como scrap.
- `InStore`: agrega o quita el item del catalogo de compra de la terminal.
- `StorePrice`: cambia el precio usado por tienda/creditsWorth.
- `RequiresBattery`: modifica si el item usa bateria.
- `MinScrapValue`: modifica el minimo de valor cuando el item aparece como scrap.
- `MaxScrapValue`: modifica el maximo de valor cuando el item aparece como scrap.

## Moons

Cada luna detectada genera su propia seccion `Moons.<MoonId>`:

```ini
[Moons.AdamanceLevel]
Enabled = false
DisplayName = 20 Adamance
RoutePrice = 0
Tier = B
RiskLevel = 3
Description =
MinScrap = 8
MaxScrap = 12
MinTotalScrapValue = 80
MaxTotalScrapValue = 120
InsideEnemiesEnabled = false
InsideEnemies = Centipede:41, HoarderBug:41
OutsideEnemiesEnabled = false
OutsideEnemies = MouthDog:20
DaytimeEnemiesEnabled = false
DaytimeEnemies = RedLocustBees:10
RouteMultiplierEnabled = false
RouteMultiplier = 1
InteriorWeightsEnabled = false
InteriorWeights = Factory:300, Mansion:100, Mineshaft:50
```

Activo hoy cuando `Enabled=true`:

- `RoutePrice`: modifica precio de ruta.
- `Tier`: modifica label de riesgo.
- `RiskLevel`: acepta `0..5` y se mapea a `None`, `D`, `C`, `B`, `A`, `S`.
- `Description`: modifica la descripcion interna de la luna.
- `MinScrap`: modifica el minimo de scrap del nivel.
- `MaxScrap`: modifica el maximo de scrap del nivel.
- `MinTotalScrapValue`: modifica el presupuesto minimo de valor total de scrap de la luna.
- `MaxTotalScrapValue`: modifica el presupuesto maximo de valor total de scrap de la luna.

Los items muestran `MinScrapValue`/`MaxScrapValue` como rango base del asset. El valor final que ves in-game puede ser ajustado por el presupuesto total de scrap de la luna y la generacion de ronda.

## Enemigos Por Luna

Cada pool esta dentro de la misma seccion de la luna:

```ini
[Moons.ExperimentationLevel]
InsideEnemiesEnabled = true
InsideEnemies = Centipede:50, HoarderBug:20
```

Cuando `<Pool>Enabled=true`, el pool se reemplaza por la lista correspondiente. Si la lista esta vacia, el pool queda vacio.

## Interiors

Los interiores detectados en runtime generan secciones `Interiors.<InteriorId>`:

```ini
[Interiors.Factory]
Enabled = false
DisplayName = Factory
RuntimeIndex = 0
UsedByMoons = ExperimentationLevel, AssuranceLevel
ObservedWeights = ExperimentationLevel:300, AssuranceLevel:300

[Moons.AdamanceLevel]
InteriorWeightsEnabled = false
InteriorWeights = Factory:300, Mansion:100, Mineshaft:50
```

Los campos `RuntimeIndex`, `UsedByMoons` y `ObservedWeights` son informativos. Cuando `InteriorWeightsEnabled=true`, la luna usa solo los interiores listados y sus rarezas. Si la lista esta vacia o usa IDs desconocidos, OverseerProtocol no aplica el cambio y registra un warning.

## Gameplay

```ini
[Gameplay]
EnableMultipliers = true
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1
RoutePriceMultiplier = 1
TravelDiscountMultiplier = 1

[Moons.AdamanceLevel]
RouteMultiplierEnabled = false
RouteMultiplier = 1
```

## Utility

```ini
[Utility]
ReduceVerboseLogs = true
VerboseDiagnostics = false
EnableDataExport = true
DisableDiagnosticExportsAfterFirstRun = false
NetworkLogLevel = Normal
```

`ReduceVerboseLogs` baja el ruido repetitivo de arranque. `VerboseDiagnostics` vuelve a habilitar precondiciones y senales de prueba detalladas.

## Precedencia Runtime

```text
snapshot vanilla -> advanced preset -> enabled .cfg entity overrides -> .cfg gameplay/multiplayer/interior tuning
```

`reload` relee el `.cfg`, reconstruye overrides, restaura snapshot y reaplica. `reset` solo restaura snapshot.
