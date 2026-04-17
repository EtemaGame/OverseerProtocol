# Checklist De Pruebas Runtime Actual

Esta checklist es la fuente operativa para probar OverseerProtocol durante la fase actual. La prioridad es confirmar startup estable, generacion de archivos editables, snapshot/reset/reload, tuning runtime conservador y logs claros.

No importa el spam: durante esta etapa los logs son parte del producto. Cada prueba debe dejar una senal visible de `OK`, `SKIP`, `WARNING`, `ERROR`, `reserved` o `no-op`.

## Donde Mirar

Log principal:

```text
BepInEx/LogOutput.log
```

Buscar primero:

```text
TEST SIGNAL
[Diagnostics]
[Runtime]
[UserConfig]
[Export]
[Snapshot]
[Validation]
[Overrides]
[RuntimeRules]
[Fingerprint]
[Admin]
```

Si algo falla, guardar el bloque completo desde `=== TEST SIGNAL: ... BEGIN ===` hasta `=== TEST SIGNAL: ... END ===`.

## Smoke Test Parcial 2026-04-17

Confirmado con logs reales:

- BepInEx carga `OverseerProtocol 0.1.0`.
- Boot diagnostics aparece correctamente.
- `StartOfRound` esta disponible cuando corre el startup pipeline.
- Runtime preconditions iniciales: `levels=13`, `items=89`, `terminalNodes=246`.
- Se crean presets, saves, definitions y rules.
- Se exportan items, moons, enemies, moon economy y spawn profiles.
- Snapshot captura y restaura items/moons/TerminalNodes.
- Multipliers con valor `1` quedan como no-op.
- Admin terminal hook esta apagado por config y por eso `op help` cae al parser vanilla.

Pendiente de re-probar tras la simplificacion de config:

- Generacion de `items.json`, `moons/*.json` y `utility-catalog.json`.
- Dry-run.
- Item/spawn/moon tuning con JSON editado.
- Multipliers distintos de `1`.
- Runtime rules activas con `travelDiscountMultiplier` y `routePriceMultiplier`.
- Admin terminal hook con `EnableAdminTerminalCommands=true`.
- Pruebas negativas.

## Config Base Para Primera Prueba

Usar esta base antes de tocar tuning agresivo:

```ini
[General]
EnableDataExport = true
ActivePreset = default

[Overrides]
EnableItemOverrides = true
EnableMoonOverrides = true
EnableSpawnOverrides = true

[Multipliers]
EnableRuntimeMultipliers = true
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1
RoutePriceMultiplier = 1

[Validation]
DryRunOverrides = false
StrictValidation = false
AbortOnInvalidOverrideBlock = false

[RuntimeRules]
EnableRuntimeRulesLoading = true

[Progression]
EnableProgressionStorage = true
EnablePerkCatalog = true

[Lobby]
EnableLobbyRulesLoading = true

[Admin]
EnableAdminTerminalCommands = false
AdminCommandPrefix = op

[ExperimentalMultiplayer]
EnableExperimentalMultiplayer = false
```

## 0. Build Y Deploy

Probar:

- Compilar la solucion.
- Copiar el plugin generado a BepInEx.
- Lanzar el juego con BepInEx.

Logs esperados:

```text
[Bootstrap] Directorios y core de OverseerProtocol preparados.
[Diagnostics] === TEST SIGNAL: BOOT DIAGNOSTICS BEGIN ===
[Diagnostics] Config ActivePreset=default
[Diagnostics] Runtime preconditions [plugin-awake]: StartOfRound=False
[Bootstrap] Harmony inicializado
```

Interpretacion:

- Si aparece `BOOT DIAGNOSTICS BEGIN`, el plugin cargo.
- Si `StartOfRound=False` en `plugin-awake`, es normal: el juego aun no entro al runtime de la run.
- Si faltan referencias, BepInEx/loader debe mostrar el error antes de estos logs.

## 1. Startup Pipeline

Probar:

- Entrar hasta que `StartOfRound.Start` ocurra.
- No editar JSON todavia.

Logs esperados:

```text
[Export] Export trigger: running initial data processing.
[Runtime] === TEST SIGNAL: STARTUP PIPELINE BEGIN ===
[Runtime] Startup step 1/6: load data-only contracts.
[Runtime] Startup step 2/6: capture vanilla runtime snapshot if needed.
[Runtime] Startup step 3/6: export vanilla catalogs.
[Runtime] Startup step 4/6: ensure human-editable config files.
[Runtime] Startup step 5/6: reset runtime state to snapshot before applying config.
[Runtime] Startup step 6/6: apply preset, user config, multipliers, and runtime rules.
[Runtime] === TEST SIGNAL: STARTUP PIPELINE END ===
```

Tambien debe aparecer:

```text
[Diagnostics] Runtime preconditions [startup-pipeline/begin]: StartOfRound=True
```

Interpretacion:

- `StartOfRound=True` confirma que el pipeline corre en el momento correcto.
- `levels=0`, `items=0` o `routeNodes=0` indica que el hook corre demasiado temprano o que cambio el runtime del juego.

## 2. Carpetas Y Archivos Editables

Probar:

- Revisar que se generen carpetas y JSON iniciales.

Rutas esperadas:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/exports/
BepInEx/plugins/OverseerProtocol/overseer-data/items.json
BepInEx/plugins/OverseerProtocol/overseer-data/moons/
BepInEx/plugins/OverseerProtocol/overseer-data/utility-catalog.json
BepInEx/plugins/OverseerProtocol/overseer-data/presets/
BepInEx/plugins/OverseerProtocol/overseer-data/saves/
BepInEx/plugins/OverseerProtocol/overseer-data/rules/
BepInEx/plugins/OverseerProtocol/overseer-data/definitions/
```

Archivos esperados:

```text
items.json
moons/ExperimentationLevel.json
moons/<cada MoonId exportada>.json
utility-catalog.json
saves/progression.json
definitions/perks.json
rules/lobby-rules.json
rules/runtime-rules.json
presets/<preset>/preset.json
```

Logs esperados:

```text
[UserConfig] Ensuring human-editable config files:
[UserConfig] Wrote utility catalog:
[UserConfig] Wrote item tuning file:
[UserConfig] Wrote moon tuning file:
[UserConfig] Human-editable config files are ready.
```

Interpretacion:

- `items.json` y `moons/*.json` son los archivos que el usuario edita.
- `utility-catalog.json` es solo referencia de IDs.
- Los exports crudos sirven para diagnostico y comparacion.

## 3. Export Baseline

Probar:

- Con `EnableDataExport = true`, dejar que arranque el pipeline.
- Revisar exports generados.

Archivos esperados:

```text
exports/items/items.json
exports/moons/moons.json
exports/enemies/enemies.json
exports/spawns/moon-spawn-profiles.json
exports/economy/moon-economy.json
```

Logs esperados:

```text
[Export] === TEST SIGNAL: DATA EXPORT BEGIN ===
[Export] Export step 2/6: reading and writing item catalog.
[Export] Item catalog read result: count=
[Export] Export step 3/6: reading and writing moon catalog.
[Export] Export step 4/6: building and writing moon economy profiles.
[Export] Export step 5/6: reading and writing enemy catalog.
[Export] Export step 6/6: reading and writing moon spawn profiles.
[Export] === TEST SIGNAL: DATA EXPORT END ===
```

Interpretacion:

- Todo `count` importante debe ser mayor a 0.
- `No TerminalNodes found in memory` significa que route prices no pueden validarse todavia.
- `Route price lookup missing` para una luna puede ser aceptable si esa luna no tiene route node.

## 4. Snapshot / Reset / Reload

Probar:

- Startup normal.
- Luego usar `op reset` y `op reload` cuando el hook admin este activo.
- Si el hook admin aun esta apagado, validar solo startup snapshot.

Logs esperados en startup:

```text
[Snapshot] Captured vanilla runtime state for
[Snapshot] Captured item state:
[Snapshot] Captured moon state:
[Snapshot] Captured route node state:
[Runtime] Phase 'reset-to-snapshot' started.
[Snapshot] Restored runtime state for
```

Logs esperados con reset/reload:

```text
[Runtime] === TEST SIGNAL: RUNTIME RELOAD BEGIN ===
[UserConfig] Ensuring human-editable config files:
[Snapshot] Restored item state:
[Snapshot] Restored moon state:
[Snapshot] Restored route node state:
[Runtime] === TEST SIGNAL: RUNTIME RELOAD END ===
```

Interpretacion:

- Si aparece `Runtime snapshot has not been captured`, reset/reload se ejecuto antes del primer startup runtime valido.
- Si los contadores de restore son 0, el snapshot no esta mapeando IDs actuales.

## 5. Dry-Run

Config:

```ini
[Validation]
DryRunOverrides = true
StrictValidation = false
AbortOnInvalidOverrideBlock = false
```

Probar:

- Dejar tuning habilitado.
- Usar JSON validos o vacios.
- Arrancar o ejecutar `op reload`.

Logs esperados:

```text
[Validation] Validation completed with
[Overrides] Dry-run enabled.
[Overrides] Dry-run enabled. Runtime multipliers were resolved but not applied.
[RuntimeRules] Dry-run enabled. Runtime rules were loaded but not applied.
```

Interpretacion:

- En dry-run deben aparecer validaciones.
- No deben aparecer logs tipo `Overriding ...`, `Item weight multiplier applied`, `Route price multiplier applied` ni `Route node mutated by runtime rules`.

## 6. Item Tuning

Preparacion:

- Elegir un `id` real desde `utility-catalog.json` o `items.json`.
- Editar `overseer-data/items.json`.
- Tocar solo campos bajo `override` para probar runtime activo.

Ejemplo dentro de un item existente:

```json
{
  "id": "Shovel",
  "enabled": true,
  "override": {
    "creditsWorth": 123,
    "weight": 1.25
  }
}
```

Logs esperados:

```text
[Overrides] Loading item tuning from:
[Overrides] Item tuning file loaded:
[Validation] Item validation references:
[Validation] Item validation policy:
[Overrides] Overriding Shovel.creditsWorth:
[Overrides] Overriding Shovel.weight:
[Overrides] Successfully applied
```

Interpretacion:

- El item real debe mutar.
- Un ID falso debe generar warning y no romper startup.
- Con `StrictValidation=true`, warnings pueden impedir aplicacion segun la politica logueada.
- `store`, `battery` y `spawn` deben loguear `reserved` si se editan.

## 7. Spawn Tuning

Preparacion:

- Elegir un archivo real en `overseer-data/moons/`.
- Elegir `enemyId` real desde `utility-catalog.json`.
- Editar `spawns.insideEnemies`, `spawns.outsideEnemies` o `spawns.daytimeEnemies`.

Ejemplo:

```json
{
  "moonId": "ExperimentationLevel",
  "spawns": {
    "insideEnemies": {
      "mode": "replace",
      "entries": [
        { "enemyId": "Centipede", "rarity": 50 }
      ]
    },
    "outsideEnemies": {
      "mode": "clear",
      "entries": []
    },
    "daytimeEnemies": {
      "mode": "keep",
      "entries": []
    }
  }
}
```

Logs esperados:

```text
[Registry] Registry built:
[Overrides] Loading moon spawn tuning from:
[Validation] Spawn validation references:
[Overrides] Replacing Inside pool for ExperimentationLevel:
[Overrides] Added spawn entry:
[Overrides] Outside pool for ExperimentationLevel now has 0 entries.
[Overrides] Successfully applied spawn tuning
```

Interpretacion:

- `mode=keep` mantiene el pool observado y refresca sus entradas al arrancar.
- `mode=replace` reemplaza por `entries`.
- `mode=clear` deja el pool vacio.
- Enemy desconocido debe loguear warning y saltar solo esa entrada.

## 8. Moon Tuning

Preparacion:

- Elegir un archivo real en `overseer-data/moons/`.
- Editar campos bajo `override`.

Ejemplo:

```json
{
  "moonId": "ExperimentationLevel",
  "override": {
    "riskLabel": "TEST-RISK",
    "routePrice": 321,
    "routePriceMultiplier": 2
  }
}
```

Logs esperados:

```text
[Overrides] Loading moon tuning from:
[Validation] Moon validation references:
[Overrides] Overriding ExperimentationLevel.riskLevel via riskLabel:
[Overrides] Overriding route node
[RuntimeRules] Merged moon tuning routePriceMultiplier:
```

Interpretacion:

- `riskLabel` gana sobre `riskLevel`.
- Si `routePrice` no encuentra node, debe aparecer warning y no crashear.
- `routePriceMultiplier` se fusiona como regla runtime por luna.
- `displayName`, `description`, `scrap` e `items` deben loguear `reserved` si se editan.

## 9. Runtime Multipliers

Config:

```ini
[Multipliers]
EnableRuntimeMultipliers = true
ItemWeightMultiplier = 1.1
SpawnRarityMultiplier = 1.2
RoutePriceMultiplier = 0.5

[SemanticDifficulty]
AggressionProfile = Aggressive
```

Logs esperados:

```text
[SemanticDifficulty] AggressionProfile=Aggressive
[Overrides] Applying runtime multipliers:
[Overrides] Runtime multiplier normalized values:
[Overrides] Item weight multiplier applied:
[Overrides] Spawn rarity multiplier applied:
[Overrides] Route price multiplier applied:
```

Interpretacion:

- Los multipliers corren despues de user JSON tuning.
- Si todos son `1`, debe aparecer `Runtime multipliers are all 1. No multiplier changes applied.`

## 10. Runtime Rules Activas Y Reserved

Activo hoy:

- `economy.travelDiscountMultiplier`
- `moonRules.<moonId>.routePriceMultiplier`
- `moons/<MoonId>.json -> override.routePriceMultiplier`

Reservado hoy:

- quota.
- deadline.
- scrap value.
- ship scanner/battery/timing.
- weather reward tuning.
- per-moon scrap/weather.

Ejemplo:

```json
{
  "schemaVersion": 1,
  "economy": {
    "quotaMultiplier": 2,
    "deadlineMultiplier": 1,
    "travelDiscountMultiplier": 0.5,
    "scrapValueMultiplier": 1,
    "preserveShipLootOnTeamWipe": false
  },
  "moonRules": {
    "ExperimentationLevel": {
      "routePriceMultiplier": 2,
      "scrapValueMultiplier": 1,
      "spawnRarityMultiplier": 1,
      "weatherRewardMultiplier": 1
    }
  }
}
```

Logs esperados:

```text
[RuntimeRules] Loaded runtime rules from
[RuntimeRules] Applying runtime rules. Active today:
[RuntimeRules] Active economy rule travelDiscountMultiplier=0.5
[RuntimeRules] Per-moon route rule found:
[RuntimeRules] Route node mutated by runtime rules:
[RuntimeRules] quotaMultiplier is reserved until quota runtime hooks are verified.
```

Interpretacion:

- Si el costo final no coincide, revisar orden: snapshot -> moon routePrice -> cfg route multiplier -> runtime rules.
- Un campo reservado debe loguear warning; no debe prometer gameplay.

## 11. Presets Y Precedencia

Prueba A:

```ini
[General]
ActivePreset = hardcore

[Multipliers]
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1
RoutePriceMultiplier = 1
```

Logs esperados:

```text
[Presets] Loaded preset 'hardcore'
[Presets] Preset item weight multiplier selected because cfg value is default:
[Presets] Preset spawn rarity multiplier selected because cfg value is default:
[Presets] Preset route price multiplier selected because cfg value is default:
```

Prueba B:

```ini
[Multipliers]
ItemWeightMultiplier = 1.3
```

Logs esperados:

```text
[Presets] Cfg item weight multiplier wins over preset:
```

Interpretacion:

- Presets hoy aportan multipliers base y metadata.
- `items.json` y `moons/*.json` son globales para el perfil.
- `.cfg` distinto de 1 gana sobre multiplier del preset.

## 12. Fingerprints

Probar:

- Arrancar con config base.
- Guardar `Preset fingerprint` y `Config fingerprint`.
- Cambiar `items.json`, un archivo en `moons/`, `runtime-rules.json`, `lobby-rules.json` o un multiplier `.cfg`.
- Ejecutar reload o reiniciar.

Logs esperados:

```text
[Fingerprint] Fingerprint input file:
[Fingerprint] Fingerprint config input:
[Fingerprint] Fingerprint result:
[Fingerprint] Preset fingerprint:
[Fingerprint] Config fingerprint:
```

Interpretacion:

- Cambiar user JSON tuning/rules debe cambiar `Preset fingerprint`.
- Cambiar toggles/multipliers `.cfg` debe cambiar `Config fingerprint`.

## 13. Admin Terminal Hook

Primero confirmar apagado:

```ini
[Admin]
EnableAdminTerminalCommands = false
```

Log esperado:

```text
[Admin] Admin terminal commands disabled by config.
```

Luego activar:

```ini
[Admin]
EnableAdminTerminalCommands = true
AdminCommandPrefix = op
```

Comandos a probar:

```text
op help
op preset
op paths
op export
op reload
op reset
op fingerprint
op rules
op perks
op progression
op progression grant ship 100
op progression reset ship
op handshake
op multiplayer
op sync snapshot
op validate
```

Logs esperados:

```text
[Admin] Admin terminal command hook patched on Terminal.ParsePlayerSentence.
[Admin] Terminal hook captured input:
[Admin] OverseerProtocol admin command parsed:
[Admin] Created Terminal response node
[Admin] Handled admin terminal command:
```

Probar tambien un comando vanilla, por ejemplo `moons`:

```text
[Admin] Terminal input is not an OverseerProtocol command.
[Admin] Terminal hook did not handle input. Vanilla terminal flow continues.
```

Interpretacion:

- Los comandos `op` deben mostrar respuesta en Terminal.
- Comandos vanilla deben seguir funcionando.
- `op paths` debe listar `items.json`, `moons/` y `utility-catalog.json`.

## 14. Pruebas Negativas Obligatorias

Probar uno por uno:

- JSON invalido en `items.json`.
- ID de item inexistente.
- Moon inexistente.
- Enemy inexistente.
- `StrictValidation=true` con warnings.
- `DryRunOverrides=true` con tuning valido.
- `runtime-rules.json` con un campo reservado distinto de 1.

Logs esperados:

```text
[Json] Failed to read JSON file
[Validation] Validation completed with
[Validation] ... was not applied because
[Overrides] ... not found ... Skipping.
[RuntimeRules] ... is reserved until
```

Interpretacion:

- Ninguna prueba negativa debe crashear el juego.
- Los errores deben decir que bloque, ID o campo fallo.
- Si una prueba negativa muta runtime en dry-run, es bug.

## 15. Que No Se Debe Validar Como Gameplay Todavia

No marcar como gameplay funcional aunque exista JSON/log:

- Store injection de items.
- Battery tuning real.
- Scrap min/max por luna.
- Item pool por luna.
- Perk appliers player/ship.
- Quota multiplier.
- Deadline multiplier.
- Scrap value multiplier.
- Weather reward tuning.
- Expanded lobby estable.
- Late join real.
- Spectator mode.
- Sync de puertas, luces, objetos, inventarios o state recovery.

Senal esperada:

```text
reserved until ... hooks are verified
diagnostics only
data-only
scaffold
```

## Criterio De Cierre Actual

La etapa actual queda lista para avanzar cuando:

- El plugin carga y muestra `BOOT DIAGNOSTICS`.
- `STARTUP PIPELINE` llega a `END`.
- Exports se escriben con conteos mayores a 0.
- `items.json`, `moons/*.json` y `utility-catalog.json` se generan.
- Snapshot captura y restaura items, moons y route nodes.
- Dry-run valida sin mutar.
- Item/spawn/moon tuning valido muta runtime y los invalidos se saltan con warning.
- Runtime multipliers aplican despues de user JSON tuning.
- Runtime rules activas mutan route prices y las reservadas loguean warning.
- Admin hook puede apagarse, prenderse, ejecutar `op` y dejar pasar comandos vanilla.
- Fingerprints cambian cuando cambia config o JSON.
