using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OverseerProtocol.Core.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace OverseerProtocol.Features;

internal sealed class LobbyHostMenuFeature : MonoBehaviour
{
    private static LobbyHostMenuFeature? Current;

    private static readonly Color ScreenTint = new(0.11f, 0.005f, 0.005f, 0.34f);
    private static readonly Color WindowColor = new(0.43f, 0.035f, 0.045f, 0.96f);
    private static readonly Color WindowDark = new(0.20f, 0.018f, 0.026f, 0.86f);
    private static readonly Color ContainerColor = new(0.32f, 0.045f, 0.038f, 0.82f);
    private static readonly Color ButtonIdle = new(0.48f, 0.12f, 0.07f, 0.96f);
    private static readonly Color ButtonActive = new(0.98f, 0.43f, 0.17f, 1f);
    private static readonly Color FieldColor = new(0.94f, 0.39f, 0.27f, 1f);
    private static readonly Color LineColor = new(0.95f, 0.04f, 0.12f, 0.95f);
    private static readonly Color TextColor = new(1f, 0.63f, 0.34f, 1f);
    private static readonly Color MutedTextColor = new(0.88f, 0.60f, 0.46f, 1f);

    private static readonly string[] Tabs = { "Lobby", "Perks", "Moons", "Scrap / Items", "Spawns" };
    private static Font? _font;

    private readonly List<TabButton> _tabButtons = new();
    private readonly List<PresetButton> _presetButtons = new();
    private TuningDraftStore _draft = null!;
    private Action? _onStartHost;
    private Action? _onCancel;
    private GameObject _content = null!;
    private Text _statusText = null!;
    private string _activeTab = "Lobby";
    private bool _initialized;

    private sealed class TabButton
    {
        public string Name = "";
        public Image Background = null!;
        public Text Label = null!;
    }

    private sealed class PresetButton
    {
        public LobbyPresetDefinition Preset = null!;
        public Image Background = null!;
        public Text Label = null!;
    }

    public static void Prepare(Transform parent)
    {
        if (Current != null)
            return;

        var root = new GameObject("OverseerLobbyHostMenu");
        root.transform.SetParent(parent, false);
        Current = root.AddComponent<LobbyHostMenuFeature>();
        Current.Initialize();
        root.SetActive(false);
    }

    public static void Open(Transform parent, Action onStartHost, Action? onCancel = null)
    {
        Prepare(parent);
        if (Current == null)
            return;

        Current.transform.SetParent(parent, false);
        Current._onStartHost = onStartHost;
        Current._onCancel = onCancel;
        Current._draft = TuningDraftStore.Load();
        Current._activeTab = "Lobby";
        Current.gameObject.SetActive(true);
        Current.BuildActiveTab();
        Current.UpdateTabs();
        Current.UpdatePresets();
        Current.SetStatus("Ready.");
        Current.transform.SetAsLastSibling();
        OPLog.Info("LobbyMenu", "Overseer host menu opened.");
    }

    private void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        _draft = TuningDraftStore.Load();

        var rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        gameObject.AddComponent<Image>().color = ScreenTint;

        var window = CreateObject("Window", transform);
        var windowRect = window.AddComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = new Vector2(1120f, 660f);
        window.AddComponent<Image>().color = WindowColor;
        AddOutline(window, 2f);

        var windowLayout = window.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(18, 18, 14, 14);
        windowLayout.spacing = 12f;
        windowLayout.childControlWidth = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childControlHeight = false;
        windowLayout.childForceExpandHeight = false;

        CreateText(window.transform, "OverseerProtocol", 28, TextColor, TextAnchor.MiddleCenter, 34f);
        CreateText(window.transform, "Lobby configuration", 16, MutedTextColor, TextAnchor.MiddleCenter, 24f);

        var body = CreateHorizontal(window.transform, "Body", 500f);
        body.GetComponent<HorizontalLayoutGroup>().spacing = 14f;

        BuildPresetColumn(body.transform);
        BuildSettingsArea(body.transform);
        BuildFooter(window.transform);

        BuildActiveTab();
        OPLog.Info("LobbyMenu", "Overseer host menu prepared.");
    }

    private void BuildPresetColumn(Transform parent)
    {
        var presets = CreatePanel(parent, "Presets", 220f);
        var layout = presets.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        CreateText(presets.transform, "Presets", 20, TextColor, TextAnchor.MiddleLeft, 34f);
        CreateText(presets.transform, "Choose the base rules before editing the tabs.", 13, MutedTextColor, TextAnchor.UpperLeft, 54f);

        foreach (var preset in LobbyPresetDefinition.All)
        {
            var captured = preset;
            var button = CreateButton(presets.transform, preset.DisplayName, () =>
            {
                _draft.ApplyPreset(captured);
                UpdatePresets();
                BuildActiveTab();
                SetStatus("Preset selected: " + captured.DisplayName);
            }, 0f, 38f);
            _presetButtons.Add(new PresetButton
            {
                Preset = preset,
                Background = button.GetComponent<Image>(),
                Label = button.GetComponentInChildren<Text>()
            });
        }

        var spacer = CreateObject("PresetSpacer", presets.transform);
        spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;
        CreateText(presets.transform, "Config is saved when Start Host is pressed.", 12, MutedTextColor, TextAnchor.LowerLeft, 46f);
    }

    private void BuildSettingsArea(Transform parent)
    {
        var area = CreatePanel(parent, "SettingsArea", 0f);
        area.GetComponent<LayoutElement>().flexibleWidth = 1f;
        var layout = area.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;

        var tabs = CreateHorizontal(area.transform, "Tabs", 46f);
        tabs.GetComponent<HorizontalLayoutGroup>().spacing = 8f;
        foreach (var tab in Tabs)
        {
            var captured = tab;
            var button = CreateButton(tabs.transform, tab, () =>
            {
                _activeTab = captured;
                UpdateTabs();
                BuildActiveTab();
            }, 128f, 38f);
            _tabButtons.Add(new TabButton
            {
                Name = tab,
                Background = button.GetComponent<Image>(),
                Label = button.GetComponentInChildren<Text>()
            });
        }

        _content = CreateScrollContent(area.transform, 418f);
    }

    private void BuildFooter(Transform parent)
    {
        var footer = CreateHorizontal(parent, "Footer", 46f);
        footer.GetComponent<HorizontalLayoutGroup>().spacing = 10f;
        _statusText = CreateText(footer.transform, "Ready.", 14, MutedTextColor, TextAnchor.MiddleLeft, 0f);
        _statusText.GetComponent<LayoutElement>().flexibleWidth = 1f;
        CreateButton(footer.transform, "[ Cancel ]", Cancel, 150f, 38f);
        CreateButton(footer.transform, "[ Start Host ]", StartHost, 170f, 38f);
    }

    private void BuildActiveTab()
    {
        Clear(_content.transform);
        switch (_activeTab)
        {
            case "Perks":
                BuildPerksTab();
                break;
            case "Moons":
                BuildMoonsTab();
                break;
            case "Scrap / Items":
                BuildItemsTab();
                break;
            case "Spawns":
                BuildSpawnsTab();
                break;
            default:
                BuildLobbyTab();
                break;
        }
    }

    private void BuildLobbyTab()
    {
        AddSection("Lobby", "Controls multiplayer patches and host compatibility checks.");
        AddToggle(_content.transform, "Enable multiplayer patches", _draft.Lobby.EnableMultiplayer, value => _draft.Lobby.EnableMultiplayer = value);
        AddIntField(_content.transform, "Max players", _draft.Lobby.MaxPlayers, value => _draft.Lobby.MaxPlayers = value, 1, 64);
        AddToggle(_content.transform, "Enable late join", _draft.Lobby.EnableLateJoin, value => _draft.Lobby.EnableLateJoin = value);
        AddToggle(_content.transform, "Late join in orbit", _draft.Lobby.LateJoinInOrbit, value => _draft.Lobby.LateJoinInOrbit = value);
        AddToggle(_content.transform, "Late join on moon as spectator", _draft.Lobby.LateJoinOnMoonAsSpectator, value => _draft.Lobby.LateJoinOnMoonAsSpectator = value);

        AddSection("Compatibility", "Reject clients when their mod version or runtime tuning does not match.");
        AddToggle(_content.transform, "Require same mod version", _draft.Lobby.RequireSameModVersion, value => _draft.Lobby.RequireSameModVersion = value);
        AddToggle(_content.transform, "Require same config hash", _draft.Lobby.RequireSameConfigHash, value => _draft.Lobby.RequireSameConfigHash = value);
    }

    private void BuildPerksTab()
    {
        AddSection("Perks", "Read-only progression view. Editing ranks during host setup is intentionally disabled.");
        var summary = new PerkMenuProvider().BuildReadOnlySummary();
        var text = CreateText(_content.transform, summary, 14, MutedTextColor, TextAnchor.UpperLeft, 1200f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private void BuildMoonsTab()
    {
        AddSection("Moons", "Route prices, risk text and scrap ranges are written to the Overseer runtime config.");
        if (_draft.Moons.Count == 0)
        {
            CreateWarning("No moon catalog is available yet. Start once with data export enabled, then reopen this menu.");
            return;
        }

        foreach (var moon in _draft.Moons)
        {
            var block = AddBlock(moon.Baseline.DisplayName + "  [" + moon.Baseline.Id + "]");
            AddToggle(block.transform, "Enabled", moon.Enabled, value => moon.Enabled = value);
            AddIntField(block.transform, "Route price", moon.RoutePrice, value => { moon.RoutePrice = value; moon.Enabled = true; }, 0, 99999);
            AddIntField(block.transform, "Risk level", moon.RiskLevel, value => { moon.RiskLevel = value; moon.Enabled = true; }, 0, 5);
            AddTextField(block.transform, "Risk label", moon.RiskLabel, value => { moon.RiskLabel = value; moon.Enabled = true; });
            AddTextField(block.transform, "Description", moon.Description, value => { moon.Description = value; moon.Enabled = true; });
            AddIntField(block.transform, "Min scrap", moon.MinScrap, value => { moon.MinScrap = value; moon.Enabled = true; }, 0, 999);
            AddIntField(block.transform, "Max scrap", moon.MaxScrap, value => { moon.MaxScrap = value; moon.Enabled = true; }, 0, 999);
            AddIntField(block.transform, "Min total scrap value", moon.MinTotalScrapValue, value => { moon.MinTotalScrapValue = value; moon.Enabled = true; }, 0, 99999);
            AddIntField(block.transform, "Max total scrap value", moon.MaxTotalScrapValue, value => { moon.MaxTotalScrapValue = value; moon.Enabled = true; }, 0, 99999);
            CreateButton(block.transform, "Reset this moon", () => { moon.Reset(); BuildActiveTab(); }, 150f, 32f);
        }
    }

    private void BuildItemsTab()
    {
        AddSection("Scrap / Items", "Item and scrap overrides are validated by the existing runtime tuning pipeline.");
        if (_draft.Items.Count == 0)
        {
            CreateWarning("No item/scrap catalog is available yet. Start once with data export enabled, then reopen this menu.");
            return;
        }

        foreach (var item in _draft.Items)
        {
            var block = AddBlock(item.Baseline.DisplayName + "  [" + item.Baseline.Id + "]");
            AddToggle(block.transform, "Enabled", item.Enabled, value => item.Enabled = value);
            AddIntField(block.transform, "Value / store price", item.Value, value => { item.Value = value; item.Enabled = true; }, 0, 99999);
            AddFloatField(block.transform, "Weight", item.Weight, value => { item.Weight = value; item.Enabled = true; }, 0f, 100f);
            AddToggle(block.transform, "Is scrap", item.IsScrap, value => { item.IsScrap = value; item.Enabled = true; });
            AddIntField(block.transform, "Min scrap value", item.MinScrapValue, value => { item.MinScrapValue = value; item.Enabled = true; }, 0, 99999);
            AddIntField(block.transform, "Max scrap value", item.MaxScrapValue, value => { item.MaxScrapValue = value; item.Enabled = true; }, 0, 99999);
            AddToggle(block.transform, "Requires battery", item.RequiresBattery, value => { item.RequiresBattery = value; item.Enabled = true; });
            AddToggle(block.transform, "Conductive", item.Conductive, value => { item.Conductive = value; item.Enabled = true; });
            AddToggle(block.transform, "Two handed", item.TwoHanded, value => { item.TwoHanded = value; item.Enabled = true; });
            CreateButton(block.transform, "Reset this item", () => { item.Reset(); BuildActiveTab(); }, 150f, 32f);
        }
    }

    private void BuildSpawnsTab()
    {
        AddSection("Spawns", "Pools use the same EnemyId:rarity format consumed by SpawnOverrideFeature.");
        if (_draft.Spawns.Count == 0)
        {
            CreateWarning("No spawn catalog is available yet. Start once with data export enabled, then reopen this menu.");
            return;
        }

        if (_draft.EnemyIds.Count > 0)
            CreateText(_content.transform, "Known enemies: " + string.Join(", ", _draft.EnemyIds.Take(40)) + (_draft.EnemyIds.Count > 40 ? " ..." : ""), 13, MutedTextColor, TextAnchor.MiddleLeft, 46f);

        foreach (var spawn in _draft.Spawns)
        {
            var block = AddBlock(spawn.Baseline.MoonId);
            AddToggle(block.transform, "Replace inside enemies", spawn.InsideEnabled, value => spawn.InsideEnabled = value);
            AddTextField(block.transform, "Inside enemies", spawn.InsideEnemies, value => { spawn.InsideEnemies = value; spawn.InsideEnabled = true; });
            AddToggle(block.transform, "Replace outside enemies", spawn.OutsideEnabled, value => spawn.OutsideEnabled = value);
            AddTextField(block.transform, "Outside enemies", spawn.OutsideEnemies, value => { spawn.OutsideEnemies = value; spawn.OutsideEnabled = true; });
            AddToggle(block.transform, "Replace daytime enemies", spawn.DaytimeEnabled, value => spawn.DaytimeEnabled = value);
            AddTextField(block.transform, "Daytime enemies", spawn.DaytimeEnemies, value => { spawn.DaytimeEnemies = value; spawn.DaytimeEnabled = true; });
            CreateButton(block.transform, "Reset this moon spawns", () => { spawn.Reset(); BuildActiveTab(); }, 190f, 32f);
        }
    }

    private void StartHost()
    {
        foreach (var spawn in _draft.Spawns)
        {
            if (!SpawnDraft.LooksLikeSpawnList(spawn.InsideEnemies) ||
                !SpawnDraft.LooksLikeSpawnList(spawn.OutsideEnemies) ||
                !SpawnDraft.LooksLikeSpawnList(spawn.DaytimeEnemies))
            {
                SetStatus("Invalid spawn format in " + spawn.Baseline.MoonId + ". Use EnemyId:rarity, EnemyId:rarity.");
                return;
            }
        }

        try
        {
            _draft.Save();
            RuntimeServices.Orchestrator.ReloadRuntimeConfiguration();
            new MultiplayerFeature().Apply();
            SetStatus("Saved. Starting host...");
            var start = _onStartHost;
            Close();
            start?.Invoke();
        }
        catch (Exception ex)
        {
            SetStatus("Could not start host: " + ex.Message);
            OPLog.Warning("LobbyMenu", "Host menu apply/start failed: " + ex);
        }
    }

    private void Cancel()
    {
        OPLog.Info("LobbyMenu", "Overseer host menu cancelled.");
        var cancel = _onCancel;
        Close();
        cancel?.Invoke();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void AddSection(string title, string description)
    {
        var section = AddBlock(title);
        CreateText(section.transform, description, 13, MutedTextColor, TextAnchor.UpperLeft, 36f);
    }

    private GameObject AddBlock(string title)
    {
        var block = CreateObject("ConfigContainer", _content.transform);
        block.AddComponent<Image>().color = ContainerColor;
        AddOutline(block, 1f);
        var layout = block.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 9, 9);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        block.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        CreateText(block.transform, title, 17, TextColor, TextAnchor.MiddleLeft, 26f);
        return block;
    }

    private void CreateWarning(string text)
    {
        var warning = CreateText(_content.transform, text, 15, TextColor, TextAnchor.MiddleLeft, 58f);
        warning.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private void AddToggle(Transform parent, string label, bool value, Action<bool> changed)
    {
        var row = CreateHorizontal(parent, label + "Row", 30f);
        var toggleObject = CreateObject("Toggle", row.transform);
        toggleObject.AddComponent<LayoutElement>().preferredWidth = 28f;
        var background = toggleObject.AddComponent<Image>();
        background.color = WindowDark;
        AddOutline(toggleObject, 1f);
        var toggle = toggleObject.AddComponent<Toggle>();
        var check = CreateObject("Checkmark", toggleObject.transform);
        var checkRect = check.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.pivot = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(14f, 14f);
        var checkImage = check.AddComponent<Image>();
        checkImage.color = TextColor;
        toggle.targetGraphic = background;
        toggle.graphic = checkImage;
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(changed));
        CreateText(row.transform, label + ":", 14, MutedTextColor, TextAnchor.MiddleLeft, 0f).GetComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void AddIntField(Transform parent, string label, int value, Action<int> changed, int min, int max)
    {
        AddTextField(parent, label, value.ToString(CultureInfo.InvariantCulture), raw =>
        {
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                SetStatus("Invalid integer for " + label + ".");
                return;
            }

            changed(Math.Max(min, Math.Min(max, parsed)));
        });
    }

    private void AddFloatField(Transform parent, string label, float value, Action<float> changed, float min, float max)
    {
        AddTextField(parent, label, value.ToString("0.###", CultureInfo.InvariantCulture), raw =>
        {
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                SetStatus("Invalid number for " + label + ".");
                return;
            }

            changed(Math.Max(min, Math.Min(max, parsed)));
        });
    }

    private void AddTextField(Transform parent, string label, string value, Action<string> changed)
    {
        var row = CreateHorizontal(parent, label + "Row", 32f);
        CreateText(row.transform, label + ":", 14, MutedTextColor, TextAnchor.MiddleLeft, 0f).GetComponent<LayoutElement>().preferredWidth = 210f;
        var input = CreateInput(row.transform, value);
        input.onEndEdit.AddListener(new UnityEngine.Events.UnityAction<string>(changed));
    }

    private void UpdateTabs()
    {
        foreach (var tab in _tabButtons)
        {
            var active = string.Equals(tab.Name, _activeTab, StringComparison.Ordinal);
            tab.Background.color = active ? ButtonActive : ButtonIdle;
            tab.Label.color = active ? Color.black : TextColor;
            tab.Label.fontSize = active ? 16 : 14;
        }
    }

    private void UpdatePresets()
    {
        var activePreset = LobbyPresetDefinition.Resolve(_draft.Lobby.PresetId);
        foreach (var preset in _presetButtons)
        {
            var active = string.Equals(activePreset.Id, preset.Preset.Id, StringComparison.OrdinalIgnoreCase);
            preset.Background.color = active ? ButtonActive : ButtonIdle;
            preset.Label.color = active ? Color.black : TextColor;
            preset.Label.fontSize = active ? 16 : 14;
        }
    }

    private static GameObject CreatePanel(Transform parent, string name, float width)
    {
        var panel = CreateObject(name, parent);
        panel.AddComponent<Image>().color = WindowDark;
        AddOutline(panel, 1f);
        var element = panel.AddComponent<LayoutElement>();
        if (width > 0f)
            element.preferredWidth = width;
        return panel;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject CreateHorizontal(Transform parent, string name, float height)
    {
        var row = CreateObject(name, parent);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        var element = row.AddComponent<LayoutElement>();
        element.preferredHeight = height;
        return row;
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
        var element = obj.AddComponent<LayoutElement>();
        if (preferredHeight > 0f)
            element.preferredHeight = preferredHeight;
        return label;
    }

    private static Button CreateButton(Transform parent, string label, Action action, float width, float height)
    {
        var obj = CreateObject(label + "Button", parent);
        var image = obj.AddComponent<Image>();
        image.color = ButtonIdle;
        AddOutline(obj, 1f);
        var button = obj.AddComponent<Button>();
        button.onClick.AddListener(new UnityEngine.Events.UnityAction(action));

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.75f, 0.48f, 1f);
        colors.pressedColor = new Color(0.80f, 0.20f, 0.08f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        var element = obj.AddComponent<LayoutElement>();
        if (width > 0f)
            element.preferredWidth = width;
        else
            element.flexibleWidth = 1f;
        element.preferredHeight = height;

        var text = CreateText(obj.transform, label, 14, TextColor, TextAnchor.MiddleCenter, 0f);
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return button;
    }

    private static InputField CreateInput(Transform parent, string value)
    {
        var obj = CreateObject("Input", parent);
        obj.AddComponent<Image>().color = FieldColor;
        AddOutline(obj, 1f);
        var input = obj.AddComponent<InputField>();
        var element = obj.AddComponent<LayoutElement>();
        element.flexibleWidth = 1f;
        element.preferredHeight = 30f;

        var text = CreateText(obj.transform, value, 14, Color.black, TextAnchor.MiddleLeft, 0f);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);
        input.textComponent = text;
        input.text = value;
        return input;
    }

    private static GameObject CreateScrollContent(Transform parent, float height)
    {
        var scrollObject = CreateObject("Scroll", parent);
        scrollObject.AddComponent<LayoutElement>().preferredHeight = height;
        scrollObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.22f);
        AddOutline(scrollObject, 1f);
        var scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = CreateObject("Viewport", scrollObject.transform);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = CreateObject("Content", viewport.transform);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(10f, 0f);
        contentRect.offsetMax = new Vector2(-10f, 0f);
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 9f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        return content;
    }

    private static void AddOutline(GameObject obj, float thickness)
    {
        var outline = obj.AddComponent<Outline>();
        outline.effectColor = LineColor;
        outline.effectDistance = new Vector2(thickness, -thickness);
        outline.useGraphicAlpha = false;
    }

    private static void Clear(Transform transform)
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void SetStatus(string text)
    {
        if (_statusText != null)
            _statusText.text = text;
    }

    private static Font GetFont()
    {
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return _font;
    }
}
