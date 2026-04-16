# Lobby Rules V1

Lobby rules are the configuration contract for multiplayer experiments. The mod creates and loads the rules file, and experimental reflection-based scaffolding can consume parts of it when explicitly enabled.

Default path:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/rules/lobby-rules.json
```

Preset path:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-id>/rules/lobby-rules.json
```

Config gate:

```ini
[Lobby]
EnableLobbyRulesLoading = true
```

## Contract

```json
{
  "schemaVersion": 1,
  "maxPlayers": 4,
  "enableExpandedLobby": false,
  "allowLateJoin": false,
  "lateJoinMode": "Disabled",
  "enableSpectatorMode": false,
  "requireMatchingOverseerVersion": true,
  "requireMatchingPreset": true,
  "syncPresetToClients": true,
  "syncOverridesToClients": false
}
```

## Late Join Modes

The intended values are:

- `Disabled`: No late join support.
- `OrbitOnly`: Future safe mode; late join only before landing or while in orbit.
- `ShipOnly`: Future recovery mode; spawn/recover late joiners in ship contexts only.
- `Spectator`: Future advanced mode; late joiners enter as spectators.
- `Moon`: Future high-risk mode; requires full moon state reconstruction.

## Sync Handshake

The handshake model is defined separately from runtime networking. It is meant to carry:

- OverseerProtocol version.
- Game version.
- Active preset.
- Preset/config fingerprints.
- Lobby rules that clients must accept.
- Enabled feature flags.

## Experimental Runtime Status

When `EnableExperimentalMultiplayer` and `EnableExpandedLobbyPatch` are enabled, OverseerProtocol attempts a conservative reflection-based max-player patch using `maxPlayers`-style fields/properties on known singleton objects.

This does not guarantee:

- lobby UI resizing,
- player object lifecycle correctness,
- ownership correctness,
- connection approval,
- late join state recovery,
- spectator runtime behavior.

Late join modes are policy-only for now. `Moon` mode is intentionally blocked until moon state recovery exists.

Actual network enforcement should be implemented only after expanded lobby research is done.
