using System;
using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using OverseerProtocol.Core.Logging;
using UnityEngine.InputSystem;

namespace OverseerProtocol.Features;

public sealed class OverseerInputActions : LcInputActions
{
    public static event Action? OpenPanelPressed;
    private static bool _inputUtilsActionReady;

    [InputAction(
        KeyboardControl.P,
        Name = "Open Overseer Panel",
        ActionId = "open_overseer_panel",
        KbmInteractions = "Press",
        GamepadControl = GamepadControl.Unbound)]
    public InputAction OpenOverseerPanel { get; set; } = null!;

    public override void OnAssetLoaded()
    {
        if (OpenOverseerPanel == null)
        {
            _inputUtilsActionReady = false;
            OPLog.Warning("Input", "InputUtils did not provide Open Overseer Panel during asset load. Falling back to Keyboard/P polling.");
            return;
        }

        OpenOverseerPanel.performed -= OnOpenPanelPerformed;
        OpenOverseerPanel.performed += OnOpenPanelPerformed;
        Enable();
        _inputUtilsActionReady = true;
        OPLog.Info("Input", "InputUtils action loaded: Open Overseer Panel.");
    }

    public void TickFallback()
    {
        if (_inputUtilsActionReady)
            return;

        var keyboard = Keyboard.current;
        if (keyboard?.pKey == null || !keyboard.pKey.wasPressedThisFrame)
            return;

        OPLog.Info("Input", "Open Overseer Panel fallback key performed.");
        OpenPanelPressed?.Invoke();
    }

    private static void OnOpenPanelPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        OPLog.Info("Input", "Open Overseer Panel action performed.");
        OpenPanelPressed?.Invoke();
    }
}
