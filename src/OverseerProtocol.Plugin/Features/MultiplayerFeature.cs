using System;
using System.Collections.Generic;
using System.Reflection;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Sync;
using Unity.Netcode;
using UnityEngine;

namespace OverseerProtocol.Features;

public sealed class MultiplayerFeature
{
    private static readonly HashSet<ulong> PendingMoonSpectators = new();
    private static int _lastAppliedMaxPlayers = -1;
    private static bool _lobbyJoinable = true;

    public void Apply()
    {
        if (!OPConfig.EnableMultiplayer.Value)
            return;

        ApplyMaxPlayers();
        RefreshLobbyJoinable("multiplayer apply");
        UpdateCurrentLobbyData();
    }

    public void ApplyMaxPlayers()
    {
        var target = GetMaxPlayersForPatch();
        var manager = GameNetworkManager.Instance;
        if (manager == null)
        {
            LogNetwork("GameNetworkManager.Instance is not available; max player apply deferred.", verboseOnly: true);
            return;
        }

        var before = manager.maxAllowedPlayers;
        manager.maxAllowedPlayers = target;
        if (_lastAppliedMaxPlayers != target || before != target)
        {
            OPLog.Info("Multiplayer", $"Applied max players: GameNetworkManager.maxAllowedPlayers {before} -> {target}");
            _lastAppliedMaxPlayers = target;
        }
    }

    public static int GetMaxPlayersForPatch()
    {
        var configured = OPConfig.MaxPlayers?.Value ?? 4;
        if (configured < 1)
            return 1;

        return configured > 64 ? 64 : configured;
    }

    public static bool ShouldOverrideStartedGameApproval(GameNetworkManager manager)
    {
        if (!OPConfig.EnableMultiplayer.Value || !OPConfig.EnableLateJoin.Value)
            return false;

        if (manager == null || !manager.gameHasStarted)
            return false;

        return true;
    }

    public static bool TryApproveStartedGameConnection(
        GameNetworkManager manager,
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        var decision = new MultiplayerFeature().EvaluateJoinPolicy(manager, request.ClientNetworkId);
        if (!decision.Approved)
        {
            response.Reason = decision.Reason;
            response.CreatePlayerObject = false;
            response.Approved = false;
            response.Pending = false;
            OPLog.Warning("Multiplayer", $"Late join rejected: client={request.ClientNetworkId}, reason={decision.Reason}");
            return true;
        }

        if (decision.AsSpectator)
            PendingMoonSpectators.Add(request.ClientNetworkId);

        response.Reason = "";
        response.CreatePlayerObject = false;
        response.Approved = true;
        response.Pending = false;
        OPLog.Info("Multiplayer", $"Late join approved: client={request.ClientNetworkId}, mode={(decision.AsSpectator ? "MoonSpectator" : "Orbit")}");
        return true;
    }

    public JoinDecision EvaluateJoinPolicy(GameNetworkManager manager, ulong clientId)
    {
        if (manager.isDisconnecting)
            return JoinDecision.Reject("Host is disconnecting.");

        if (manager.disallowConnection)
            return JoinDecision.Reject("Host is not accepting connections.");

        if (manager.connectedPlayers >= GetMaxPlayersForPatch())
            return JoinDecision.Reject("Lobby is full.");

        var round = StartOfRound.Instance;
        if (round == null)
            return OPConfig.LateJoinInOrbit.Value
                ? JoinDecision.AllowAlive("No active round yet; treating as lobby/orbit.")
                : JoinDecision.Reject("Late join in lobby/orbit is disabled.");

        if (IsCriticalTransition(round))
            return JoinDecision.Reject("Round is in a transition state.");

        if (round.inShipPhase || !round.shipHasLanded)
        {
            return OPConfig.LateJoinInOrbit.Value
                ? JoinDecision.AllowAlive("Ship is in orbit/ship phase.")
                : JoinDecision.Reject("Late join in orbit is disabled.");
        }

        if (OPConfig.LateJoinOnMoonAsSpectator.Value)
            return JoinDecision.AllowSpectator("Ship is on a moon; joining as spectator.");

        return JoinDecision.Reject("Ship is on a moon and spectator late join is disabled.");
    }

    public static bool IsLobbyJoinableForInvites() =>
        OPConfig.EnableMultiplayer.Value &&
        OPConfig.EnableLateJoin.Value &&
        _lobbyJoinable &&
        GameNetworkManager.Instance != null &&
        !GameNetworkManager.Instance.disallowConnection &&
        !GameNetworkManager.Instance.isDisconnecting;

    public static void RefreshLobbyJoinable(string reason)
    {
        if (!OPConfig.EnableMultiplayer.Value || !OPConfig.EnableLateJoin.Value)
        {
            SetLobbyJoinable(false, reason + ": multiplayer/late join disabled");
            return;
        }

        var manager = GameNetworkManager.Instance;
        if (manager == null)
            return;

        if (manager.connectedPlayers >= GetMaxPlayersForPatch())
        {
            SetLobbyJoinable(false, reason + ": lobby full");
            return;
        }

        var round = StartOfRound.Instance;
        if (manager.isDisconnecting || manager.disallowConnection || (round != null && IsCriticalTransition(round)))
        {
            SetLobbyJoinable(false, reason + ": transition/closed");
            return;
        }

        if (round == null || round.inShipPhase || !round.shipHasLanded)
        {
            SetLobbyJoinable(OPConfig.LateJoinInOrbit.Value, reason + ": orbit");
            return;
        }

        SetLobbyJoinable(OPConfig.LateJoinOnMoonAsSpectator.Value, reason + ": moon spectator");
    }

    public static void SetLobbyJoinable(bool joinable, string reason)
    {
        _lobbyJoinable = joinable;

        var manager = GameNetworkManager.Instance;
        if (manager == null)
            return;

        try
        {
            manager.SetLobbyJoinable(joinable);
            UpdateInviteButtonAlpha(joinable);
            OPLog.Info("Multiplayer", $"Lobby joinable={joinable} ({reason}).");
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Could not set lobby joinable={joinable}: {ex.Message}");
        }
    }

    public static bool IsCriticalTransition(StartOfRound round)
    {
        return round.newGameIsLoading ||
               round.beganLoadingNewLevel ||
               round.travellingToNewLevel ||
               round.shipIsLeaving ||
               round.allPlayersDead ||
               round.firingPlayersCutsceneRunning;
    }

    public static void PromotePendingSpectatorIfNeeded(ulong clientId, int assignedPlayerObjectId)
    {
        if (!PendingMoonSpectators.Remove(clientId))
            return;

        var round = StartOfRound.Instance;
        if (round?.allPlayerScripts == null ||
            assignedPlayerObjectId < 0 ||
            assignedPlayerObjectId >= round.allPlayerScripts.Length)
        {
            OPLog.Warning("Multiplayer", $"Could not mark late join client {clientId} as spectator: assigned player id is invalid.");
            return;
        }

        var player = round.allPlayerScripts[assignedPlayerObjectId];
        if (player == null)
        {
            OPLog.Warning("Multiplayer", $"Could not mark late join client {clientId} as spectator: player script is null.");
            return;
        }

        player.isPlayerDead = true;
        player.hasBegunSpectating = true;
        player.disableMoveInput = true;
        player.disableInteract = true;
        player.isInHangarShipRoom = false;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            round.overrideSpectateCamera = true;
            try
            {
                player.GetType().GetMethod("SpectateNextPlayer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(player, new object[] { false });
            }
            catch (Exception ex)
            {
                OPLog.Warning("Multiplayer", $"Local spectator camera handoff failed: {ex.Message}");
            }
        }

        OPLog.Info("Multiplayer", $"Late join client {clientId} assigned to spectator state until the next orbit revive.");
    }

    public string BuildStatusText()
    {
        var manager = GameNetworkManager.Instance;
        var round = StartOfRound.Instance;
        var connected = ResolveConnectedPlayers(manager, round);
        var maxPlayers = GetMaxPlayersForPatch();
        var state = ResolveLobbyState(manager, round);
        var lateJoin = ResolveLateJoinStatus();

        return "Overseer: " + connected + "/" + maxPlayers + " players\n" +
               "Late Join: " + lateJoin + "\n" +
               "Lobby Status: " + state;
    }

    public string BuildPlayersText()
    {
        var round = StartOfRound.Instance;
        if (round?.allPlayerScripts == null)
            return "Players: runtime player list is not available.";

        var lines = new List<string> { "Players:" };
        for (var i = 0; i < round.allPlayerScripts.Length; i++)
        {
            var player = round.allPlayerScripts[i];
            if (player == null || !player.isPlayerControlled)
                continue;

            var name = string.IsNullOrWhiteSpace(player.playerUsername) ? "Player " + i : player.playerUsername;
            var state = player.isPlayerDead ? "spectator/dead" : "alive";
            lines.Add(i + ": " + name + " (" + state + ")");
        }

        return lines.Count == 1 ? "Players: none connected yet." : string.Join("\n", lines);
    }

    public ProtocolHandshakeDefinition BuildHandshake() =>
        new HandshakeFeature().BuildCurrent();

    public RuntimeStateSyncSnapshotDefinition BuildSyncSnapshot()
    {
        return new RuntimeStateSyncSnapshotDefinition
        {
            SnapshotId = Guid.NewGuid().ToString("N"),
            CreatedUtc = DateTime.UtcNow.ToString("O"),
            ActivePreset = OPConfig.ActivePresetName,
            ShipState =
            {
                new SyncStateEntry
                {
                    Id = "ship-state",
                    Kind = "Status",
                    State = ResolveLobbyState(GameNetworkManager.Instance, StartOfRound.Instance),
                    Source = "MultiplayerFeature"
                }
            },
            MoonState =
            {
                new SyncStateEntry
                {
                    Id = "moon-state",
                    Kind = "Status",
                    State = ResolveLateJoinStatus(),
                    Source = "MultiplayerFeature"
                }
            }
        };
    }

    private static int ResolveConnectedPlayers(GameNetworkManager? manager, StartOfRound? round)
    {
        if (round != null)
            return Math.Max(1, round.connectedPlayersAmount + 1);

        return manager == null ? 0 : Math.Max(0, manager.connectedPlayers);
    }

    private static string ResolveLobbyState(GameNetworkManager? manager, StartOfRound? round)
    {
        if (manager != null && manager.isDisconnecting)
            return "Closed/Disconnecting";

        if (round == null)
            return "Open";

        if (IsCriticalTransition(round))
            return "Closed/Transition";

        if (round.inShipPhase || !round.shipHasLanded)
            return "Open/Orbit";

        return "In Moon";
    }

    private static string ResolveLateJoinStatus()
    {
        if (!OPConfig.EnableLateJoin.Value)
            return "Closed";

        if (OPConfig.LateJoinOnMoonAsSpectator.Value)
            return "Orbit + Spectator";

        return OPConfig.LateJoinInOrbit.Value ? "Orbit" : "Closed";
    }

    private static void UpdateCurrentLobbyData()
    {
        var manager = GameNetworkManager.Instance;
        if (manager == null)
            return;

        try
        {
            var currentLobbyProperty = typeof(GameNetworkManager).GetProperty("currentLobby", BindingFlags.Public | BindingFlags.Instance);
            var nullableLobby = currentLobbyProperty?.GetValue(manager);
            if (nullableLobby == null)
                return;

            var hasValueProperty = nullableLobby.GetType().GetProperty("HasValue");
            if (hasValueProperty?.GetValue(nullableLobby) is not true)
                return;

            var valueProperty = nullableLobby.GetType().GetProperty("Value");
            var lobby = valueProperty?.GetValue(nullableLobby);
            if (lobby == null)
                return;

            var lobbyType = lobby.GetType();
            lobbyType.GetProperty("MaxMembers")?.SetValue(lobby, GetMaxPlayersForPatch());
            lobbyType.GetMethod("SetJoinable", new[] { typeof(bool) })?.Invoke(lobby, new object[] { IsLobbyJoinableForInvites() });
            lobbyType.GetMethod("SetData", new[] { typeof(string), typeof(string) })?.Invoke(lobby, new object[] { "overseer_max", GetMaxPlayersForPatch().ToString() });
            lobbyType.GetMethod("SetData", new[] { typeof(string), typeof(string) })?.Invoke(lobby, new object[] { "overseer_late", ResolveLateJoinStatus() });
            lobbyType.GetMethod("SetData", new[] { typeof(string), typeof(string) })?.Invoke(lobby, new object[] { "joinable", IsLobbyJoinableForInvites() ? "true" : "false" });
            LogNetwork("Updated Steam lobby metadata for Overseer multiplayer.", verboseOnly: true);
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Could not update Steam lobby metadata: {ex.Message}");
        }
    }

    private static void UpdateInviteButtonAlpha(bool joinable)
    {
        try
        {
            var quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
            if (quickMenu == null)
                return;

            var field = typeof(QuickMenuManager).GetField("inviteFriendsTextAlpha", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var canvasGroup = field?.GetValue(quickMenu);
            if (canvasGroup == null)
                return;

            canvasGroup.GetType().GetProperty("alpha", BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(canvasGroup, joinable ? 1f : 0.2f, null);
        }
        catch (Exception ex)
        {
            LogNetwork($"Could not update invite friends button alpha: {ex.Message}", verboseOnly: true);
        }
    }

    private static void LogNetwork(string message, bool verboseOnly)
    {
        var level = OPConfig.NetworkLogLevel?.Value ?? "Normal";
        if (verboseOnly && !string.Equals(level, "Verbose", StringComparison.OrdinalIgnoreCase))
            return;

        if (!string.Equals(level, "Quiet", StringComparison.OrdinalIgnoreCase))
            OPLog.Info("Multiplayer", message);
    }
}

public sealed class JoinDecision
{
    private JoinDecision(bool approved, bool asSpectator, string reason)
    {
        Approved = approved;
        AsSpectator = asSpectator;
        Reason = reason;
    }

    public bool Approved { get; }
    public bool AsSpectator { get; }
    public string Reason { get; }

    public static JoinDecision AllowAlive(string reason) => new(true, false, reason);
    public static JoinDecision AllowSpectator(string reason) => new(true, true, reason);
    public static JoinDecision Reject(string reason) => new(false, false, reason);
}
