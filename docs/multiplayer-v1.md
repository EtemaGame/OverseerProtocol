# Multiplayer V1

Multiplayer V1 exposes a public, non-experimental config surface for lobby capacity, late join policy, compatibility checks, and an in-game status HUD.

## Config

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

## Runtime Behavior

- `MaxPlayers` is applied to `GameNetworkManager.maxAllowedPlayers`, lobby metadata, and connection approval capacity checks.
- Joining while the host is in lobby or orbit is allowed when late join is enabled.
- Joining during an active moon can be approved as spectator when `LateJoinOnMoonAsSpectator=true`.
- Joining during loading, disconnect, game over, or other transition states is rejected with a clear reason.
- The HUD reports player count, late join mode, and lobby state when `ShowLobbyStatusHud=true`.

## In-Game Status

Multiplayer status is shown through the HUD and the Overseer panel. The panel uses the `Open Overseer Panel` InputUtils keybind; status is no longer duplicated through terminal commands.

Moon spectator join intentionally uses vanilla dead/spectator fields where possible. It does not attempt a live mid-round respawn.
