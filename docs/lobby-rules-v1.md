# Lobby Rules V1

Lobby rules are configured in `BepInEx/config/com.overseerprotocol.core.cfg`.

```ini
[Lobby]
EnableLobbyRulesLoading = true
MaxPlayers = 4
EnableExpandedLobby = false
AllowLateJoin = false
LateJoinMode = Disabled
EnableSpectatorMode = false
RequireMatchingOverseerVersion = true
RequireMatchingPreset = true
SyncPresetToClients = true
SyncOverridesToClients = false
```

There is no `lobby-rules.json` source of truth.

## Runtime Status

Experimental multiplayer remains disabled by default. Expanded lobby patching is reflection-based and may not cover UI, ownership, player lifecycle, approval, or late-join state recovery.

`LateJoinMode = Moon` is still blocked by policy until full moon state reconstruction exists.
