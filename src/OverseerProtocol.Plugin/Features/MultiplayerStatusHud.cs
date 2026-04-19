using System;
using System.Reflection;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using UnityEngine;

namespace OverseerProtocol.Features;

public sealed class MultiplayerStatusHud : MonoBehaviour
{
    private readonly MultiplayerFeature _feature = new();
    private Behaviour? _canvas;
    private Component? _label;
    private PropertyInfo? _labelTextProperty;
    private float _nextRefreshTime;
    private bool _failedToCreate;

    private void Update()
    {
        if (!OPConfig.EnableMultiplayer.Value || !OPConfig.ShowLobbyStatusHud.Value)
        {
            if (_canvas != null)
                _canvas.enabled = false;
            return;
        }

        EnsureHud();
        if (_canvas != null)
            _canvas.enabled = true;

        if (_label != null && _labelTextProperty != null && Time.unscaledTime >= _nextRefreshTime)
        {
            _labelTextProperty.SetValue(_label, _feature.BuildStatusText(), null);
            _nextRefreshTime = Time.unscaledTime + 1f;
        }
    }

    private void EnsureHud()
    {
        if (_failedToCreate || (_canvas != null && _label != null))
            return;

        try
        {
            var canvasType = FindType("UnityEngine.Canvas, UnityEngine.UIModule");
            var renderModeType = FindType("UnityEngine.RenderMode, UnityEngine.UIModule");
            var canvasScalerType = FindType("UnityEngine.UI.CanvasScaler, UnityEngine.UI");
            var graphicRaycasterType = FindType("UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
            var textType = FindType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

            var root = new GameObject("OverseerMultiplayerStatusHud");
            DontDestroyOnLoad(root);

            _canvas = (Behaviour)root.AddComponent(canvasType);
            canvasType.GetProperty("renderMode")?.SetValue(_canvas, Enum.Parse(renderModeType, "ScreenSpaceOverlay"), null);
            canvasType.GetProperty("sortingOrder")?.SetValue(_canvas, 1000, null);
            root.AddComponent(canvasScalerType);
            root.AddComponent(graphicRaycasterType);

            var labelObject = new GameObject("StatusText");
            labelObject.transform.SetParent(root.transform, false);
            _label = labelObject.AddComponent(textType);
            _labelTextProperty = textType.GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
            textType.GetProperty("fontSize")?.SetValue(_label, 14f, null);
            textType.GetProperty("color")?.SetValue(_label, Color.white, null);
            textType.GetProperty("raycastTarget")?.SetValue(_label, false, null);

            var rect = labelObject.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(12f, -12f);
                rect.sizeDelta = new Vector2(340f, 78f);
            }
        }
        catch (Exception ex)
        {
            _failedToCreate = true;
            OPLog.Warning("Multiplayer", $"Could not create multiplayer HUD: {ex.Message}");
        }
    }

    private static Type FindType(string assemblyQualifiedName)
    {
        var type = Type.GetType(assemblyQualifiedName);
        if (type == null)
            throw new InvalidOperationException("Type not found: " + assemblyQualifiedName);

        return type;
    }
}
