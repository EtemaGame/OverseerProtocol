# Experimental Multiplayer V1

This layer exists because expanded lobby, late join, spectator mode, and state sync are core ambitions for OverseerProtocol. It is intentionally disabled by default.

## Config

```ini
[Multiplayer]
EnableExperimentalPatches = false
EnableExpandedLobbyPatch = false
EnableLateJoinSafeMode = false
EnableSpectatorModeScaffold = false
EnableHandshakeCompatibilityChecks = true
ExperimentalMaxPlayers = 4
```

## What Exists

- Dynamic Harmony hook registration for likely multiplayer lifecycle methods.
- Reflection-based max-player patch attempts.
- Late-join policy diagnostics.
- Spectator mode diagnostics.
- Reserved sync snapshot model.
- Admin commands:
  - `op multiplayer`
  - `op multiplayer apply`
  - `op sync snapshot`

## What Does Not Exist Yet

- Safe UI expansion.
- Network approval override.
- Runtime player object lifecycle repair.
- Moon late join state recovery.
- Spectator player lifecycle control.
- Door/light/object/inventory resync.

## Safety Rules

- All multiplayer experiments are disabled by default.
- `LateJoinMode = Moon` is blocked by policy until recovery hooks exist.
- Reflection patches log what they find and no-op when fields/methods do not exist.
- This layer should remain separable from the stable data-driven tuning core.
