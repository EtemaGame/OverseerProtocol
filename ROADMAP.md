# Roadmap Oficial De OverseerProtocol

## Resumen

Este roadmap es la direccion principal de OverseerProtocol.

El proyecto prioriza primero un nucleo data-driven, reversible, diagnosticable y configurable; despues progresion, runtime tooling y sync basico; y deja expanded lobby, late join y multiplayer avanzado como fases posteriores sujetas a evidencia real de estabilidad.

Regla guia:

- No convertir contratos data-only en promesas de gameplay hasta que esten aplicados, verificados y documentados como runtime.
- Todo hook delicado debe tener un feature flag de apagado.
- Todo campo no verificado debe quedar marcado como data-only, reserved o experimental.

## Estado Actual Confirmado

### Gameplay / Runtime Muy Adelantado

- Plugin BepInEx/Harmony y lifecycle inicial.
- Exports: items, moons, enemies, spawn profiles y moon economy.
- Item overrides, spawn overrides y moon overrides.
- Validacion con warnings/errors, strict mode y dry-run.
- Runtime multipliers para item weight, spawn rarity y route prices.
- Runtime snapshot para reset/reload.
- Runtime orchestrator para startup, reload y reset.

### Producto / Data Layer Muy Adelantado

- `.cfg + JSON` como configuracion hibrida.
- Presets con manifiesto, override templates y rules templates.
- Paths organizados para exports, overrides, presets, saves, rules y definitions.
- Docs tecnicas por subsistema.
- Node local para validacion JSON de samples.

### Contratos Data-Only Listos Para Integrar

- `runtime-rules.json`: economia, weather, ship rules y moon rules.
- `lobby-rules.json`: expanded lobby, late join modes y sync flags.
- `progression.json`: progreso player/ship.
- `perks.json`: catalogo seed de perks player/ship.
- `ProtocolHandshakeDefinition`: fingerprints, feature flags y reglas host/client.
- `AdminCommandService`: comandos listos, pendiente hook real a Terminal.

## Fases

### Fase 1 - Cierre De Nucleo Y Verificacion Inicial

Objetivo: confirmar que lo actual compila, carga y no rompe startup.

Tareas:

- Corregir errores de build cuando haya SDK/runtime disponible.
- Verificar carga BepInEx sin excepciones.
- Confirmar generacion de exports, presets, saves, rules y definitions.
- Confirmar `DryRunOverrides`, strict validation y logs.
- Confirmar snapshot, reset y reload.
- Confirmar route price y moon risk en runtime.

Gate A:

- No avanzar a reglas runtime aplicadas hasta tener startup estable, exports correctos y snapshot/reload/reset funcionando.

### Fase 2 - Producto Data-Driven V1

Objetivo: convertir el nucleo en sistema usable por host/modpack maker.

Tareas:

- Formalizar precedencia: snapshot vanilla, preset, JSON overrides, `.cfg` multipliers/toggles.
- Completar docs de IDs, fields y contratos.
- Consolidar validacion comun y `AbortOnInvalidOverrideBlock`.
- Anadir metricas por fase: aplicados, omitidos, warnings, errores y duracion.
- Loguear fingerprints en startup.

Resultado:

- Un host puede entender exactamente que preset/config se cargo y que mutaciones ocurrieron.

### Fase 3 - Primera Integracion Real De Runtime Rules

Objetivo: activar reglas conservadoras ya verificables.

Tareas:

- Aplicar primero reglas economicas seguras:
  - route price global/per-moon.
  - travel discount via route price.
- Investigar y aplicar solo si se confirma runtime:
  - quota multiplier.
  - deadline multiplier.
  - scrap value multiplier.
- Marcar cada campo como `active`, `reserved` o `experimental`.

Resultado:

- `runtime-rules.json` empieza a producir gameplay real sin sobredimensionarse.

### Fase 4 - Terminal/Admin Hook Apagable

Objetivo: conectar `AdminCommandService` a la Terminal real sin arriesgar compatibilidad.

Tareas:

- Anadir `EnableAdminTerminalCommands`.
- Hook Harmony aislado para input de Terminal.
- Ejecutar solo comandos con prefijo `op`.
- Mantener fallback seguro al flujo vanilla.
- Activar comandos:
  - `op help`
  - `op preset`
  - `op paths`
  - `op export`
  - `op reload`
  - `op reset`
  - `op fingerprint`
  - `op rules`
  - `op perks`
  - `op handshake`
  - `op validate`

Resultado:

- Host/modder puede diagnosticar y recargar sin reiniciar.

### Fase 5 - Perk Appliers Conservadores

Objetivo: convertir perks/progression en gameplay minimo y verificable.

Principio:

- Primero appliers simples y verificables.
- Luego persistencia robusta.
- Luego debug/respec.
- No UX rica todavia.

Tareas:

- Definir policy de progreso: ship host-side, player por ID estable, fallback debug/local.
- Implementar perks seguros:
  - player sprint/stamina/carry/climb/resistance solo con campos confirmados.
  - ship scanner/battery/travel discount/deadline solo con hooks confirmados.
- Anadir comandos debug para inspect/grant/reset/respec.
- Versionar migraciones simples de `progression.json`.

Gate B:

- No avanzar a progresion rica sin runtime rules minimas aplicadas, terminal/admin usable y strict validation clara.

### Fase 6 - Sistema Completo De Host-Configurable Run

Objetivo: expandir Fase 3 hacia reglas tipo AdvancedCompany, pero sobre arquitectura propia.

Tareas:

- Ampliar economia y reglas:
  - quota.
  - deadline.
  - travel discount.
  - scrap value.
  - weather reward tuning.
  - loot retention si hay hook seguro.
  - landing/dropship timing si hay hook seguro.
- Integrar `runtime-rules.json` con presets y fingerprints.
- Mostrar resumen desde `op rules`.

Resultado:

- El host puede configurar de verdad la experiencia del run desde presets/rules.

### Fase 7 - Sync Basico Host/Cliente

Objetivo: detectar incompatibilidad antes de sincronizar estado complejo.

Tareas:

- Usar `ProtocolHandshakeDefinition` como payload real.
- Comparar version, preset, preset fingerprint, config fingerprint y lobby rules.
- Politica inicial:
  - warning por defecto.
  - rechazo solo si config lo exige.
- No sincronizar aun puertas, luces, objetos, inventarios ni late join state.

Resultado:

- Host y cliente saben si tienen la misma configuracion efectiva.

Gate C:

- No comenzar expanded lobby sin presets estables, progression/perks sin corrupcion de saves y handshake/fingerprint funcionando.

### Fase 8 - Expanded Lobby Research Branch

Objetivo: investigar lobby grande sin contaminar el nucleo estable.

Tareas:

- Rama experimental separada.
- Mapear player caps, lobby creation, UI, ownership y player lifecycle.
- Prototipo >4 players sin late join on moon.
- Feature flag fuerte, apagado por default.

Resultado:

- Decision informada sobre viabilidad y costo real.

### Fase 9 - Late Join Safe Mode

Objetivo: abordar late join solo en modos limitados.

Condicion:

- No comenzar Fase 9 hasta que Fase 7 este estable en pruebas multi-cliente reales.

Tareas:

- Implementar primero:
  - `OrbitOnly`
  - `ShipOnly`
- Reusar handshake/fingerprint.
- Bloquear `Moon` hasta tener state recovery real.
- Mantener spectator como subfase posterior.

Gate D:

- No avanzar a late join/spectator avanzado sin pruebas con varios clientes y evidencia de estabilidad en sync base.

### Fase 10 - Multiplayer Avanzado

Objetivo: evaluar paridad espiritual con AdvancedCompany solo despues de tener cimientos.

Tareas posibles:

- Spectator mode.
- Late join on moon.
- Resync parcial de objetos criticos.
- Compat framework para presets/config.
- Expanded lobby estable.

Resultado:

- Solo avanzar si Fases 7-9 demuestran estabilidad real.

## Tabla De Estado Por Subsistema

| Subsistema | Estado |
|---|---|
| BepInEx/Harmony core | In Progress |
| Exports/catalogs | In Progress |
| Item overrides | In Progress |
| Spawn overrides | In Progress |
| Moon overrides | In Progress |
| Validation layer | In Progress |
| Runtime snapshot/reload/reset | In Progress |
| Presets/config hibrida | In Progress |
| Runtime rules | In Progress / Partially Active |
| Admin commands | In Progress / Service Ready |
| Terminal hook | Experimental / Disabled By Default |
| Progression store | Planned / Data-Only |
| Perk catalog | Planned / Data-Only |
| Perk appliers | Planned |
| Lobby rules | Planned / Data-Only |
| Fingerprints/handshake | In Progress / Comparison Ready |
| Host/client sync | Experimental / Contract + Diagnostics |
| Expanded lobby | Experimental / Reflection Scaffold |
| Late join | Experimental / Policy Scaffold |
| Spectator | Experimental / Diagnostics Scaffold |
| Cosmetics/clothing/hotbar rework | Deferred |

## Non-Goals Por Ahora

- No 32 players todavia.
- No late join on moon todavia.
- No spectator mode todavia.
- No portable terminal rica todavia.
- No clothing/cosmetics/modeling.
- No hotbar rework profundo.
- No sync framework general para terceros.
- No port 1:1 de AdvancedCompany.
- No hooks delicados sin feature flag de apagado.

## Pruebas Y Aceptacion

### Sin Runtime

- `git diff --check`.
- Validacion JSON con Node local.
- Revision estatica de paths/config/docs.
- Build cuando `dotnet` este disponible.

### Con Runtime

- Plugin carga en BepInEx.
- Exports se generan correctamente.
- JSON seeds aparecen en folders esperados.
- Dry-run valida sin mutar.
- Overrides aplican con logs claros.
- Snapshot reset/reload funciona.
- Route prices y moon risk cambian in-game.
- Terminal hook no rompe comandos vanilla.
- Fingerprints cambian al modificar config/preset.
- Progression/perks no corrompen saves.

## Supuestos Oficiales

- `default` usa `overseer-data/overrides` y `overseer-data/rules`.
- Presets no sobrescriben archivos editados por usuario.
- Todo hook delicado debe tener config para apagarse.
- Todo campo no verificado debe quedar como data-only, reserved o experimental.
- La prioridad del proyecto es estabilidad, mantenibilidad y observabilidad antes que paridad rapida con AdvancedCompany.
