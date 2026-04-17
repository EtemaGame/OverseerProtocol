using System;
using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;

namespace OverseerProtocol.Patches;

internal static class ExperimentalMultiplayerHook
{
    public static void TryPatch(Harmony harmony)
    {
        if (!OPConfig.EnableExperimentalMultiplayer.Value)
        {
            OPLog.Info("Multiplayer", "Experimental multiplayer hooks disabled by config.");
            return;
        }

        PatchIfPresent(harmony, "GameNetworkManager", "Start");
        PatchIfPresent(harmony, "GameNetworkManager", "Awake");
        PatchIfPresent(harmony, "StartOfRound", "Start");
        PatchIfPresent(harmony, "StartOfRound", "OnClientConnect");
        PatchIfPresent(harmony, "StartOfRound", "OnPlayerConnectedClientRpc");
    }

    private static void PatchIfPresent(Harmony harmony, string typeName, string methodName)
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
            var postfix = new HarmonyMethod(typeof(ExperimentalMultiplayerHook), nameof(ApplyPostfix));
            harmony.Patch(method, postfix: postfix);
            OPLog.Info("Multiplayer", $"Patched experimental multiplayer postfix on {typeName}.{methodName}.");
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
            if (!OPConfig.EnableExperimentalMultiplayer.Value)
                return;

            new ExperimentalMultiplayerFeature().Apply();
        }
        catch (Exception ex)
        {
            OPLog.Warning("Multiplayer", $"Experimental multiplayer postfix failed: {ex.Message}");
        }
    }
}
