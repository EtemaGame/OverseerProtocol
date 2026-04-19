using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;
using Unity.Netcode;

namespace OverseerProtocol.Patches;

internal static class MultiplayerHook
{
    public static void TryPatch(Harmony harmony)
    {
        if (!OPConfig.EnableMultiplayer.Value)
        {
            OPLog.Info("Multiplayer", "Multiplayer hooks disabled by config.");
            return;
        }

        PatchPostfix(harmony, "GameNetworkManager", "Awake", nameof(ApplyPostfix));
        PatchPostfix(harmony, "GameNetworkManager", "Start", nameof(ApplyPostfix));
        PatchPostfix(harmony, "GameNetworkManager", "SteamMatchmaking_OnLobbyCreated", nameof(ApplyPostfix));
        PatchPrefix(harmony, "GameNetworkManager", "ConnectionApproval", nameof(ConnectionApprovalPrefix));
        PatchPostfix(harmony, "GameNetworkManager", "ConnectionApproval", nameof(ConnectionApprovalPostfix));
        PatchTranspiler(harmony, "GameNetworkManager", "ConnectionApproval", nameof(ConnectionApprovalTranspiler));
        PatchPostfix(harmony, "GameNetworkManager", "LobbyDataIsJoinable", nameof(LobbyDataIsJoinablePostfix));
        PatchPrefix(harmony, "GameNetworkManager", "LeaveLobbyAtGameStart", nameof(LeaveLobbyAtGameStartPrefix));
        PatchPrefix(harmony, "Steamworks.SteamMatchmaking", "CreateLobbyAsync", nameof(CreateLobbyAsyncPrefix));
        PatchPrefix(harmony, "QuickMenuManager", "DisableInviteFriendsButton", nameof(DisableInviteFriendsButtonPrefix));
        PatchPrefix(harmony, "QuickMenuManager", "InviteFriendsButton", nameof(InviteFriendsButtonPrefix));
        PatchPostfix(harmony, "StartOfRound", "Start", nameof(ApplyPostfix));
        PatchPrefix(harmony, "StartOfRound", "StartGame", nameof(StartGamePrefix));
        PatchPostfix(harmony, "StartOfRound", "SetShipReadyToLand", nameof(SetShipReadyToLandPostfix));
        PatchPostfix(harmony, "StartOfRound", "OnShipLandedMiscEvents", nameof(OnShipLandedMiscEventsPostfix));
        PatchPostfix(harmony, "StartOfRound", "OnPlayerDC", nameof(OnPlayerDcPostfix));
        PatchPostfix(harmony, "StartOfRound", "OnPlayerConnectedClientRpc", nameof(OnPlayerConnectedClientRpcPostfix));
    }

    private static void PatchPrefix(Harmony harmony, string typeName, string methodName, string prefixName)
    {
        Patch(harmony, typeName, methodName, prefixName, postfixName: null);
    }

    private static void PatchPostfix(Harmony harmony, string typeName, string methodName, string postfixName)
    {
        Patch(harmony, typeName, methodName, prefixName: null, postfixName);
    }

    private static void PatchTranspiler(Harmony harmony, string typeName, string methodName, string transpilerName)
    {
        Patch(harmony, typeName, methodName, prefixName: null, postfixName: null, transpilerName);
    }

    private static void Patch(Harmony harmony, string typeName, string methodName, string? prefixName, string? postfixName, string? transpilerName = null)
    {
        var type = AccessTools.TypeByName(typeName);
        if (type == null)
        {
            OPLog.Warning("Multiplayer", $"Cannot patch {typeName}.{methodName}: type not found.");
            return;
        }

        var method = AccessTools.Method(type, methodName);
        if (method == null)
        {
            OPLog.Warning("Multiplayer", $"Cannot patch {typeName}.{methodName}: method not found.");
            return;
        }

        try
        {
            var prefix = prefixName == null ? null : new HarmonyMethod(typeof(MultiplayerHook), prefixName);
            var postfix = postfixName == null ? null : new HarmonyMethod(typeof(MultiplayerHook), postfixName);
            var transpiler = transpilerName == null ? null : new HarmonyMethod(typeof(MultiplayerHook), transpilerName);
            harmony.Patch(method, prefix, postfix, transpiler);
            OPLog.Info("Multiplayer", $"Patched multiplayer hook on {typeName}.{methodName}.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Failed to patch {typeName}.{methodName}: {ex.Message}");
        }
    }

    private static void ApplyPostfix()
    {
        try
        {
            if (!OPConfig.EnableMultiplayer.Value)
                return;

            new MultiplayerFeature().Apply();
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Multiplayer apply hook failed: {ex.Message}");
        }
    }

    private static bool ConnectionApprovalPrefix(
        GameNetworkManager __instance,
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            if (!OPConfig.EnableMultiplayer.Value ||
                NetworkManager.Singleton == null ||
                request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
            {
                return true;
            }

            if (!ValidateGameVersion(__instance, request, response))
                return false;

            if (!__instance.gameHasStarted)
            {
                if (__instance.connectedPlayers >= MultiplayerFeature.GetMaxPlayersForPatch())
                {
                    response.Reason = "Lobby is full!";
                    response.CreatePlayerObject = false;
                    response.Approved = false;
                    response.Pending = false;
                    OPLog.Warning("Multiplayer", $"Connection rejected by Overseer capacity: connected={__instance.connectedPlayers}, max={MultiplayerFeature.GetMaxPlayersForPatch()}");
                    return false;
                }

                return true;
            }

            if (!MultiplayerFeature.ShouldOverrideStartedGameApproval(__instance))
                return true;

            MultiplayerFeature.TryApproveStartedGameConnection(__instance, request, response);
            return false;
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Connection approval hook failed. Falling back to vanilla approval. {ex.Message}");
            return true;
        }
    }

    private static void ConnectionApprovalPostfix(
        GameNetworkManager __instance,
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            if (!OPConfig.EnableMultiplayer.Value ||
                request.ClientNetworkId == NetworkManager.Singleton?.LocalClientId)
            {
                return;
            }

            if (response.Approved)
                return;

            if (!__instance.gameHasStarted &&
                string.Equals(response.Reason, "Lobby is full!", StringComparison.OrdinalIgnoreCase) &&
                __instance.connectedPlayers < MultiplayerFeature.GetMaxPlayersForPatch())
            {
                response.Reason = "";
                response.Approved = true;
                response.Pending = false;
                response.CreatePlayerObject = true;
                OPLog.Info("Multiplayer", $"Overrode vanilla four-player lobby rejection: connected={__instance.connectedPlayers}, max={MultiplayerFeature.GetMaxPlayersForPatch()}.");
                return;
            }

            if (__instance.gameHasStarted &&
                string.Equals(response.Reason, "Game has already started!", StringComparison.OrdinalIgnoreCase) &&
                MultiplayerFeature.ShouldOverrideStartedGameApproval(__instance))
            {
                MultiplayerFeature.TryApproveStartedGameConnection(__instance, request, response);
            }
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Connection approval postfix failed: {ex.Message}");
        }
    }

    private static IEnumerable<CodeInstruction> ConnectionApprovalTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var result = new List<CodeInstruction>();
        var foundConnectedPlayers = false;
        var replaced = false;

        foreach (var instruction in instructions)
        {
            if (!replaced)
            {
                if (!foundConnectedPlayers &&
                    instruction.opcode == OpCodes.Ldfld &&
                    instruction.operand?.ToString() == "System.Int32 GameNetworkManager::connectedPlayers")
                {
                    foundConnectedPlayers = true;
                }
                else if (foundConnectedPlayers && instruction.opcode == OpCodes.Ldc_I4_4)
                {
                    result.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MultiplayerHook), nameof(GetMaxPlayersForApproval))));
                    replaced = true;
                    continue;
                }
            }

            result.Add(instruction);
        }

        if (replaced)
            OPLog.Info("Multiplayer", "Patched GameNetworkManager.ConnectionApproval hardcoded four-player limit.");
        else
            OPLog.Warning("Multiplayer", "Could not find hardcoded four-player limit in GameNetworkManager.ConnectionApproval.");

        return result.AsEnumerable();
    }

    private static int GetMaxPlayersForApproval() =>
        MultiplayerFeature.GetMaxPlayersForPatch();

    private static bool LeaveLobbyAtGameStartPrefix()
    {
        if (!OPConfig.EnableMultiplayer.Value || !OPConfig.EnableLateJoin.Value)
            return true;

        OPLog.Info("Multiplayer", "Prevented vanilla lobby close at game start so late join/invites can remain available.");
        return false;
    }

    private static void CreateLobbyAsyncPrefix(ref int maxMembers)
    {
        if (!OPConfig.EnableMultiplayer.Value)
            return;

        var target = MultiplayerFeature.GetMaxPlayersForPatch();
        var before = maxMembers;
        maxMembers = target;
        OPLog.Info("Multiplayer", $"Steam lobby creation capacity patched: {before} -> {target}");
    }

    private static bool DisableInviteFriendsButtonPrefix()
    {
        if (!OPConfig.EnableMultiplayer.Value || !OPConfig.EnableLateJoin.Value)
            return true;

        return !MultiplayerFeature.IsLobbyJoinableForInvites();
    }

    private static bool InviteFriendsButtonPrefix()
    {
        if (!OPConfig.EnableMultiplayer.Value || !OPConfig.EnableLateJoin.Value)
            return true;

        if (!MultiplayerFeature.IsLobbyJoinableForInvites())
            return false;

        if (!GameNetworkManager.Instance.disableSteam)
            GameNetworkManager.Instance.InviteFriendsUI();

        return false;
    }

    private static void StartGamePrefix()
    {
        if (OPConfig.EnableMultiplayer.Value)
            MultiplayerFeature.SetLobbyJoinable(false, "round start transition");
    }

    private static void SetShipReadyToLandPostfix()
    {
        if (OPConfig.EnableMultiplayer.Value)
            MultiplayerFeature.RefreshLobbyJoinable("ship ready to land");
    }

    private static void OnShipLandedMiscEventsPostfix()
    {
        if (OPConfig.EnableMultiplayer.Value)
            MultiplayerFeature.RefreshLobbyJoinable("ship landed");
    }

    private static void OnPlayerDcPostfix()
    {
        if (OPConfig.EnableMultiplayer.Value)
            MultiplayerFeature.RefreshLobbyJoinable("player disconnected");
    }

    private static void LobbyDataIsJoinablePostfix(object lobby, ref bool __result)
    {
        try
        {
            if (!OPConfig.EnableMultiplayer.Value || __result)
                return;

            var memberCount = GetLobbyInt(lobby, "MemberCount");
            var maxPlayers = MultiplayerFeature.GetMaxPlayersForPatch();
            if (memberCount < 4 || memberCount >= maxPlayers)
                return;

            var version = InvokeLobbyString(lobby, "GetData", "vers");
            var gameVersion = GameNetworkManager.Instance?.gameVersionNum.ToString() ?? "";
            var joinable = InvokeLobbyString(lobby, "GetData", "joinable");
            if (version != gameVersion || string.Equals(joinable, "false", StringComparison.OrdinalIgnoreCase))
                return;

            __result = true;
            OPLog.Info("Multiplayer", $"Lobby browser accepted Overseer capacity lobby: members={memberCount}, max={maxPlayers}.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Lobby joinable postfix failed: {ex.Message}");
        }
    }

    private static void OnPlayerConnectedClientRpcPostfix(ulong clientId, int assignedPlayerObjectId)
    {
        try
        {
            MultiplayerFeature.PromotePendingSpectatorIfNeeded(clientId, assignedPlayerObjectId);
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Late join spectator handoff failed: {ex.Message}");
        }
    }

    private static bool ValidateGameVersion(
        GameNetworkManager manager,
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        var payload = System.Text.Encoding.ASCII.GetString(request.Payload ?? Array.Empty<byte>());
        var parts = payload.Split(',');
        var clientVersion = parts.Length == 0 ? "" : parts[0];
        var hostVersion = manager.gameVersionNum.ToString();
        if (string.IsNullOrWhiteSpace(clientVersion) || clientVersion == hostVersion)
            return true;

        response.Reason = $"Game version mismatch! Their version: {hostVersion}. Your version: {clientVersion}";
        response.CreatePlayerObject = false;
        response.Approved = false;
        response.Pending = false;
        OPLog.Warning("Multiplayer", $"Connection rejected by game version mismatch: host={hostVersion}, client={clientVersion}");
        return false;
    }

    private static int GetLobbyInt(object lobby, string propertyName)
    {
        var property = lobby.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(lobby) is int value ? value : 0;
    }

    private static string InvokeLobbyString(object lobby, string methodName, string key)
    {
        var method = lobby.GetType().GetMethod(methodName, new[] { typeof(string) });
        return method?.Invoke(lobby, new object[] { key }) as string ?? "";
    }
}
