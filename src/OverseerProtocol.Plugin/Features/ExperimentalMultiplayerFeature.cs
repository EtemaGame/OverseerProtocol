using System;
using System.Reflection;
using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Lobby;
using OverseerProtocol.Data.Models.Sync;

namespace OverseerProtocol.Features;

public sealed class ExperimentalMultiplayerFeature
{
    private static readonly string[] MaxPlayerMemberNames =
    {
        "maxPlayers",
        "maxAllowedPlayers",
        "maxLobbyPlayers",
        "MaxPlayers",
        "MaxConnectedPlayers"
    };

    public void Apply()
    {
        if (!OPConfig.EnableExperimentalMultiplayer.Value)
        {
            OPLog.Info("Multiplayer", "Experimental multiplayer disabled by config.");
            return;
        }

        var rules = new LobbyRulesFeature().LoadOrCreate();
        LogRules(rules);

        if (OPConfig.EnableExpandedLobbyPatch.Value && rules.EnableExpandedLobby)
            TryApplyExpandedLobby(rules);
        else
            OPLog.Info("Multiplayer", "Expanded lobby patch disabled or not enabled by lobby rules.");

        if (OPConfig.EnableLateJoinSafeMode.Value && rules.AllowLateJoin)
            EvaluateLateJoinPolicy(rules);

        if (OPConfig.EnableSpectatorModeScaffold.Value && rules.EnableSpectatorMode)
            OPLog.Warning("Multiplayer", "Spectator mode scaffold is enabled, but runtime spectator control is reserved until player lifecycle hooks are verified.");
    }

    public ProtocolHandshakeDefinition BuildHandshake() =>
        new HandshakeFeature().BuildCurrent();

    public RuntimeStateSyncSnapshotDefinition BuildReservedSyncSnapshot()
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
                    Kind = "Reserved",
                    State = "Ship object sync is not active until runtime hooks are verified.",
                    Source = "ExperimentalMultiplayerFeature"
                }
            },
            MoonState =
            {
                new SyncStateEntry
                {
                    Id = "moon-state",
                    Kind = "Reserved",
                    State = "Moon state sync is not active until runtime hooks are verified.",
                    Source = "ExperimentalMultiplayerFeature"
                }
            }
        };
    }

    public string GetStatusText()
    {
        var rules = new LobbyRulesFeature().LoadOrCreate();
        return "Experimental multiplayer: " + OPConfig.EnableExperimentalMultiplayer.Value + "\n" +
               "Expanded lobby patch: " + OPConfig.EnableExpandedLobbyPatch.Value + "\n" +
               "Late join safe mode: " + OPConfig.EnableLateJoinSafeMode.Value + "\n" +
               "Spectator scaffold: " + OPConfig.EnableSpectatorModeScaffold.Value + "\n" +
               "Rules max players: " + rules.MaxPlayers + "\n" +
               "Config max players: " + OPConfig.ExperimentalMaxPlayers.Value + "\n" +
               "Late join mode: " + rules.LateJoinMode;
    }

    private static void LogRules(LobbyRulesDefinition rules)
    {
        OPLog.Info(
            "Multiplayer",
            $"Rules loaded: expanded={rules.EnableExpandedLobby}, maxPlayers={rules.MaxPlayers}, lateJoin={rules.AllowLateJoin}, mode={rules.LateJoinMode}, spectator={rules.EnableSpectatorMode}");
    }

    private static void TryApplyExpandedLobby(LobbyRulesDefinition rules)
    {
        var targetMaxPlayers = Math.Max(1, Math.Min(OPConfig.ExperimentalMaxPlayers.Value, rules.MaxPlayers));
        var patchedMembers = 0;

        patchedMembers += TryPatchSingletonInt("GameNetworkManager", "Instance", targetMaxPlayers);
        patchedMembers += TryPatchSingletonInt("StartOfRound", "Instance", targetMaxPlayers);

        if (patchedMembers == 0)
        {
            OPLog.Warning(
                "Multiplayer",
                "Expanded lobby reflection patch did not find known max-player fields/properties. Runtime research is still required.");
            return;
        }

        OPLog.Warning(
            "Multiplayer",
            $"Experimental expanded lobby patched {patchedMembers} members to maxPlayers={targetMaxPlayers}. This does not guarantee UI, ownership, or late-join safety.");
    }

    private static int TryPatchSingletonInt(string typeName, string instanceMemberName, int value)
    {
        var type = AccessTools.TypeByName(typeName);
        if (type == null)
            return 0;

        var instance = GetStaticMemberValue(type, instanceMemberName);
        if (instance == null)
            return 0;

        var patched = 0;
        foreach (var memberName in MaxPlayerMemberNames)
        {
            patched += TrySetIntField(instance, memberName, value);
            patched += TrySetIntProperty(instance, memberName, value);
        }

        return patched;
    }

    private static object? GetStaticMemberValue(Type type, string memberName)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        var field = type.GetField(memberName, flags);
        if (field != null)
            return field.GetValue(null);

        var property = type.GetProperty(memberName, flags);
        return property?.GetValue(null, null);
    }

    private static int TrySetIntField(object instance, string fieldName, int value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null || field.FieldType != typeof(int))
            return 0;

        field.SetValue(instance, value);
        return 1;
    }

    private static int TrySetIntProperty(object instance, string propertyName, int value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property == null || property.PropertyType != typeof(int) || !property.CanWrite)
            return 0;

        property.SetValue(instance, value, null);
        return 1;
    }

    private static void EvaluateLateJoinPolicy(LobbyRulesDefinition rules)
    {
        var mode = string.IsNullOrWhiteSpace(rules.LateJoinMode) ? "Disabled" : rules.LateJoinMode.Trim();
        if (string.Equals(mode, "Moon", StringComparison.OrdinalIgnoreCase))
        {
            OPLog.Warning("Multiplayer", "LateJoinMode=Moon is blocked by policy until moon state recovery is implemented.");
            return;
        }

        OPLog.Warning(
            "Multiplayer",
            $"Late join safe mode '{mode}' is enabled for diagnostics only. Connection approval/state recovery hooks are not active yet.");
    }
}
