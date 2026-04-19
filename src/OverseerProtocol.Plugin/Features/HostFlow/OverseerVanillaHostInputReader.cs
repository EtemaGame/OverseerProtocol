using System;
using System.Reflection;
using HarmonyLib;
using OverseerProtocol.Core.Logging;
using TMPro;
using UnityEngine;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerVanillaHostInputReader
{
    private const int MaxLobbyNameLength = 40;

    public bool TryReadAndValidate(
        MenuManager menu,
        out HostVanillaInput? input,
        out VanillaHostInputFailureReason reason,
        out string error)
    {
        input = null;
        reason = VanillaHostInputFailureReason.None;
        error = "";

        try
        {
            if (!IsAlive(menu))
            {
                reason = VanillaHostInputFailureReason.MenuUnavailable;
                error = "MenuManager is not available.";
                return false;
            }

            var canvas = ResolveCanvasRoot(menu);
            if (!IsAlive(canvas))
            {
                reason = VanillaHostInputFailureReason.CanvasUnavailable;
                error = "Menu canvas was not found.";
                return false;
            }

            if (!IsAlive(menu.HostSettingsScreen))
            {
                reason = VanillaHostInputFailureReason.HostSettingsScreenUnavailable;
                error = "HostSettingsScreen is not available.";
                return false;
            }

            var originalLobbyName = ReadInputText(menu.lobbyNameInputField);
            var originalLobbyTag = ReadInputText(menu.lobbyTagInputField);
            var originalTipText = menu.tipTextHostSettings == null ? "" : menu.tipTextHostSettings.text ?? "";
            var lobbyName = originalLobbyName.Trim();

            if (string.IsNullOrEmpty(lobbyName))
            {
                SetTip(menu, "Enter a lobby name!");
                reason = VanillaHostInputFailureReason.EmptyLobbyName;
                error = "Lobby name is empty.";
                return false;
            }

            if (lobbyName.Length > MaxLobbyNameLength)
            {
                lobbyName = lobbyName.Substring(0, MaxLobbyNameLength);
                WriteInputText(menu.lobbyNameInputField, lobbyName);
            }

            var isPublic = menu.hostSettings_LobbyPublic;
            var lobbyTag = isPublic ? originalLobbyTag : "";
            if (!isPublic)
                WriteInputText(menu.lobbyTagInputField, "");

            input = new HostVanillaInput(
                menu,
                lobbyName,
                isPublic,
                lobbyTag,
                originalLobbyName,
                originalLobbyTag,
                originalTipText,
                menu.HostSettingsScreen,
                canvas!);
            return true;
        }
        catch (Exception ex)
        {
            reason = VanillaHostInputFailureReason.UnexpectedError;
            error = ex.Message;
            OPLog.Warning("HostFlow", "Could not read vanilla host input: " + ex);
            return false;
        }
    }

    public void SaveVanillaHostSettings(HostVanillaInput input)
    {
        try
        {
            var es3 = AccessTools.TypeByName("ES3");
            if (es3 == null)
            {
                OPLog.Warning("HostFlow", "Could not save vanilla host settings: ES3 type not found.");
                return;
            }

            TryInvokeSave(es3, "HostSettings_Name", input.LobbyName, "LCGeneralSaveData");
            TryInvokeSave(es3, "HostSettings_Public", input.IsPublic, "LCGeneralSaveData");
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Could not save vanilla host settings: " + ex.Message);
        }
    }

    public void HideHostSettings(HostVanillaInput input)
    {
        if (IsAlive(input.HostSettingsScreen))
            input.HostSettingsScreen.SetActive(false);
    }

    public void RestoreHostSettings(HostVanillaInput input)
    {
        if (IsAlive(input.MenuManager))
        {
            WriteInputText(input.MenuManager.lobbyNameInputField, input.OriginalLobbyName);
            WriteInputText(input.MenuManager.lobbyTagInputField, input.OriginalLobbyTag);
            if (input.MenuManager.tipTextHostSettings != null)
                input.MenuManager.tipTextHostSettings.text = input.OriginalTipText;
        }

        if (IsAlive(input.HostSettingsScreen))
            input.HostSettingsScreen.SetActive(true);
    }

    private static Transform? ResolveCanvasRoot(MenuManager menu)
    {
        var canvasType = FindCanvasType();
        var canvas = menu.menuButtons == null || canvasType == null
            ? null
            : menu.menuButtons.GetComponentInParent(canvasType) as Component;
        if (canvas != null)
            return canvas.transform;

        canvas = canvasType == null ? null : UnityEngine.Object.FindObjectOfType(canvasType) as Component;
        return canvas == null ? null : canvas.transform;
    }

    private static string ReadInputText(TMP_InputField? input) =>
        input == null ? "" : input.text ?? "";

    private static void WriteInputText(TMP_InputField? input, string value)
    {
        if (input != null)
            input.text = value ?? "";
    }

    private static void SetTip(MenuManager menu, string message)
    {
        if (menu.tipTextHostSettings != null)
            menu.tipTextHostSettings.text = message;
    }

    private static Type? FindCanvasType() =>
        AccessTools.TypeByName("UnityEngine.Canvas");

    private static void TryInvokeSave(Type es3, string key, object value, string file)
    {
        try
        {
            foreach (var method in es3.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != "Save")
                    continue;

                var parameters = method.GetParameters();
                if (parameters.Length != 3)
                    continue;

                if (parameters[0].ParameterType != typeof(string) || parameters[2].ParameterType != typeof(string))
                    continue;

                var generic = method.IsGenericMethodDefinition ? method.MakeGenericMethod(value.GetType()) : method;
                generic.Invoke(null, new[] { key, value, file });
                return;
            }

            OPLog.Warning("HostFlow", "Could not save vanilla host setting '" + key + "': ES3.Save overload not found.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Could not save vanilla host setting '" + key + "': " + Unwrap(ex).Message);
        }
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;

    private static bool IsAlive(UnityEngine.Object? obj) => obj != null;
}
