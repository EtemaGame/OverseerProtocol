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
            var input = TryReadSubmittedTerminalText(__instance);
            OPLog.Info("Admin", $"Terminal hook captured input: '{input}'");
            var result = CommandService.Execute(input);
            if (!result.Handled)
            {
                OPLog.Info("Admin", "Terminal hook did not handle input. Vanilla terminal flow continues.");
                return true;
            }

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

    private static string TryReadSubmittedTerminalText(Terminal terminal)
    {
        var fullText = TryReadTerminalScreenText(terminal);
        var textAddedField = AccessTools.Field(typeof(Terminal), "textAdded");
        if (textAddedField?.GetValue(terminal) is int textAdded &&
            textAdded > 0 &&
            textAdded <= fullText.Length)
        {
            OPLog.Info("Admin", $"Read terminal input from textAdded span. textAdded={textAdded}.");
            return SanitizeSubmittedInput(fullText.Substring(fullText.Length - textAdded));
        }

        OPLog.Info("Admin", "Terminal textAdded was unavailable or out of range. Falling back to last visible terminal line.");
        return ExtractLastVisibleInputLine(fullText);
    }

    private static string TryReadTerminalScreenText(Terminal terminal)
    {
        var screenTextField = AccessTools.Field(typeof(Terminal), "screenText");
        var screenText = screenTextField?.GetValue(terminal);
        if (screenText == null)
        {
            OPLog.Warning("Admin", "Could not read Terminal.screenText. Returning empty input.");
            return "";
        }

        var textProperty = screenText.GetType().GetProperty("text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty?.GetValue(screenText) is string textFromProperty)
        {
            OPLog.Info("Admin", "Read terminal input from screenText.text property.");
            return textFromProperty;
        }

        var textField = screenText.GetType().GetField("text", BindingFlags.Instance | BindingFlags.Public);
        if (textField?.GetValue(screenText) is string textFromField)
        {
            OPLog.Info("Admin", "Read terminal input from screenText.text field.");
            return textFromField;
        }

        OPLog.Warning("Admin", "Terminal screenText has no readable text property/field. Returning empty input.");
        return "";
    }

    private static string ExtractLastVisibleInputLine(string fullText)
    {
        var normalized = (fullText ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');
        for (var index = lines.Length - 1; index >= 0; index--)
        {
            var candidate = SanitizeSubmittedInput(lines[index]);
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        return "";
    }

    private static string SanitizeSubmittedInput(string input)
    {
        var trimmed = (input ?? "").Trim();
        if (trimmed.StartsWith(">", StringComparison.Ordinal))
            trimmed = trimmed.Substring(1).TrimStart();

        return trimmed;
    }

    private static TerminalNode CreateResponseNode(string message)
    {
        var node = ScriptableObject.CreateInstance<TerminalNode>();
        node.displayText = message + "\n";
        node.clearPreviousText = true;
        OPLog.Info("Admin", $"Created Terminal response node with {message.Length} chars.");
        return node;
    }
}
