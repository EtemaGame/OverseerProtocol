# User Tuning V1

OverseerProtocol genera archivos editables desde los datos reales del juego. La regla practica es:

- `observed`: foto regenerada desde el runtime. Sirve para mirar, no para editar.
- `override`, `spawns`, `scrap`, `items`, `store`, `battery`: campos que el usuario puede tocar.
- `utility-catalog.json`: lista de IDs para copiar y pegar.

## Archivos Principales

```text
BepInEx/plugins/OverseerProtocol/overseer-data/items.json
BepInEx/plugins/OverseerProtocol/overseer-data/moons/<MoonId>.json
BepInEx/plugins/OverseerProtocol/overseer-data/utility-catalog.json
```

Tambien se siguen generando exports crudos en `overseer-data/exports/` para diagnostico, pero el flujo normal de edicion usa los archivos de arriba.

## Items

`items.json` contiene todos los items detectados. Cada bloque tiene el ID estable, datos observados y campos editables.

Ejemplo:

```json
{
  "id": "Shovel",
  "enabled": true,
  "observed": {
    "displayName": "Shovel",
    "creditsWorth": 30,
    "weight": 1.13,
    "requiresBattery": false
  },
  "override": {
    "creditsWorth": 45,
    "weight": 1.05
  },
  "store": {
    "storePrice": 45
  },
  "battery": {},
  "spawn": {}
}
```

Activo hoy:

- `override.creditsWorth`
- `override.weight`

Reservado hasta verificar hooks seguros:

- `store.addToStore`, `store.storePrice`, `store.maxStoreStock`
- `battery.requiresBattery`, `battery.batteryUsageMultiplier`, `battery.batteryCapacityMultiplier`
- `spawn.allowAsScrap`, `spawn.minValue`, `spawn.maxValue`

## Lunas

Cada luna tiene su propio archivo en `moons/<MoonId>.json`. Si quieres tocar Experimentation, editas:

```text
overseer-data/moons/ExperimentationLevel.json
```

Ejemplo de costo y riesgo:

```json
{
  "moonId": "ExperimentationLevel",
  "enabled": true,
  "override": {
    "routePrice": 125,
    "riskLabel": "B",
    "routePriceMultiplier": 1.5
  }
}
```

Activo hoy:

- `override.routePrice`
- `override.riskLevel`
- `override.riskLabel`
- `override.routePriceMultiplier`

Reservado hasta verificar hooks seguros:

- `override.displayName`
- `override.description`
- `scrap.*`
- `items.*`

## Spawns Por Luna

Dentro de cada luna hay tres pools:

- `spawns.insideEnemies`
- `spawns.outsideEnemies`
- `spawns.daytimeEnemies`

Cada pool usa `mode`:

- `keep`: mantiene el pool observado. Las entradas se refrescan desde el juego.
- `replace`: reemplaza el pool por `entries`.
- `clear`: deja el pool vacio.

Ejemplo para reemplazar enemigos interiores:

```json
{
  "spawns": {
    "insideEnemies": {
      "mode": "replace",
      "entries": [
        { "enemyId": "Centipede", "rarity": 50 },
        { "enemyId": "Flowerman", "rarity": 20 }
      ]
    },
    "outsideEnemies": {
      "mode": "keep",
      "entries": []
    },
    "daytimeEnemies": {
      "mode": "clear",
      "entries": []
    }
  }
}
```

## Utility Catalog

`utility-catalog.json` no aplica cambios. Es una hoja de referencia con:

- items disponibles.
- lunas disponibles.
- enemigos disponibles.
- spawn profiles actuales.

Usalo cuando quieras agregar un mob que no aparece en una luna, sacar un mob, o copiar el ID exacto de un item.

## Precedencia Runtime

El orden efectivo es:

```text
snapshot vanilla -> preset -> user JSON tuning -> cfg multipliers/toggles -> runtime rules
```

Esto significa que `items.json` y `moons/*.json` aplican despues de restaurar vanilla y antes de los multipliers globales.

## Limpieza Del Formato Antiguo

El runtime ya no lee el formato legacy de la carpeta `overseer-data/overrides`. Si esa carpeta existe en un perfil viejo, queda ignorada. No se borra automaticamente para evitar destruir archivos editados por el usuario.
