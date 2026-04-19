# Lobby Rules V1

Lobby rules are configured in `BepInEx/config/com.overseerprotocol.core.cfg`.

```ini
[Multiplayer]
EnableMultiplayer = true
MaxPlayers = 8
EnableLateJoin = true
LateJoinInOrbit = true
LateJoinOnMoonAsSpectator = true
ShowLobbyStatusHud = true
RequireSameModVersion = true
RequireSameConfigHash = false
```

There is no `lobby-rules.json` source of truth.

## Runtime Status

Multiplayer is a public feature surface. Lobby capacity, invite button state, lobby metadata, and approval policy are patched directly where the current runtime exposes supported hooks.

Moon late join is spectator-gated. It does not attempt live mid-round respawn.
