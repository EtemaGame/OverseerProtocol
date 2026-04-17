# Hybrid Configuration V1

OverseerProtocol now has two configuration layers:

- `.cfg`: Simple user-facing switches, preset selection, and broad multipliers.
- JSON: Detailed entity-level tuning for items, moons, and spawn pools.

## BepInEx Config

BepInEx generates the config at:

```text
BepInEx/config/com.overseerprotocol.core.cfg
```

Current entries:

```ini
[General]
EnableDataExport = true
ActivePreset = default

[Overrides]
EnableItemOverrides = true
EnableMoonOverrides = true
EnableSpawnOverrides = true

[Validation]
StrictValidation = false
DryRunOverrides = false

[Multipliers]
EnableRuntimeMultipliers = true
ItemWeightMultiplier = 1
SpawnRarityMultiplier = 1
RoutePriceMultiplier = 1

[SemanticDifficulty]
AggressionProfile = Balanced

[Progression]
EnableProgressionStorage = true
EnablePerkCatalog = true

[Lobby]
EnableLobbyRulesLoading = true

[ExperimentalMultiplayer]
EnableExperimentalMultiplayer = false
EnableExpandedLobbyPatch = false
EnableLateJoinSafeMode = false
EnableSpectatorModeScaffold = false
EnableHandshakeCompatibilityChecks = true
ExperimentalMaxPlayers = 4

[RuntimeRules]
EnableRuntimeRulesLoading = true

[Admin]
EnableAdminTerminalCommands = false
AdminCommandPrefix = op
```

## Preset Resolution

`ActivePreset` selects preset metadata and default multipliers. User-facing entity tuning is global to the current profile and lives in:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/items.json
BepInEx/plugins/OverseerProtocol/overseer-data/moons/<MoonId>.json
BepInEx/plugins/OverseerProtocol/overseer-data/utility-catalog.json
```

For example, this selects the `hardcore` preset multipliers:

```ini
[General]
ActivePreset = hardcore
```

Preset names are sanitized before becoming folder names.

## Runtime Multipliers

Runtime multipliers run after user JSON tuning.

- `ItemWeightMultiplier`: Multiplies every runtime item `weight`.
- `SpawnRarityMultiplier`: Multiplies every runtime spawn rarity in inside, outside, and daytime pools.
- `RoutePriceMultiplier`: Multiplies every runtime terminal route price.
- Spawn rarities are clamped to `0..1000`.
- Multiplier values are clamped by config to `0..10`.

Exports still happen before tuning and multipliers when `EnableDataExport = true`, so exported catalogs remain the vanilla snapshot.

## Validation Controls

- `StrictValidation`: when enabled, any validation warning aborts the affected tuning flow. Validation errors always abort.
- `DryRunOverrides`: when enabled, the mod still exports and validates configuration, but it does not apply item tuning, spawn tuning, moon tuning, runtime multipliers, or runtime rules.
- `AbortOnInvalidOverrideBlock`: strict policy flag. When enabled, validation warnings abort the affected tuning flow instead of applying the sanitized subset.

Before export, OverseerProtocol captures a lightweight in-memory runtime snapshot for item values and moon spawn pools. The snapshot is restored after export and before user tuning, keeping the exported catalogs and the mutation baseline aligned with vanilla runtime state.

## Progression Storage

`EnableProgressionStorage` creates and loads the V1 progression save file at:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/saves/progression.json
```

This is data-only for now. Runtime perks are intentionally deferred until the tuning and admin tooling layers are stable.

## Lobby Rules

`EnableLobbyRulesLoading` creates and loads the V1 lobby rules file. This is also data-only for now; expanded lobby, late join, spectator mode, and handshake enforcement remain future runtime work.

## Experimental Multiplayer

Experimental multiplayer is disabled by default. It provides reflection-based scaffolding for expanded lobby diagnostics, late-join policy checks, spectator diagnostics, and future handshake/sync work.

Current behavior:

- `EnableExpandedLobbyPatch`: attempts to set known max-player integer fields/properties on known singleton objects. This is not guaranteed to patch UI, ownership, spawn lifecycle, or network approval.
- `EnableLateJoinSafeMode`: evaluates late-join policy and blocks `Moon` mode by policy until state recovery exists.
- `EnableSpectatorModeScaffold`: logs spectator readiness only; it does not control player lifecycle yet.
- `EnableHandshakeCompatibilityChecks`: keeps local handshake comparison services available for future networking.

## Runtime Rules

`EnableRuntimeRulesLoading` creates and loads the V1 runtime rules file for future quota, deadline, travel discount, ship timing, weather reward, and moon-specific balancing.

## Admin Terminal

`EnableAdminTerminalCommands` enables the experimental Terminal hook. It is disabled by default because Terminal input patches are compatibility-sensitive.

When enabled, commands use `AdminCommandPrefix`, which defaults to `op`.
