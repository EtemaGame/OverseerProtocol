using System;
using System.Reflection;
using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;
using UnityEngine;

namespace OverseerProtocol.Patches;

internal static class LobbyHostMenuHook
{
    private static object? _menuManager;
    private static string _lobbyName = "";
    private static bool _lobbyPublic;

    public static void TryPatch(Harmony harmony)
    {
        var type = AccessTools.TypeByName("MenuManager");
        if (type == null)
        {
            OPLog.Warning("LobbyMenu", "Cannot patch MenuManager.ConfirmHostButton: MenuManager type not found.");
            return;
        }

        var confirmHost = AccessTools.Method(type, "ConfirmHostButton");
        if (confirmHost == null)
        {
            OPLog.Warning("LobbyMenu", "Cannot patch MenuManager.ConfirmHostButton: method not found.");
            return;
        }

        var start = AccessTools.Method(type, "Start");

        try
        {
            if (start != null)
                harmony.Patch(start, postfix: new HarmonyMethod(typeof(LobbyHostMenuHook), nameof(MenuManagerStartPostfix)));
            else
                OPLog.Warning("LobbyMenu", "MenuManager.Start was not found. Host menu will be prepared lazily.");

            harmony.Patch(confirmHost, prefix: new HarmonyMethod(typeof(LobbyHostMenuHook), nameof(ConfirmHostButtonPrefix)));
            OPLog.Info("LobbyMenu", "Patched MenuManager.ConfirmHostButton for Overseer host menu.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("LobbyMenu", "Failed to patch MenuManager.ConfirmHostButton: " + ex.Message);
        }
    }

    private static void MenuManagerStartPostfix(object __instance)
    {
        try
        {
            var parent = ResolveCanvasParent(__instance);
            if (parent == null)
            {
                OPLog.Warning("LobbyMenu", "Could not prepare Overseer host menu: menu canvas not found.");
                return;
            }

            LobbyHostMenuFeature.Prepare(parent);
        }
        catch (Exception ex)
        {
            OPLog.Warning("LobbyMenu", "Could not prepare Overseer host menu on MenuManager.Start: " + ex.Message);
        }
    }

    private static bool ConfirmHostButtonPrefix(object __instance)
    {
        try
        {
            if (!OPConfig.ShowLobbyHostMenu.Value)
                return true;

            _menuManager = __instance;
            _lobbyName = ReadInputText(__instance, "lobbyNameInputField");
            _lobbyPublic = ReadBool(__instance, "hostSettings_LobbyPublic");

            if (string.IsNullOrWhiteSpace(_lobbyName))
            {
                SetText(__instance, "tipTextHostSettings", "Enter a lobby name!");
                return false;
            }

            if (_lobbyName.Length > 40)
                _lobbyName = _lobbyName.Substring(0, 40);

            WriteInputText(__instance, "lobbyNameInputField", _lobbyName);
            TrySaveHostSettings(_lobbyName, _lobbyPublic);

            var hostSettings = GetGameObject(__instance, "HostSettingsScreen");
            if (hostSettings != null)
                hostSettings.SetActive(false);

            var parent = ResolveCanvasParent(__instance);
            if (parent == null)
            {
                OPLog.Warning("LobbyMenu", "Could not find a menu canvas. Falling back to vanilla host flow.");
                return true;
            }

            LobbyHostMenuFeature.Open(
                parent,
                StartHostFromMenu,
                () =>
                {
                    if (hostSettings != null)
                        hostSettings.SetActive(true);
                });

            return false;
        }
        catch (Exception ex)
        {
            OPLog.Warning("LobbyMenu", "Host menu prefix failed. Falling back to vanilla host flow. " + ex.Message);
            return true;
        }
    }

    private static void StartHostFromMenu()
    {
        try
        {
            SetHostSettings(_lobbyName, _lobbyPublic);
            var manager = GameNetworkManager.Instance;
            if (manager == null)
            {
                OPLog.Warning("LobbyMenu", "GameNetworkManager.Instance was null; host start aborted.");
                return;
            }

            manager.StartHost();
            OPLog.Info("LobbyMenu", "Started host from Overseer menu.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("LobbyMenu", "Could not start host from Overseer menu: " + ex);
        }
    }

    private static Transform? ResolveCanvasParent(object menuManager)
    {
        var canvasType = AccessTools.TypeByName("UnityEngine.Canvas");
        var menuButtons = GetGameObject(menuManager, "menuButtons");
        var canvas = menuButtons == null || canvasType == null ? null : menuButtons.GetComponentInParent(canvasType) as Component;
        if (canvas != null)
            return canvas.transform;

        canvas = canvasType == null ? null : UnityEngine.Object.FindObjectOfType(canvasType) as Component;
        return canvas == null ? null : canvas.transform;
    }

    private static void SetHostSettings(string lobbyName, bool isPublic)
    {
        var manager = GameNetworkManager.Instance;
        if (manager == null)
            return;

        var hostSettingsType = AccessTools.TypeByName("HostSettings");
        if (hostSettingsType == null)
        {
            OPLog.Warning("LobbyMenu", "HostSettings type was not found. GameNetworkManager.lobbyHostSettings was not updated.");
            return;
        }

        var settings = Activator.CreateInstance(hostSettingsType, lobbyName, isPublic);
        var field = typeof(GameNetworkManager).GetField("lobbyHostSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(manager, settings);
            return;
        }

        var property = typeof(GameNetworkManager).GetProperty("lobbyHostSettings", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        property?.SetValue(manager, settings, null);
    }

    private static void TrySaveHostSettings(string lobbyName, bool isPublic)
    {
        var es3 = AccessTools.TypeByName("ES3");
        if (es3 == null)
            return;

        TryInvokeSave(es3, "HostSettings_Name", lobbyName, "LCGeneralSaveData");
        TryInvokeSave(es3, "HostSettings_Public", isPublic, "LCGeneralSaveData");
    }

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
        }
        catch (Exception ex)
        {
            OPLog.Warning("LobbyMenu", "Could not save vanilla host setting '" + key + "': " + Unwrap(ex).Message);
        }
    }

    private static string ReadInputText(object instance, string memberName)
    {
        var input = GetMemberValue(instance, memberName);
        if (input == null)
            return "";

        return input.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance)?.GetValue(input, null) as string ?? "";
    }

    private static void WriteInputText(object instance, string memberName, string text)
    {
        var input = GetMemberValue(instance, memberName);
        input?.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance)?.SetValue(input, text, null);
    }

    private static bool ReadBool(object instance, string memberName)
    {
        var value = GetMemberValue(instance, memberName);
        return value is bool boolValue && boolValue;
    }

    private static void SetText(object instance, string memberName, string text)
    {
        var label = GetMemberValue(instance, memberName);
        label?.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance)?.SetValue(label, text, null);
    }

    private static GameObject? GetGameObject(object instance, string memberName)
    {
        var value = GetMemberValue(instance, memberName);
        return value switch
        {
            GameObject gameObject => gameObject,
            Component component => component.gameObject,
            _ => null
        };
    }

    private static object? GetMemberValue(object instance, string memberName)
    {
        var type = instance.GetType();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var field = type.GetField(memberName, flags);
        if (field != null)
            return field.GetValue(instance);

        var property = type.GetProperty(memberName, flags);
        return property?.GetValue(instance, null);
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;
}
