# BepInEx Configuration V1

OverseerProtocol ya no usa configuracion hibrida. La fuente editable es:

```text
BepInEx/config/com.overseerprotocol.core.cfg
```

## Esquema

```ini
[General]
EnableDataExport = true
ActivePreset = default

[General]
EnableItemOverrides = true
EnableMoonOverrides = true
EnableSpawnOverrides = true

[Items]
Shovel = enabled=false; displayName=Shovel; value=30; weight=1.13; scrap=false; store=false; storePrice=-1; battery=false; minValue=0; maxValue=0

[Moons]
ExperimentationLevel = enabled=false; displayName=41 Experimentation; price=0; tier=B; riskLevel=3; description=; minScrap=8; maxScrap=12; interior=reserved

[Moons.InsideEnemies]
ExperimentationLevel = enabled=false; entries=Centipede:50, HoarderBug:20

[Moons.OutsideEnemies]
ExperimentationLevel = enabled=false; entries=MouthDog:10

[Moons.DaytimeEnemies]
ExperimentationLevel = enabled=false; entries=RedLocustBees:20

[Moons.RouteMultiplier]
ExperimentationLevel = enabled=false; multiplier=1
```

Entradas generadas con `enabled=false` sirven como catalogo observado. Solo mutan runtime cuando cambias `enabled=true`.

## Lifecycle

Startup:

1. bind de config estatica;
2. snapshot vanilla;
3. exports diagnosticos opcionales;
4. bind de catalogos `.cfg` por IDs reales;
5. restore de snapshot;
6. aplicacion de overrides habilitados.

Reload:

1. relee `.cfg`;
2. reconstruye overrides;
3. restaura snapshot;
4. reaplica.

Reset:

1. restaura snapshot;
2. no toca archivos del usuario.

## No Autoritativo

`overseer-data/exports/*.json`, `progression.json` y `perks.json` no son fuente de verdad para item/moon/spawn tuning.
