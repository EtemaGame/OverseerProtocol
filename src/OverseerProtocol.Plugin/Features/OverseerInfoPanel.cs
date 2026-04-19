using System;
using System.Linq;
using System.Reflection;
using OverseerProtocol.Core.Logging;
using Unity.Netcode;
using UnityEngine;

namespace OverseerProtocol.Features;

public sealed class OverseerInfoPanel : MonoBehaviour
{
    private readonly MultiplayerFeature _multiplayer = new();
    private Behaviour? _canvas;
    private Component? _label;
    private PropertyInfo? _labelTextProperty;
    private bool _visible;
    private bool _failedToCreate;
    private float _nextRefreshTime;

    public void Initialize()
    {
        EnsurePanel();
        OverseerInputActions.OpenPanelPressed -= TogglePanel;
        OverseerInputActions.OpenPanelPressed += TogglePanel;
        if (_canvas != null)
            _canvas.enabled = false;
    }

    private void Update()
    {
        Tick();
    }

    public void Tick()
    {
        if (_canvas != null)
            _canvas.enabled = _visible;

        if (_visible)
            RefreshText(force: false);
    }

    private void EnsurePanel()
    {
        if (_failedToCreate || (_canvas != null && _label != null))
            return;

        try
        {
            var canvasType = FindType("UnityEngine.Canvas, UnityEngine.UIModule");
            var renderModeType = FindType("UnityEngine.RenderMode, UnityEngine.UIModule");
            var canvasScalerType = FindType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            var graphicRaycasterType = FindType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
            var imageType = FindType("UnityEngine.UI.Image, UnityEngine.UI");
            var textType = FindType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

            var root = new GameObject("OverseerInfoPanel");
            DontDestroyOnLoad(root);

            _canvas = (Behaviour)root.AddComponent(canvasType);
            canvasType.GetProperty("renderMode")?.SetValue(_canvas, Enum.Parse(renderModeType, "ScreenSpaceOverlay"), null);
            canvasType.GetProperty("sortingOrder")?.SetValue(_canvas, 1100, null);
            root.AddComponent(canvasScalerType);
            root.AddComponent(graphicRaycasterType);
            _canvas.enabled = false;

            var backgroundObject = new GameObject("PanelBackground");
            backgroundObject.transform.SetParent(root.transform, false);
            var background = backgroundObject.AddComponent(imageType);
            imageType.GetProperty("color")?.SetValue(background, new Color(0f, 0f, 0f, 0.78f), null);

            var backgroundRect = backgroundObject.transform as RectTransform;
            if (backgroundRect != null)
            {
                backgroundRect.anchorMin = new Vector2(0f, 1f);
                backgroundRect.anchorMax = new Vector2(0f, 1f);
                backgroundRect.pivot = new Vector2(0f, 1f);
                backgroundRect.anchoredPosition = new Vector2(20f, -48f);
                backgroundRect.sizeDelta = new Vector2(660f, 520f);
            }

            var labelObject = new GameObject("PanelText");
            labelObject.transform.SetParent(root.transform, false);
            _label = labelObject.AddComponent(textType);
            _labelTextProperty = textType.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            textType.GetProperty("fontSize")?.SetValue(_label, 16f, null);
            textType.GetProperty("color")?.SetValue(_label, new Color(0.82f, 1f, 0.72f, 1f), null);
            textType.GetProperty("raycastTarget")?.SetValue(_label, false, null);

            var rect = labelObject.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(32f, -64f);
                rect.sizeDelta = new Vector2(620f, 480f);
            }

            OPLog.Info("HUD", "Overseer info panel ready. Toggle action=Open Overseer Panel.");
        }
        catch (Exception ex)
        {
            _failedToCreate = true;
            OPLog.Warning("HUD", $"Could not create Overseer info panel: {ex.Message}");
        }
    }

    private void TogglePanel()
    {
        _visible = !_visible;
        EnsurePanel();
        RefreshText(force: true);
        OPLog.Info("HUD", "Overseer info panel " + (_visible ? "opened." : "closed."));
    }

    private void RefreshText(bool force)
    {
        if (_label == null || _labelTextProperty == null)
            return;

        if (!force && Time.unscaledTime < _nextRefreshTime)
            return;

        _labelTextProperty.SetValue(_label, BuildPanelText(), null);
        _nextRefreshTime = Time.unscaledTime + 1f;
    }

    private string BuildPanelText()
    {
        var isHost = NetworkManager.Singleton == null || NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer;
        var progression = new ProgressionStore().LoadOrCreate();
        var catalog = new PerkCatalogFeature().LoadOrCreate();
        var localName = GameNetworkManager.Instance?.localPlayerController?.playerUsername ?? "Local player";
        var localPlayer = progression.Players.FirstOrDefault(player => string.Equals(player.DisplayName, localName, StringComparison.OrdinalIgnoreCase));

        var text =
            "OverseerProtocol\n" +
            "Toggle: Open Overseer Panel\n\n" +
            _multiplayer.BuildStatusText() + "\n\n" +
            "Player perks\n" +
            "Name: " + localName + "\n" +
            "Level: " + (localPlayer?.Level ?? 0) + "\n" +
            "XP: " + (localPlayer?.Experience ?? 0) + "\n" +
            "Unspent points: " + (localPlayer?.UnspentPoints ?? 0) + "\n" +
            "Unlocked ranks: " + (localPlayer?.PerkRanks.Count ?? 0) + "\n" +
            "Available player perks: " + catalog.PlayerPerks.Count + "\n";

        if (!isHost)
            return text + "\nShip perks\nHost access only.";

        return text +
               "\nShip perks (host)\n" +
               "Ship level: " + progression.Ship.Level + "\n" +
               "Ship XP: " + progression.Ship.Experience + "\n" +
               "Unspent points: " + progression.Ship.UnspentPoints + "\n" +
               "Unlocked ranks: " + progression.Ship.PerkRanks.Count + "\n" +
               "Available ship perks: " + catalog.ShipPerks.Count + "\n" +
               "\nPerk appliers are not active yet; this panel is read-only.";
    }

    private static Type FindType(string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName);
        if (type == null)
            throw new InvalidOperationException("Type not found: " + assemblyQualifiedName);

        return type;
    }

    private void OnDestroy()
    {
        OverseerInputActions.OpenPanelPressed -= TogglePanel;
    }
}
