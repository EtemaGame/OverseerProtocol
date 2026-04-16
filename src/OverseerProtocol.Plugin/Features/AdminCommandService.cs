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
                DataExportFeature.RunInitialExport();
                return AdminCommandResult.FromHandled("Export completed. Check OverseerProtocol logs for catalog counts.");
            case "validate":
                return AdminCommandResult.FromHandled("Validation runs during startup/reload. Enable DryRunOverrides=true to validate without runtime mutations.");
            default:
                return AdminCommandResult.FromHandled($"Unknown OverseerProtocol command '{command}'. Try: op help");
        }
    }

    private static string GetHelpText() =>
        "OverseerProtocol commands: op help, op preset, op paths, op export, op validate";

    private static string GetPathsText() =>
        "DataRoot: " + OPPaths.DataRoot + "\n" +
        "Exports: " + OPPaths.ExportRoot + "\n" +
        "Overrides: " + OPPaths.OverridesRoot + "\n" +
        "Presets: " + OPPaths.PresetsRoot + "\n" +
        "Saves: " + OPPaths.PersistenceRoot;
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
