using System;
using System.Reflection;
using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;
using UnityEngine;

namespace OverseerProtocol.Patches;

internal static class TerminalAdminCommandHook
{
    private static readonly AdminCommandService CommandService = new();

    public static void TryPatch(Harmony harmony)
    {
        if (!OPConfig.EnableAdminTerminalCommands.Value)
        {
            OPLog.Info("Admin", "Admin terminal commands disabled by config.");
            return;
        }

        var target = AccessTools.Method(typeof(Terminal), "ParsePlayerSentence");
        if (target == null)
        {
            OPLog.Warning("Admin", "Terminal.ParsePlayerSentence was not found. Admin terminal commands will not be patched.");
            return;
        }

        var prefix = new HarmonyMethod(typeof(TerminalAdminCommandHook), nameof(ParsePlayerSentencePrefix));
        harmony.Patch(target, prefix);
        OPLog.Info("Admin", "Admin terminal command hook patched on Terminal.ParsePlayerSentence.");
    }

    private static bool ParsePlayerSentencePrefix(Terminal __instance, ref TerminalNode __result)
    {
        try
        {
            if (!OPConfig.EnableAdminTerminalCommands.Value)
                return true;

            var input = TryReadTerminalText(__instance);
            var result = CommandService.Execute(input);
            if (!result.Handled)
                return true;

            __result = CreateResponseNode(result.Message);
            OPLog.Info("Admin", $"Handled admin terminal command: {input}");
            return false;
        }
        catch (Exception ex)
        {
            OPLog.Warning("Admin", $"Admin terminal command hook failed. Falling back to vanilla terminal flow. {ex.Message}");
            return true;
        }
    }

    private static string TryReadTerminalText(Terminal terminal)
    {
        var screenTextField = AccessTools.Field(typeof(Terminal), "screenText");
        var screenText = screenTextField?.GetValue(terminal);
        if (screenText == null)
            return "";

        var textProperty = screenText.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty?.GetValue(screenText) is string textFromProperty)
            return textFromProperty;

        var textField = screenText.GetType().GetField("text", BindingFlags.Instance | BindingFlags.Public);
        if (textField?.GetValue(screenText) is string textFromField)
            return textFromField;

        return "";
    }

    private static TerminalNode CreateResponseNode(string message)
    {
        var node = ScriptableObject.CreateInstance<TerminalNode>();
        node.displayText = message + "\n";
        node.clearPreviousText = true;
        return node;
    }
}
