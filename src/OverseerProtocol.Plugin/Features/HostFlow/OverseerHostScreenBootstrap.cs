using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using OverseerProtocol.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace OverseerProtocol.Features.HostFlow;

internal sealed class OverseerHostScreenBootstrap
{
    // Temporary compatibility shim until AssetBundle host screen lands.
    private static readonly bool UseLegacyHostScreenForBootstrapTesting = true;
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

        if (!UseLegacyHostScreenForBootstrapTesting)
        {
            error = "AssetBundle host screen is not implemented yet.";
            return false;
        }

        try
        {
            var root = new GameObject("OverseerHostScreenShim");
            root.transform.SetParent(canvas, false);
            var shim = root.AddComponent<LegacyHostScreenShim>();
            shim.Initialize();
            root.SetActive(false);
            screen = shim;
            OPLog.Info("HostFlow", "Prepared temporary Overseer host screen shim.");
            return true;
        }
        catch (Exception ex)
        {
            error = "Could not create host screen shim: " + ex.Message;
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

    private sealed class LegacyHostScreenShim : MonoBehaviour, IOverseerHostScreen
    {
        private static Font? _font;
        private OverseerHostReadModel? _model;
        private Text _title = null!;
        private Text _body = null!;
        private Text _status = null!;
        private Button _continueButton = null!;
        private Button _cancelButton = null!;
        private bool _waitingForWarningConfirmation;

        public event Action<OverseerHostProfile>? ContinueRequested;
        public event Action<OverseerHostProfile>? WarningsConfirmed;
        public event Action? CancelRequested;
#pragma warning disable CS0067
        public event Action<OverseerHostProfile>? SaveRequested;
#pragma warning restore CS0067

        public void Initialize()
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var overlay = gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            var window = CreateObject("Window", transform);
            var windowRect = window.AddComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(760f, 420f);
            window.AddComponent<Image>().color = new Color(0.12f, 0.035f, 0.04f, 0.97f);
            var layout = window.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            _title = CreateText(window.transform, "OverseerProtocol", 28, new Color(1f, 0.68f, 0.38f), TextAnchor.MiddleCenter, 42f);
            _body = CreateText(window.transform, "", 15, new Color(0.95f, 0.78f, 0.60f), TextAnchor.UpperLeft, 210f);
            _status = CreateText(window.transform, "Ready.", 14, new Color(0.88f, 0.62f, 0.48f), TextAnchor.MiddleLeft, 46f);

            var footer = CreateObject("Footer", window.transform);
            footer.AddComponent<LayoutElement>().preferredHeight = 44f;
            var footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 10f;
            footerLayout.childControlWidth = false;
            footerLayout.childForceExpandWidth = false;
            footerLayout.childControlHeight = true;
            footerLayout.childForceExpandHeight = true;

            var spacer = CreateObject("Spacer", footer.transform);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
            _cancelButton = CreateButton(footer.transform, "[ Cancel ]", OnCancelClicked, 150f);
            _continueButton = CreateButton(footer.transform, "[ Continue ]", OnContinueClicked, 170f);
        }

        public void Open(OverseerHostReadModel model)
        {
            _model = model;
            _waitingForWarningConfirmation = false;
            _continueButton.GetComponentInChildren<Text>().text = "[ Continue ]";
            _title.text = "OverseerProtocol Host Setup";
            _body.text = BuildBody(model);
            _status.text = model.InitialWarnings.Count == 0
                ? "Ready."
                : "Opened with warnings. Review before continuing.";
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void SetBusy(bool busy)
        {
            _continueButton.interactable = !busy;
            _cancelButton.interactable = !busy;
        }

        public void ShowError(string message)
        {
            _waitingForWarningConfirmation = false;
            _continueButton.GetComponentInChildren<Text>().text = "[ Continue ]";
            _status.text = message;
        }

        public void ShowStatus(string message)
        {
            _status.text = message;
        }

        public void ShowWarnings(IReadOnlyList<string> warnings)
        {
            _waitingForWarningConfirmation = true;
            _continueButton.GetComponentInChildren<Text>().text = "[ Confirm ]";
            _status.text = warnings.Count == 0 ? "Confirm warnings to continue." : string.Join("\n", warnings);
        }

        private void OnContinueClicked()
        {
            if (_model == null)
                return;

            if (_waitingForWarningConfirmation)
            {
                WarningsConfirmed?.Invoke(_model.InitialDraft);
                return;
            }

            ContinueRequested?.Invoke(_model.InitialDraft);
        }

        private void OnCancelClicked()
        {
            CancelRequested?.Invoke();
        }

        private static string BuildBody(OverseerHostReadModel model)
        {
            var presetNames = string.Join(", ", model.BuiltInPresets.Select(preset => preset.DisplayName));
            var lines = new List<string>
            {
                "Temporary host setup surface. AssetBundle UI will replace this shim.",
                "",
                "Active preset: " + model.ActivePresetId,
                "Built-in presets: " + presetNames,
                "",
                "This screen currently validates the new host-flow lifecycle only: open, cancel, warning confirmation, and safe return to vanilla."
            };

            if (model.InitialWarnings.Count > 0)
            {
                lines.Add("");
                lines.Add("Warnings:");
                lines.AddRange(model.InitialWarnings);
            }

            return string.Join("\n", lines);
        }

        private static GameObject CreateObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static Text CreateText(Transform parent, string text, int size, Color color, TextAnchor anchor, float preferredHeight)
        {
            var obj = CreateObject("Text", parent);
            var label = obj.AddComponent<Text>();
            label.font = GetFont();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            obj.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            return label;
        }

        private static Button CreateButton(Transform parent, string label, Action clicked, float width)
        {
            var obj = CreateObject(label + "Button", parent);
            obj.AddComponent<Image>().color = new Color(0.55f, 0.14f, 0.08f, 1f);
            var button = obj.AddComponent<Button>();
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(clicked));
            obj.AddComponent<LayoutElement>().preferredWidth = width;

            var text = CreateText(obj.transform, label, 14, new Color(1f, 0.76f, 0.52f), TextAnchor.MiddleCenter, 0f);
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return button;
        }

        private static Font GetFont()
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            return _font;
        }
    }
}
