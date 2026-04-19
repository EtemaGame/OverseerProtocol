using System;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features;

public sealed class LethalConfigBridgeFeature
{
    private const string ManagerTypeName = "LethalConfig.LethalConfigManager";
    private static bool _buttonRegistered;
    private static bool _missingLogged;

    public void RegisterDynamicConfig(ConfigFile config)
    {
        if (!OPConfig.EnableLethalConfigBridge.Value)
            return;

        var managerType = FindType(ManagerTypeName);
        if (managerType == null)
        {
            LogMissingOnce();
            return;
        }

        try
        {
            var baseItemType = FindType("LethalConfig.ConfigItems.BaseConfigItem");
            if (baseItemType == null)
            {
                OPLog.Warning("LethalConfig", "LethalConfig BaseConfigItem API was not found. Dynamic config UI registration skipped.");
                return;
            }

            var addConfigItem = FindAddConfigItemMethod(managerType, baseItemType);
            if (addConfigItem == null)
            {
                OPLog.Warning("LethalConfig", "LethalConfig AddConfigItem API was not found. Dynamic config UI registration skipped.");
                return;
            }

            RegisterReloadButton(managerType);

            var submitted = 0;
            var skipped = 0;
            foreach (var entry in config.Select(pair => pair.Value).OrderBy(entry => entry.Definition.Section).ThenBy(entry => entry.Definition.Key))
            {
                var item = CreateConfigItem(entry);
                if (item == null)
                {
                    skipped++;
                    continue;
                }

                addConfigItem.Invoke(null, new[] { item, Assembly.GetExecutingAssembly() });
                submitted++;
            }

            OPLog.Info("LethalConfig", $"Submitted {submitted} OverseerProtocol config entries to LethalConfig. Unsupported entries skipped={skipped}.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("LethalConfig", $"Could not register OverseerProtocol config entries in LethalConfig: {Unwrap(ex).Message}");
        }
    }

    private static object? CreateConfigItem(ConfigEntryBase entry)
    {
        var settingType = entry.SettingType;
        if (settingType == typeof(bool))
            return CreateValueConfigItem("LethalConfig.ConfigItems.BoolCheckBoxConfigItem", entry);

        if (settingType == typeof(int))
            return CreateValueConfigItem("LethalConfig.ConfigItems.IntInputFieldConfigItem", entry);

        if (settingType == typeof(float))
            return CreateValueConfigItem("LethalConfig.ConfigItems.FloatInputFieldConfigItem", entry);

        if (settingType == typeof(string))
            return CreateValueConfigItem("LethalConfig.ConfigItems.TextInputFieldConfigItem", entry);

        if (settingType.IsEnum)
        {
            var openType = FindType("LethalConfig.ConfigItems.EnumDropDownConfigItem`1");
            return openType == null
                ? null
                : Activator.CreateInstance(openType.MakeGenericType(settingType), entry, false);
        }

        return null;
    }

    private static object? CreateValueConfigItem(string itemTypeName, ConfigEntryBase entry)
    {
        var itemType = FindType(itemTypeName);
        return itemType == null
            ? null
            : Activator.CreateInstance(itemType, entry, false);
    }

    private static void RegisterReloadButton(Type managerType)
    {
        if (_buttonRegistered)
            return;

        var buttonType = FindType("LethalConfig.ConfigItems.GenericButtonConfigItem");
        var baseItemType = FindType("LethalConfig.ConfigItems.BaseConfigItem");
        var handlerType = FindType("LethalConfig.ConfigItems.Options.GenericButtonOptions+GenericButtonHandler");
        if (buttonType == null || baseItemType == null || handlerType == null)
            return;

        var addConfigItem = FindAddConfigItemMethod(managerType, baseItemType);

        if (addConfigItem == null)
            return;

        var handler = Delegate.CreateDelegate(
            handlerType,
            typeof(LethalConfigBridgeFeature).GetMethod(nameof(ReloadRuntimeFromButton), BindingFlags.NonPublic | BindingFlags.Static)!);

        var button = Activator.CreateInstance(
            buttonType,
            "Overseer",
            "Apply current config",
            "Reloads OverseerProtocol runtime config after editing values in LethalConfig.",
            "Apply / Reload",
            handler);

        if (button == null)
            return;

        addConfigItem.Invoke(null, new[] { button, Assembly.GetExecutingAssembly() });
        _buttonRegistered = true;
        OPLog.Info("LethalConfig", "Registered OverseerProtocol Apply / Reload button in LethalConfig.");
    }

    private static MethodInfo? FindAddConfigItemMethod(Type managerType, Type baseItemType) =>
        managerType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method =>
            {
                if (method.Name != "AddConfigItem")
                    return false;

                var parameters = method.GetParameters();
                return parameters.Length == 2 &&
                       parameters[0].ParameterType == baseItemType &&
                       parameters[1].ParameterType == typeof(Assembly);
            });

    private static void ReloadRuntimeFromButton()
    {
        RuntimeServices.Orchestrator.ReloadRuntimeConfiguration();
    }

    private static Type? FindType(string fullName) =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, false))
            .FirstOrDefault(type => type != null);

    private static void LogMissingOnce()
    {
        if (_missingLogged)
            return;

        _missingLogged = true;
        OPLog.Info("LethalConfig", "LethalConfig is not installed or not loaded. In-game config bridge skipped.");
    }

    private static Exception Unwrap(Exception exception) =>
        exception is TargetInvocationException { InnerException: not null }
            ? exception.InnerException
            : exception;
}
