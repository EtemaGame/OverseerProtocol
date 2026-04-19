using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OverseerProtocol.Features.HostFlow.AdvancedCompanyPort;
using OverseerProtocol.Core.Logging;
using UnityEngine;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerHostScreenBootstrap
{
    private readonly AdvancedCompanyAssetLoader _assetLoader = new();
    private readonly Dictionary<int, ScreenHandle> _screens = new();

    public bool TryPrepare(MenuManager menu, out IOverseerHostScreen? screen, out string error)
    {
        screen = null;
        error = "";
        InvalidateDeadReferences();

        if (!IsAlive(menu))
        {
            error = "MenuManager is not alive.";
            return false;
        }

        var menuId = menu.GetInstanceID();
        if (_screens.TryGetValue(menuId, out var existing) && existing.PrepareFailed)
        {
            error = existing.LastError;
            return false;
        }

        var canvas = ResolveCanvasRoot(menu);
        if (!IsAlive(canvas))
        {
            RememberFailure(menuId, menu, "Menu canvas not found.");
            error = "Menu canvas not found.";
            return false;
        }

        if (existing != null &&
            IsAlive(existing.Menu) &&
            IsAlive(existing.CanvasRoot) &&
            IsAlive(existing.ScreenObject) &&
            existing.CanvasRoot == canvas)
        {
            screen = existing.Screen;
            return true;
        }

        DisposeForMenu(menu);
        if (!TryCreateScreen(menu, canvas!, out screen, out error))
        {
            RememberFailure(menuId, menu, error);
            return false;
        }

        var screenObject = screen as UnityEngine.Object;
        if (!IsAlive(screenObject))
        {
            error = "Created screen is not a Unity object.";
            RememberFailure(menuId, menu, error);
            return false;
        }

        _screens[menuId] = new ScreenHandle
        {
            Menu = menu,
            CanvasRoot = canvas!,
            Screen = screen!,
            ScreenObject = screenObject!
        };

        return true;
    }

    public void DisposeForMenu(MenuManager menu)
    {
        if (!IsAlive(menu))
            return;

        var menuId = menu.GetInstanceID();
        if (!_screens.TryGetValue(menuId, out var handle))
            return;

        if (handle.ScreenObject is Component component && IsAlive(component.gameObject))
            UnityEngine.Object.Destroy(component.gameObject);

        _screens.Remove(menuId);
    }

    public void InvalidateDeadReferences()
    {
        foreach (var key in _screens.Keys.ToList())
        {
            var handle = _screens[key];
            if (handle.PrepareFailed)
                continue;

            if (!IsAlive(handle.Menu) || !IsAlive(handle.CanvasRoot) || !IsAlive(handle.ScreenObject))
                _screens.Remove(key);
        }
    }

    private bool TryCreateScreen(MenuManager menu, Transform canvas, out IOverseerHostScreen? screen, out string error)
    {
        screen = null;
        error = "";

        try
        {
            if (!_assetLoader.TryLoadLobbyScreenPrefab(out var prefab, out error))
                return false;

            var root = new GameObject("OverseerAdvancedCompanyHostScreen");
            root.transform.SetParent(canvas, false);
            var adapter = root.AddComponent<AdvancedCompanyHostScreenAdapter>();
            adapter.Initialize(prefab);
            screen = adapter;
            OPLog.Info("HostFlow", "Prepared AdvancedCompany host screen port.");
            return true;
        }
        catch (Exception ex)
        {
            error = "Could not create AdvancedCompany host screen: " + ex.Message;
            OPLog.Warning("HostFlow", error);
            return false;
        }
    }

    private static Transform? ResolveCanvasRoot(MenuManager menu)
    {
        var canvasType = AccessTools.TypeByName("UnityEngine.Canvas");
        var canvas = menu.menuButtons == null || canvasType == null
            ? null
            : menu.menuButtons.GetComponentInParent(canvasType) as Component;
        if (canvas != null)
            return canvas.transform;

        canvas = canvasType == null ? null : UnityEngine.Object.FindObjectOfType(canvasType) as Component;
        return canvas == null ? null : canvas.transform;
    }

    private void RememberFailure(int menuId, MenuManager menu, string error)
    {
        _screens[menuId] = new ScreenHandle
        {
            Menu = menu,
            PrepareFailed = true,
            LastError = error
        };
    }

    private static bool IsAlive(UnityEngine.Object? obj) => obj != null;

    private sealed class ScreenHandle
    {
        public MenuManager Menu = null!;
        public Transform? CanvasRoot;
        public IOverseerHostScreen Screen = null!;
        public UnityEngine.Object? ScreenObject;
        public bool PrepareFailed;
        public string LastError = "";
    }

}
