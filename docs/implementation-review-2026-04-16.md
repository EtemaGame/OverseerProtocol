# Implementation Review - 2026-04-16

## Reference State

Available locally:

- `references/bepinex/BepInEx.dll`
- `references/bepinex/0Harmony.dll`

Still missing locally:

- `references/game/Assembly-CSharp.dll`
- `references/game/Unity.Netcode.Runtime.dll`
- `references/game/UnityEngine.dll`
- `references/game/UnityEngine.CoreModule.dll`

Because game assemblies are not present, this review could not verify Lethal Company runtime symbols such as `Terminal`, `TerminalNode`, `StartOfRound`, `GameNetworkManager`, or `PlayerControllerB`.

## Static Review Summary

This review is historical. Current runtime-facing docs supersede any note below that says terminal or multiplayer is disabled by default.

Implemented and wired:

- Runtime orchestration with startup/reload/reset pipeline.
- Phase metrics and startup fingerprints.
- Strict validation policy through `AbortOnInvalidOverrideBlock`.
- Runtime route/economy rules for travel discount and per-moon route price multipliers.
- Minimal always-on terminal admin hook.
- Progression/perk read-only panel.
- Handshake compatibility comparison service.
- Multiplayer V1 public config surface.
- Reference setup helper for BepInEx and local game assemblies.

## High-Risk Areas Requiring Runtime Verification

- `Terminal.ParsePlayerSentence` hook shape and return handling.
- Terminal input text source field/property.
- `TerminalNode` response rendering fields.
- Route price mutation through `TerminalNode.itemCost`.
- Moon risk mutation through `SelectableLevel.riskLevel`.
- Multiplayer capacity, lobby metadata, and lifecycle hooks.
- Lifecycle hooks used by `MultiplayerHook`.
- Snapshot restore behavior after live runtime mutation.

## Known Safe Boundaries

- Terminal command surface is limited to `op help` and `op reload`.
- Moon late join is spectator-gated and avoids live mid-round respawn.
- Runtime state sync snapshot is diagnostic and does not mutate gameplay.
- Proprietary game assemblies are not downloaded from third-party sources.

## Verification Performed

- JSON sample validation with local Node: passed.
- `git diff --check`: passed.
- BepInEx/Harmony reference download from official GitHub release: completed.

Not performed:

- `dotnet build`, because `dotnet` is not available in PATH.
- Runtime launch, because local game assemblies/runtime are not available in this environment.
