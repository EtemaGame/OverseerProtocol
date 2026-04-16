using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Paths;

namespace OverseerProtocol.Features;

public sealed class AdminCommandService
{
    public AdminCommandResult Execute(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return AdminCommandResult.NotHandled();

        var normalized = input.Trim();
        if (!normalized.StartsWith("op ", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, "op", StringComparison.OrdinalIgnoreCase))
        {
            return AdminCommandResult.NotHandled();
        }

        var command = normalized.Length == 2
            ? "help"
            : normalized.Substring(3).Trim().ToLowerInvariant();

        switch (command)
        {
            case "":
            case "help":
                return AdminCommandResult.FromHandled(GetHelpText());
            case "preset":
                return AdminCommandResult.FromHandled($"Active preset: {OPConfig.ActivePresetName}");
            case "paths":
                return AdminCommandResult.FromHandled(GetPathsText());
            case "export":
                RuntimeServices.Orchestrator.ExportVanillaCatalogs();
                return AdminCommandResult.FromHandled("Export completed. Check OverseerProtocol logs for catalog counts.");
            case "reload":
                RuntimeServices.Orchestrator.ReloadRuntimeConfiguration();
                return AdminCommandResult.FromHandled("Runtime configuration reloaded from the captured vanilla snapshot.");
            case "reset":
                RuntimeServices.Orchestrator.ResetToSnapshot();
                return AdminCommandResult.FromHandled("Runtime state restored from the captured vanilla snapshot.");
            case "fingerprint":
                return AdminCommandResult.FromHandled(GetFingerprintText());
            case "rules":
                return AdminCommandResult.FromHandled(GetRulesText());
            case "perks":
                return AdminCommandResult.FromHandled(GetPerksText());
            case "handshake":
                return AdminCommandResult.FromHandled(GetHandshakeText());
            case "validate":
                return AdminCommandResult.FromHandled("Validation runs during startup/reload. Enable DryRunOverrides=true to validate without runtime mutations.");
            default:
                return AdminCommandResult.FromHandled($"Unknown OverseerProtocol command '{command}'. Try: op help");
        }
    }

    private static string GetHelpText() =>
        "OverseerProtocol commands: op help, op preset, op paths, op export, op reload, op reset, op fingerprint, op rules, op perks, op handshake, op validate";

    private static string GetPathsText() =>
        "DataRoot: " + OPPaths.DataRoot + "\n" +
        "Exports: " + OPPaths.ExportRoot + "\n" +
        "Overrides: " + OPPaths.OverridesRoot + "\n" +
        "Presets: " + OPPaths.PresetsRoot + "\n" +
        "Saves: " + OPPaths.PersistenceRoot + "\n" +
        "Rules: " + OPPaths.RulesRoot;

    private static string GetFingerprintText()
    {
        var fingerprints = new FingerprintFeature().ComputeCurrent();
        return "Active preset: " + fingerprints.ActivePreset + "\n" +
               "Preset fingerprint: " + fingerprints.PresetFingerprint + "\n" +
               "Config fingerprint: " + fingerprints.ConfigFingerprint;
    }

    private static string GetRulesText()
    {
        var lobbyRules = new LobbyRulesFeature().LoadOrCreate();
        var runtimeRules = new RuntimeRulesFeature().LoadOrCreate();

        return "Lobby max players: " + lobbyRules.MaxPlayers + "\n" +
               "Late join: " + lobbyRules.AllowLateJoin + " (" + lobbyRules.LateJoinMode + ")\n" +
               "Quota multiplier: " + runtimeRules.Economy.QuotaMultiplier.ToString("0.###") + "\n" +
               "Deadline multiplier: " + runtimeRules.Economy.DeadlineMultiplier.ToString("0.###") + "\n" +
               "Moon rule count: " + runtimeRules.MoonRules.Count;
    }

    private static string GetHandshakeText()
    {
        var handshake = new HandshakeFeature().BuildCurrent();
        return "Overseer: " + handshake.OverseerVersion + "\n" +
               "Preset: " + handshake.ActivePreset + "\n" +
               "Preset fingerprint: " + handshake.PresetFingerprint + "\n" +
               "Config fingerprint: " + handshake.ConfigFingerprint + "\n" +
               "Max players: " + handshake.Rules.MaxPlayers + "\n" +
               "Late join: " + handshake.Rules.AllowLateJoin + " (" + handshake.Rules.LateJoinMode + ")";
    }

    private static string GetPerksText()
    {
        var catalog = new PerkCatalogFeature().LoadOrCreate();
        var progression = new ProgressionStore().LoadOrCreate();

        return "Player perks: " + catalog.PlayerPerks.Count + "\n" +
               "Ship perks: " + catalog.ShipPerks.Count + "\n" +
               "Saved players: " + progression.Players.Count + "\n" +
               "Ship level: " + progression.Ship.Level;
    }
}

public sealed class AdminCommandResult
{
    private AdminCommandResult(bool handled, string message)
    {
        Handled = handled;
        Message = message;
    }

    public bool Handled { get; }
    public string Message { get; }

    public static AdminCommandResult FromHandled(string message) =>
        new(true, message);

    public static AdminCommandResult NotHandled() =>
        new(false, "");
}
