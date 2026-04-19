using System;
using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features.HostFlow;

namespace OverseerProtocol.Patches;

internal static class OverseerHostEntryPatch
{
    private static readonly OverseerVanillaHostInputReader InputReader = new();
    private static readonly OverseerHostScreenBootstrap Bootstrap = new();
    private static readonly OverseerHostSessionController Controller = new(
        Bootstrap,
        InputReader,
        new OverseerHostReadModelFactory(),
        new OverseerEffectiveConfigBuilder(),
        new OverseerHostPlanValidator(),
        new OverseerRuntimeApplyService(),
        new OverseerMultiplayerApplyService(),
        new OverseerHostStartAdapter());

    public static void TryPatch(Harmony harmony)
    {
        var type = AccessTools.TypeByName("MenuManager");
        if (type == null)
        {
            OPLog.Warning("HostFlow", "Cannot patch MenuManager: type not found.");
            return;
        }

        var start = AccessTools.Method(type, "Start");
        var confirmHost = AccessTools.Method(type, "ConfirmHostButton");
        if (confirmHost == null)
        {
            OPLog.Warning("HostFlow", "Cannot patch MenuManager.ConfirmHostButton: method not found.");
            return;
        }

        try
        {
            if (start != null)
                harmony.Patch(start, postfix: new HarmonyMethod(typeof(OverseerHostEntryPatch), nameof(MenuManagerStartPostfix)));
            else
                OPLog.Warning("HostFlow", "MenuManager.Start not found; host screen will prepare lazily.");

            harmony.Patch(confirmHost, prefix: new HarmonyMethod(typeof(OverseerHostEntryPatch), nameof(ConfirmHostButtonPrefix)));
            OPLog.Info("HostFlow", "Patched MenuManager.ConfirmHostButton for Overseer host flow.");
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "Failed to patch MenuManager host flow: " + ex.Message);
        }
    }

    private static void MenuManagerStartPostfix(MenuManager __instance)
    {
        try
        {
            if (!OPConfig.ShowLobbyHostMenu.Value)
                return;

            if (!Bootstrap.TryPrepare(__instance, out _, out var error))
                OPLog.Warning("HostFlow", "Overseer host screen was not prepared on MenuManager.Start: " + error);
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "MenuManager.Start host-flow preparation failed: " + ex.Message);
        }
    }

    private static bool ConfirmHostButtonPrefix(MenuManager __instance)
    {
        try
        {
            if (!OPConfig.ShowLobbyHostMenu.Value)
                return true;

            if (Controller.State != HostFlowState.Idle)
            {
                OPLog.Warning("HostFlow", "Host flow is already active; passing through vanilla.");
                return true;
            }

            if (!Bootstrap.TryPrepare(__instance, out _, out var bootstrapError))
            {
                OPLog.Warning("HostFlow", "Overseer host UI unavailable; using vanilla host flow: " + bootstrapError);
                return true;
            }

            if (!InputReader.TryReadAndValidate(__instance, out var input, out var reason, out var inputError))
            {
                if (reason == VanillaHostInputFailureReason.EmptyLobbyName)
                    return false;

                OPLog.Warning("HostFlow", "Could not read vanilla host input; using vanilla host flow: " + inputError);
                return true;
            }

            if (input == null)
            {
                OPLog.Warning("HostFlow", "Vanilla host input reader returned no input; using vanilla host flow.");
                return true;
            }

            InputReader.SaveVanillaHostSettings(input);
            InputReader.HideHostSettings(input);

            if (!Controller.TryBegin(input, out var beginError))
            {
                OPLog.Warning("HostFlow", "Could not begin Overseer host flow; using vanilla host flow: " + beginError);
                InputReader.RestoreHostSettings(input);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            OPLog.Warning("HostFlow", "ConfirmHostButton prefix failed; using vanilla host flow. " + ex);
            return true;
        }
    }
}
