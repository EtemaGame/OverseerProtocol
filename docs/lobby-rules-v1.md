# Lobby Rules V1

Lobby rules are a data contract only in V1. The mod creates and loads the rules file, but it does not yet patch player caps, late join, spectator mode, or network approval.

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

Actual network enforcement should be implemented only after expanded lobby research is done.
