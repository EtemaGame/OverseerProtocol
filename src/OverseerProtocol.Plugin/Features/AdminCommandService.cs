using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features;

public sealed class AdminCommandService
{
    public AdminCommandResult Execute(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            OPLog.Info("Admin", "Terminal input was empty. Passing through to vanilla.");
            return AdminCommandResult.NotHandled();
        }

        var normalized = input.Trim();
        var prefix = OPConfig.AdminCommandPrefix?.Value;
        if (string.IsNullOrWhiteSpace(prefix))
            prefix = "op";

        prefix = prefix.Trim();

        if (!normalized.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, prefix, StringComparison.OrdinalIgnoreCase))
        {
            OPLog.Info("Admin", $"Terminal input is not an OverseerProtocol command. prefix={prefix}, input='{normalized}'. Passing through to vanilla.");
            return AdminCommandResult.NotHandled();
        }

        var command = normalized.Length == prefix.Length
            ? "help"
            : normalized.Substring(prefix.Length + 1).Trim().ToLowerInvariant();

        OPLog.Info("Admin", $"OverseerProtocol admin command parsed: raw='{normalized}', command='{command}'");

        switch (command)
        {
            case "":
            case "help":
                return AdminCommandResult.FromHandled(GetHelpText());
            case "reload":
                RuntimeServices.Orchestrator.ReloadRuntimeConfiguration();
                return AdminCommandResult.FromHandled("OverseerProtocol config reloaded.");
            default:
                return AdminCommandResult.FromHandled($"Unknown OverseerProtocol command '{command}'. Try: op help");
        }
    }

    private static string GetHelpText() =>
        "OverseerProtocol commands: op help, op reload\n" +
        "Open the in-game Overseer panel with the 'Open Overseer Panel' keybind in the controls menu.";
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
