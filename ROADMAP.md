# Roadmap Oficial De OverseerProtocol

## Resumen

Este roadmap es la direccion principal de OverseerProtocol.

El proyecto prioriza primero un nucleo data-driven, reversible, diagnosticable y configurable; despues progresion, runtime tooling y sync basico; y deja expanded lobby, late join y multiplayer avanzado como fases posteriores sujetas a evidencia real de estabilidad.

Regla guia:

- No convertir contratos data-only en promesas de gameplay hasta que esten aplicados, verificados y documentados como runtime.
- Todo hook delicado debe tener un feature flag de apagado.
- Todo campo no verificado debe quedar como diagnostico, oculto o protegido por feature flag.

## Estado Actual Implementado / Pendiente De Re-Verificacion

### Gameplay / Runtime Implementado, Pendiente De Re-Verificacion

- Plugin BepInEx/Harmony y lifecycle inicial implementados; pendiente re-verificacion en el entorno actual.
- Exports: items, moons, enemies, spawn profiles y moon economy implementados; pendiente re-verificacion runtime.
- Item tuning, spawn tuning y moon tuning implementados desde BepInEx `.cfg`; pendiente re-verificacion runtime.
- Validacion con warnings/errors, strict mode y dry-run implementada; pendiente re-verificacion runtime.
- Runtime multipliers para item weight, spawn rarity y route prices implementados; pendiente re-verificacion runtime.
- Runtime snapshot para reset/reload implementado; pendiente re-verificacion runtime.
- Runtime orchestrator para startup, reload y reset implementado; pendiente re-verificacion runtime.

### Producto / Data Layer Implementado, Pendiente De Re-Verificacion

- BepInEx `.cfg` como fuente unica de verdad editable por el usuario.
- Presets internos con multipliers base.
- Paths organizados para exports diagnosticos, saves y definitions no autoritativas.
- Docs tecnicas por subsistema.
- Exports JSON conservados solo como diagnostico no autoritativo.

### Contratos Data-Only Listos Para Integrar

- Gameplay route rules desde `.cfg`: travel discount y route multiplier per-moon activos.
- Multiplayer desde `.cfg`: max players, late join policy, HUD y compatibilidad basica.
- `progression.json`: progreso player/ship.
- `perks.json`: catalogo seed de perks player/ship.
- `ProtocolHandshakeDefinition`: fingerprints, feature flags y reglas host/client.
- `AdminCommandService`: superficie minima siempre activa para `op help` y `op reload`.

## Fases

### Fase 1 - Cierre De Nucleo Y Verificacion Inicial

Objetivo: confirmar que lo actual compila, carga y no rompe startup.

Tareas:

- Corregir errores de build cuando haya SDK/runtime disponible.
- Verificar carga BepInEx sin excepciones.
- Confirmar generacion de exports diagnosticos y bind de entradas `.cfg` por ID.
- Confirmar `DryRunOverrides`, strict validation y logs.
- Confirmar snapshot, reset y reload.
- Confirmar route price y moon risk en runtime.

Gate A:

- No avanzar a reglas runtime aplicadas hasta tener startup estable, exports correctos y snapshot/reload/reset funcionando.

### Fase 2 - Producto Data-Driven V1

Objetivo: convertir el nucleo en sistema usable por host/modpack maker.

Tareas:

- Formalizar precedencia: snapshot vanilla, preset avanzado, overrides `.cfg`, multipliers/toggles `.cfg`, gameplay route rules `.cfg`.
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
- Marcar cada campo como activo, diagnostico u oculto tras feature flag.

Resultado:

- Runtime rules en `.cfg` empiezan a producir gameplay real sin sobredimensionarse.

### Fase 4 - Terminal/Admin Hook Minimo

Objetivo: conectar `AdminCommandService` a la Terminal real sin convertir la Terminal en una segunda UI de debug.

Tareas:

- Hook Harmony aislado para input de Terminal.
- Ejecutar solo comandos con prefijo `op`.
- Mantener fallback seguro al flujo vanilla.
- Activar solo comandos con efecto practico de testing:
  - `op help`
  - `op reload`
- Mover estado multiplayer, progression y perks al panel in-game.

Resultado:

- Host/modder puede recargar config sin reiniciar; la informacion visual vive en HUD/panel.

### Fase 5 - Persistencia Y Perk Appliers Conservadores

Objetivo: convertir progression/perks en persistencia y gameplay minimo verificable.

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
- Mostrar estado de progression/perks en panel in-game; no usar comandos terminal para perks hasta que existan appliers reales.
- Versionar migraciones simples de `progression.json`.

Gate B:

- No avanzar a progresion rica sin gameplay route rules minimas aplicadas, terminal/admin usable y strict validation clara.

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
- Integrar gameplay route rules `.cfg` con presets y fingerprints.
- Mostrar resumen desde HUD/panel.

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

Principio:

- No forma parte del core stable.
- No bloquea el roadmap principal.
- Puede pausarse o archivarse si el costo real supera el beneficio.

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

## Criterios De Cierre Por Fase

- Fase 1 se considera cerrada cuando el plugin compila, carga sin excepciones, genera exports/seeds esperados y valida snapshot/reload/reset en runtime.
- Fase 2 se considera cerrada cuando la precedencia esta documentada, los logs muestran preset/config efectivos, las metricas por fase existen y los fingerprints aparecen en startup.
- Fase 3 se considera cerrada cuando route price global/per-moon y travel discount aplican desde `.cfg` con campos activos y documentados.
- Fase 4 se considera cerrada cuando `op help` y `op reload` funcionan siempre, el panel cubre informacion visual y el flujo vanilla de Terminal no se rompe.
- Fase 5 se considera cerrada cuando progression usa policy definida, los saves versionados migran sin corrupcion y los perk appliers conservadores tienen comandos debug.
- Fase 6 se considera cerrada cuando el host puede configurar economia/run rules desde presets/rules y revisar el resumen efectivo desde HUD/panel.
- Fase 7 se considera cerrada cuando host/cliente comparan handshake/fingerprints en pruebas reales y la politica warning/reject queda registrada en logs.
- Fase 8 se considera cerrada cuando existe decision documentada de continuar, pausar o archivar expanded lobby segun evidencia tecnica.
- Fase 9 se considera cerrada cuando `OrbitOnly` y `ShipOnly` funcionan en pruebas multi-cliente sin intentar recuperar estado de moon.
- Fase 10 se considera cerrada solo si las fases 7-9 sostienen sync estable y hay evidencia suficiente para features multiplayer avanzadas.

## Tabla De Estado Por Subsistema

Vocabulario base: `Implemented`, `Runtime Verified`, `Pending Runtime Verification`, `Partially Active`, `Data-Only`, `Service Ready`, `Planned`, `Experimental`, `Deferred`.

| Subsistema | Estado |
|---|---|
| BepInEx/Harmony core | Implemented / Pending Runtime Verification |
| Exports/catalogs | Implemented / Pending Runtime Verification |
| Item tuning | Implemented / Pending Runtime Verification |
| Spawn tuning | Implemented / Pending Runtime Verification |
| Moon tuning | Implemented / Pending Runtime Verification |
| Validation layer | Implemented / Pending Runtime Verification |
| Runtime snapshot/reload/reset | Implemented / Pending Runtime Verification |
| Presets/config hibrida | Implemented / Pending Runtime Verification |
| Runtime rules | Partially Active / Experimental |
| Admin commands | Implemented / Service Ready |
| Terminal hook | Implemented / Always On Minimal Surface |
| Progression store | Data-Only |
| Perk catalog | Data-Only |
| Perk appliers | Planned |
| Lobby rules | Data-Only |
| Fingerprints/handshake | Implemented / Comparison Ready |
| Host/client sync | Experimental / Diagnostics Only |
| Expanded lobby | Experimental / Research Scaffold |
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
- Revision estatica de paths/config/docs.
- Build cuando `dotnet` este disponible.

### Con Runtime

- Plugin carga en BepInEx.
- Exports se generan correctamente.
- No se generan seeds JSON editables.
- Dry-run valida sin mutar.
- Tuning de usuario aplica con logs claros.
- Snapshot reset/reload funciona.
- Route prices y moon risk cambian in-game.
- Terminal hook no rompe comandos vanilla.
- Panel in-game con keybind configurable muestra estado multiplayer y progression/perks sin depender de comandos terminal.
- Fingerprints cambian al modificar config/preset.
- Progression/perks no corrompen saves.

## Supuestos Oficiales

- `default` usa la configuracion BepInEx `.cfg` y no depende de JSON editable.
- Presets no sobrescriben archivos editados por usuario.
- Todo hook delicado debe tener config para apagarse.
- Todo campo no verificado debe quedar como diagnostico, oculto o protegido por feature flag.
- La prioridad del proyecto es estabilidad, mantenibilidad y observabilidad antes que paridad rapida con AdvancedCompany.

## Compatibilidad Y Versionado

- Este roadmap asume la version actual de Lethal Company validada contra la rama principal del mod.
- Cualquier cambio fuerte en assemblies internos del juego puede mover subsistemas de vuelta a `Pending Runtime Verification`.
- Los exports JSON son diagnosticos no autoritativos.
- Cambios incompatibles de `.cfg` deben documentar migracion o fallback sin sobrescribir al usuario.

## Rollback Policy

- Toda mutacion runtime debe ser reversible cuando sea tecnicamente posible.
- Snapshot/reset/reload tienen prioridad sobre acumulacion de parches incrementales dificiles de auditar.
- Ante error de validacion critica, el mod debe preferir omitir el bloque afectado o abortar segun configuracion antes que dejar estado parcial silencioso.
