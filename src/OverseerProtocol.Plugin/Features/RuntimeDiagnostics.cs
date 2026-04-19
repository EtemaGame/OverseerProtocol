using BepInEx.Configuration;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using UnityEngine;

namespace OverseerProtocol.Features;

public static class RuntimeDiagnostics
{
    public static void LogBootSummary()
    {
        if (!OPConfig.VerboseDiagnostics.Value)
            return;

        var modName = global::OverseerProtocol.Plugin.ModName;
        var modVersion = global::OverseerProtocol.Plugin.ModVersion;
        LogTestSignal("Diagnostics", "=== TEST SIGNAL: BOOT DIAGNOSTICS BEGIN ===");
        OPLog.Info("Diagnostics", $"Mod={modName}, version={modVersion}");
        OPLog.Info("Diagnostics", "Current test target: Fase 1 - build, load, exports, user tuning, snapshot, dry-run, route prices, admin hook.");
        LogPathSummary();
        LogConfigSummary();
        LogRuntimePreconditions("plugin-awake");
        LogTestSignal("Diagnostics", "=== TEST SIGNAL: BOOT DIAGNOSTICS END ===");
    }

    public static void LogPathSummary()
    {
        if (!OPConfig.VerboseDiagnostics.Value)
            return;

        OPLog.Info("Diagnostics", $"Path PluginRoot={OPPaths.PluginRoot}");
        OPLog.Info("Diagnostics", $"Path DataRoot={OPPaths.DataRoot}");
        OPLog.Info("Diagnostics", $"Path ExportRoot={OPPaths.ExportRoot}");
        OPLog.Info("Diagnostics", $"Path Saves={OPPaths.PersistenceRoot}");
        OPLog.Info("Diagnostics", $"Path Definitions={OPPaths.DefinitionsRoot}");
    }

    public static void LogConfigSummary()
    {
        OPLog.Info("Diagnostics", $"Config Advanced.ActivePreset={OPConfig.ActivePresetName}");
        OPLog.Info(
            "Diagnostics",
            "Config flags: " +
            $"EnableDataExport={Value(OPConfig.EnableDataExport)}, " +
            $"EnableItemOverrides={Value(OPConfig.EnableItemOverrides)}, " +
            $"EnableSpawnOverrides={Value(OPConfig.EnableSpawnOverrides)}, " +
            $"EnableMoonOverrides={Value(OPConfig.EnableMoonOverrides)}, " +
            $"EnableRuntimeMultipliers={Value(OPConfig.EnableRuntimeMultipliers)}, " +
            $"EnableProgressionStorage={Value(OPConfig.EnableProgressionStorage)}, " +
            $"EnablePerkCatalog={Value(OPConfig.EnablePerkCatalog)}");
        OPLog.Info(
            "Diagnostics",
            "Config safety: " +
            $"DryRunOverrides={Value(OPConfig.DryRunOverrides)}, " +
            $"StrictValidation={Value(OPConfig.StrictValidation)}, " +
            $"AbortOnInvalidOverrideBlock={Value(OPConfig.AbortOnInvalidOverrideBlock)}, " +
            $"AdminTerminalCommands=AlwaysOn, " +
            $"AdminCommandPrefix={Value(OPConfig.AdminCommandPrefix)}");
        OPLog.Info(
            "Diagnostics",
            "Config multipliers: " +
            $"ItemWeightMultiplier={Value(OPConfig.ItemWeightMultiplier)}, " +
            $"SpawnRarityMultiplier={Value(OPConfig.SpawnRarityMultiplier)}, " +
            $"RoutePriceMultiplier={Value(OPConfig.RoutePriceMultiplier)}, " +
            $"TravelDiscountMultiplier={Value(OPConfig.TravelDiscountMultiplier)}");
        OPLog.Info(
            "Diagnostics",
            "Config multiplayer: " +
            $"EnableMultiplayer={Value(OPConfig.EnableMultiplayer)}, " +
            $"MaxPlayers={Value(OPConfig.MaxPlayers)}, " +
            $"EnableLateJoin={Value(OPConfig.EnableLateJoin)}, " +
            $"LateJoinInOrbit={Value(OPConfig.LateJoinInOrbit)}, " +
            $"LateJoinOnMoonAsSpectator={Value(OPConfig.LateJoinOnMoonAsSpectator)}");
    }

    public static void LogRuntimePreconditions(string stage)
    {
        if (!OPConfig.VerboseDiagnostics.Value)
            return;

        var hasStartOfRound = StartOfRound.Instance != null;
        var levelCount = StartOfRound.Instance?.levels?.Length ?? 0;
        var itemCount = StartOfRound.Instance?.allItemsList?.itemsList?.Count ?? 0;
        var terminalNodes = Resources.FindObjectsOfTypeAll<TerminalNode>() ?? new TerminalNode[0];

        var routeNodeCount = 0;
        foreach (var node in terminalNodes)
        {
            if (node != null && node.buyRerouteToMoon >= 0)
                routeNodeCount++;
        }

        OPLog.Info(
            "Diagnostics",
            $"Runtime preconditions [{stage}]: StartOfRound={hasStartOfRound}, levels={levelCount}, items={itemCount}, terminalNodes={terminalNodes.Length}, routeNodes={routeNodeCount}");
    }

    public static void LogFeatureDecision(string featureName, bool enabled, string action)
    {
        if (OPConfig.ReduceVerboseLogs.Value && !OPConfig.VerboseDiagnostics.Value)
            return;

        OPLog.Info(
            "Diagnostics",
            $"Feature decision: {featureName}, enabled={enabled}, action={action}");
    }

    public static void LogTestSignal(string tag, string message)
    {
        if (OPConfig.VerboseDiagnostics.Value)
            OPLog.Info(tag, message);
    }

    private static string Value(ConfigEntry<bool>? entry) =>
        entry == null ? "<unbound>" : entry.Value ? "true" : "false";

    private static string Value(ConfigEntry<int>? entry) =>
        entry == null ? "<unbound>" : entry.Value.ToString();

    private static string Value(ConfigEntry<float>? entry) =>
        entry == null ? "<unbound>" : entry.Value.ToString("0.###");

    private static string Value(ConfigEntry<string>? entry) =>
        entry == null ? "<unbound>" : entry.Value;
}
