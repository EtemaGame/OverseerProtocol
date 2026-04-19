using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using OverseerProtocol.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OverseerProtocol.Features.HostFlow.OverseerHostScreen;

internal sealed class OverseerHostScreenAdapter : MonoBehaviour, IOverseerHostScreen
{
    private static readonly Color Background = FromHex(0x0A0000, 0.97f);
    private static readonly Color FrameColor = FromHex(0x120202, 0.98f);
    private static readonly Color HeaderColor = FromHex(0x100101, 0.98f);
    private static readonly Color Panel = FromHex(0x120202, 0.98f);
    private static readonly Color ContentColor = FromHex(0x0F0101, 0.98f);
    private static readonly Color SectionColor = FromHex(0x160303, 0.98f);
    private static readonly Color SectionHeaderColor = FromHex(0x1C0404, 0.98f);
    private static readonly Color PanelDark = FromHex(0x0D0101, 0.98f);
    private static readonly Color BorderColor = FromHex(0x3A0505, 1f);
    private static readonly Color Accent = FromHex(0xE03020, 1f);
    private static readonly Color Text = FromHex(0xF5A030, 1f);
    private static readonly Color TextMuted = FromHex(0x8A3A18, 1f);
    private static readonly Color TextDim = FromHex(0x5A1A1A, 1f);
    private static readonly Color StatusGreen = FromHex(0x4A7A40, 1f);
    private static readonly Color ButtonColor = FromHex(0x6A0F0A, 1f);

    private readonly OverseerHostPresetStore _presetStore = new();
    private readonly List<Button> _tabs = new();
    private readonly string[] _tabNames = { "Lobby", "Game", "Items", "Perks", "Enemies", "Moons" };

    private GameObject _screenRoot = null!;
    private Transform _presetContainer = null!;
    private Transform _tabContent = null!;
    private Button _continueButton = null!;
    private Button _cancelButton = null!;
    private Button _saveButton = null!;
    private Button _saveAsNewButton = null!;
    private TextMeshProUGUI _notificationText = null!;
    private Image _statusDot = null!;
    private TuningDraftStore? _draft;
    private OverseerHostReadModel? _model;
    private bool _waitingForWarningConfirmation;
    private bool _busy;
    private int _currentTabIndex;

    public event Action<OverseerHostProfile>? ContinueRequested;
    public event Action<OverseerHostProfile>? WarningsConfirmed;
    public event Action? CancelRequested;
    public event Action<OverseerHostProfile>? SaveRequested;

    public void Initialize()
    {
        var rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _screenRoot = CreateRoot();
        BuildLayout();
        WireFooterButtons();
        gameObject.SetActive(false);
    }

    public void Open(OverseerHostReadModel model)
    {
        _model = model;
        _draft = model.InitialDraft.Draft;
        _waitingForWarningConfirmation = false;
        _busy = false;
        SetButtonText(_continueButton, "> Continue");
        RebuildPresets();
        SelectTab(0);
        ShowStatus(model.InitialWarnings.Count == 0 ? "Ready." : string.Join("\n", model.InitialWarnings));
        gameObject.SetActive(true);
        _screenRoot.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        _screenRoot.SetActive(false);
        gameObject.SetActive(false);
    }

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _continueButton.interactable = !busy;
        _cancelButton.interactable = !busy;
        _saveButton.interactable = !busy;
        _saveAsNewButton.interactable = !busy;
    }

    public void ShowError(string message)
    {
        _waitingForWarningConfirmation = false;
        SetButtonText(_continueButton, "> Continue");
        ShowStatus(message);
    }

    public void ShowStatus(string message)
    {
        _notificationText.text = (message ?? "").ToUpperInvariant();
        _notificationText.color = StatusGreen;
        if (_statusDot != null)
            _statusDot.color = StatusGreen;
    }

    public void ShowWarnings(IReadOnlyList<string> warnings)
    {
        _waitingForWarningConfirmation = true;
        SetButtonText(_continueButton, "> Confirm");
        ShowStatus(warnings.Count == 0 ? "Confirm warnings to continue." : string.Join("\n", warnings));
    }

    private GameObject CreateRoot()
    {
        var root = new GameObject("OverseerHostScreen");
        root.transform.SetParent(transform, false);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        root.AddComponent<Image>().color = Background;
        return root;
    }

    private void BuildLayout()
    {
        var frame = CreatePanel(_screenRoot.transform, "Frame", FrameColor, true);
        var frameRect = (RectTransform)frame.transform;
        frameRect.anchorMin = new Vector2(0.16f, 0.12f);
        frameRect.anchorMax = new Vector2(0.84f, 0.88f);
        frameRect.offsetMin = Vector2.zero;
        frameRect.offsetMax = Vector2.zero;

        var frameLayout = frame.AddComponent<VerticalLayoutGroup>();
        frameLayout.padding = new RectOffset(0, 0, 0, 0);
        frameLayout.spacing = 0f;
        frameLayout.childForceExpandWidth = true;
        frameLayout.childForceExpandHeight = false;
        frameLayout.childControlWidth = true;
        frameLayout.childControlHeight = true;

        BuildHeader(frame.transform);

        var body = new GameObject("Body");
        body.transform.SetParent(frame.transform, false);
        body.AddComponent<RectTransform>();
        var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 0f;
        bodyLayout.childForceExpandHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childControlHeight = true;
        bodyLayout.childControlWidth = true;
        body.AddComponent<LayoutElement>().flexibleHeight = 1f;

        BuildPresetPanel(body.transform);
        BuildMainPanel(body.transform);
        BuildFooter(frame.transform);
    }

    private void BuildHeader(Transform parent)
    {
        var header = CreatePanel(parent, "Header", HeaderColor);
        header.AddComponent<LayoutElement>().preferredHeight = 48f;
        var layout = header.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 8, 8);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        var icon = CreatePanel(header.transform, "HeaderIcon", PanelDark, true);
        icon.AddComponent<LayoutElement>().preferredWidth = 28f;
        var iconText = CreateLabel(icon.transform, "O", 13f, Accent);
        iconText.alignment = TextAlignmentOptions.Center;
        Stretch(iconText.rectTransform, 0f);

        var title = CreateLabel(header.transform, "OVERSEERPROTOCOL - HOST SETUP", 17f, Text);
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 10f;
        title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var version = CreateLabel(header.transform, "v81", 10f, TextDim);
        version.alignment = TextAlignmentOptions.Center;
        version.gameObject.AddComponent<LayoutElement>().preferredWidth = 36f;
    }

    private void BuildPresetPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "PresetsPanel", Panel, true);
        panel.AddComponent<LayoutElement>().preferredWidth = 180f;
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 10, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var title = CreateLabel(panel.transform, "PRESETS", 11f, FromHex(0xA03010, 1f));
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 14f;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        var scroll = CreateScroll(panel.transform, "PresetScroll");
        scroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        _presetContainer = scroll.content;

        var create = CreateButton(panel.transform, "+ Create new", 13f, PanelDark, Text);
        create.onClick.AddListener(new UnityEngine.Events.UnityAction(SaveAsPreset));
        create.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
    }

    private void BuildMainPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "MainPanel", ContentColor);
        panel.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var tabBar = new GameObject("Tabs");
        tabBar.transform.SetParent(panel.transform, false);
        tabBar.AddComponent<RectTransform>();
        var tabsLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 0f;
        tabsLayout.childControlHeight = true;
        tabsLayout.childControlWidth = true;
        tabsLayout.childForceExpandWidth = false;
        tabBar.AddComponent<Image>().color = PanelDark;
        tabBar.AddComponent<LayoutElement>().preferredHeight = 32f;

        for (var i = 0; i < _tabNames.Length; i++)
        {
            var tabIndex = i;
            var button = CreateButton(tabBar.transform, _tabNames[i].ToUpperInvariant(), 13f, PanelDark, TextMuted);
            button.gameObject.AddComponent<LayoutElement>().preferredWidth = 78f;
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectTab(tabIndex)));
            _tabs.Add(button);
        }

        var scroll = CreateScroll(panel.transform, "TabScroll");
        scroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        _tabContent = scroll.content;

        BuildStatusBar(panel.transform);
    }

    private void BuildStatusBar(Transform parent)
    {
        var status = CreatePanel(parent, "Status", PanelDark, true);
        status.AddComponent<LayoutElement>().preferredHeight = 32f;
        var statusLayout = status.AddComponent<HorizontalLayoutGroup>();
        statusLayout.padding = new RectOffset(12, 12, 7, 7);
        statusLayout.spacing = 6f;
        statusLayout.childControlWidth = true;
        statusLayout.childControlHeight = true;

        var dot = CreatePanel(status.transform, "StatusDot", StatusGreen);
        dot.AddComponent<LayoutElement>().preferredWidth = 6f;
        _statusDot = dot.GetComponent<Image>();

        _notificationText = CreateLabel(status.transform, "", 11f, StatusGreen);
        _notificationText.fontStyle = FontStyles.Bold;
        _notificationText.enableWordWrapping = false;
        _notificationText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void BuildFooter(Transform parent)
    {
        var footer = new GameObject("FooterButtons");
        footer.transform.SetParent(parent, false);
        footer.AddComponent<RectTransform>();
        footer.AddComponent<Image>().color = PanelDark;
        var layout = footer.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleRight;
        footer.AddComponent<LayoutElement>().preferredHeight = 52f;

        var spacer = new GameObject("FooterSpacer");
        spacer.transform.SetParent(footer.transform, false);
        spacer.AddComponent<RectTransform>();
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

        _cancelButton = CreateButton(footer.transform, "> CANCEL", 13f, PanelDark, Text);
        _saveAsNewButton = CreateButton(footer.transform, "> SAVE AS NEW PRESET", 13f, PanelDark, Text);
        _saveButton = CreateButton(footer.transform, "> SAVE", 13f, ButtonColor, Text);
        _continueButton = CreateButton(footer.transform, "> CONTINUE", 13f, ButtonColor, Text);
        _cancelButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        _saveAsNewButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 220f;
        _saveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;
        _continueButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;
    }

    private void WireFooterButtons()
    {
        _continueButton.onClick.AddListener(new UnityEngine.Events.UnityAction(ContinueClicked));
        _cancelButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => CancelRequested?.Invoke()));
        _saveButton.onClick.AddListener(new UnityEngine.Events.UnityAction(SavePreset));
        _saveAsNewButton.onClick.AddListener(new UnityEngine.Events.UnityAction(SaveAsPreset));
    }

    private void SelectTab(int index)
    {
        if (_draft == null || index < 0 || index >= _tabNames.Length)
            return;

        _currentTabIndex = index;
        for (var i = 0; i < _tabs.Count; i++)
            StyleTab(_tabs[i], i == index);

        ClearChildren(_tabContent);
        switch (_tabNames[index].ToLowerInvariant())
        {
            case "lobby":
                BuildLobbyTab(_tabContent);
                break;
            case "game":
                BuildGameTab(_tabContent);
                break;
            case "items":
                BuildItemsTab(_tabContent);
                break;
            case "perks":
                BuildPerksTab(_tabContent);
                break;
            case "enemies":
                BuildEnemiesTab(_tabContent);
                break;
            case "moons":
                BuildMoonsTab(_tabContent);
                break;
        }
    }

    private static void StyleTab(Button button, bool active)
    {
        if (button.targetGraphic is Image image)
            image.color = active ? FromHex(0x1E0404, 1f) : PanelDark;
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.color = active ? Text : TextMuted;
            label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        }
    }

    private void RebuildPresets()
    {
        ClearChildren(_presetContainer);
        if (_model == null || _draft == null)
            return;

        foreach (var preset in _model.BuiltInPresets.Concat(_presetStore.LoadSummaries()))
            AddPresetButton(preset);
    }

    private void AddPresetButton(PresetSummary preset)
    {
        var button = CreateButton(_presetContainer, (IsSelected(preset) ? "> " : "") + preset.DisplayName, 13f, IsSelected(preset) ? FromHex(0x3A0808, 1f) : FromHex(0x1E0404, 1f), IsSelected(preset) ? Text : FromHex(0xC87030, 1f));
        button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectPreset(preset)));
        button.gameObject.AddComponent<LayoutElement>().preferredHeight = 33f;
    }

    private void SelectPreset(PresetSummary preset)
    {
        if (_draft == null)
            return;

        if (!_presetStore.TryApply(preset.Id, _draft, out var error))
        {
            ShowError(error);
            return;
        }

        _draft.Lobby.PresetId = preset.Id;
        ShowStatus("Preset selected: " + preset.DisplayName);
        RebuildPresets();
        SelectTab(_currentTabIndex);
    }

    private bool IsSelected(PresetSummary preset) =>
        _draft != null && string.Equals(_draft.Lobby.PresetId, preset.Id, StringComparison.OrdinalIgnoreCase);

    private void SaveAsPreset()
    {
        if (_draft == null)
            return;

        var name = CurrentPresetDisplayName();
        var summary = _presetStore.SaveAs(name, _draft);
        _draft.Lobby.PresetId = summary.Id;
        RebuildPresets();
        ShowStatus("Preset created.");
        SaveRequested?.Invoke(CurrentProfile());
    }

    private void SavePreset()
    {
        if (_draft == null)
            return;

        if (!_presetStore.IsCustom(_draft.Lobby.PresetId))
        {
            ShowStatus("Built-in presets cannot be overwritten. Use Save as new preset.");
            return;
        }

        _presetStore.Save(_draft.Lobby.PresetId, CurrentPresetDisplayName(), _draft);
        RebuildPresets();
        ShowStatus("Preset saved.");
        SaveRequested?.Invoke(CurrentProfile());
    }

    private string CurrentPresetDisplayName()
    {
        if (_draft == null)
            return "New preset";

        var selected = _model?.BuiltInPresets.Concat(_presetStore.LoadSummaries())
            .FirstOrDefault(preset => string.Equals(preset.Id, _draft.Lobby.PresetId, StringComparison.OrdinalIgnoreCase));
        return selected?.DisplayName ?? "New preset";
    }

    private void BuildLobbyTab(Transform page)
    {
        if (_draft == null)
            return;

        var lobby = AddContainer(page, "Lobby", "");
        AddNumeric(lobby, "Lobby size", Math.Max(1, _draft.Lobby.MaxPlayers), value => _draft.Lobby.MaxPlayers = Clamp(ParseInt(value, _draft.Lobby.MaxPlayers), 1, 64));

        var keepOpen = AddContainer(page, "Keep open - EXPERIMENTAL", "When activated your Steam lobby policy is managed by OverseerProtocol.");
        AddToggle(keepOpen, "Enabled", _draft.Lobby.EnableLateJoin, value => _draft.Lobby.EnableLateJoin = value);
        AddToggle(keepOpen, "Allow when in orbit", _draft.Lobby.LateJoinInOrbit, value => _draft.Lobby.LateJoinInOrbit = value);
        AddToggle(keepOpen, "Allow when landed", _draft.Lobby.LateJoinOnMoonAsSpectator, value => _draft.Lobby.LateJoinOnMoonAsSpectator = value);

        var compatibility = AddContainer(page, "Compatibility", "Host/client restrictions exposed in multiplayer status and handshake checks.");
        AddToggle(compatibility, "Enable multiplayer", _draft.Lobby.EnableMultiplayer, value => _draft.Lobby.EnableMultiplayer = value);
        AddToggle(compatibility, "Require same mod version", _draft.Lobby.RequireSameModVersion, value => _draft.Lobby.RequireSameModVersion = value);
        AddToggle(compatibility, "Require same config hash", _draft.Lobby.RequireSameConfigHash, value => _draft.Lobby.RequireSameConfigHash = value);
    }

    private void BuildGameTab(Transform page)
    {
        var runtime = AddContainer(page, "Runtime", "These switches control which Overseer tuning blocks are applied.");
        AddToggle(runtime, "Item edits", OPConfig.EnableItemOverrides.Value, value => OPConfig.EnableItemOverrides.Value = value);
        AddToggle(runtime, "Moon edits", OPConfig.EnableMoonOverrides.Value, value => OPConfig.EnableMoonOverrides.Value = value);
        AddToggle(runtime, "Enemy spawn edits", OPConfig.EnableSpawnOverrides.Value, value => OPConfig.EnableSpawnOverrides.Value = value);
        AddToggle(runtime, "Runtime multipliers", OPConfig.EnableRuntimeMultipliers.Value, value => OPConfig.EnableRuntimeMultipliers.Value = value);

        var multipliers = AddContainer(page, "Multipliers", "Global multipliers applied after explicit item, moon, and spawn edits.");
        AddNumeric(multipliers, "Item weight", OPConfig.ItemWeightMultiplier.Value, value => OPConfig.ItemWeightMultiplier.Value = Clamp(ParseFloat(value, OPConfig.ItemWeightMultiplier.Value), 0f, 10f));
        AddNumeric(multipliers, "Spawn rarity", OPConfig.SpawnRarityMultiplier.Value, value => OPConfig.SpawnRarityMultiplier.Value = Clamp(ParseFloat(value, OPConfig.SpawnRarityMultiplier.Value), 0f, 10f));
        AddNumeric(multipliers, "Route price", OPConfig.RoutePriceMultiplier.Value, value => OPConfig.RoutePriceMultiplier.Value = Clamp(ParseFloat(value, OPConfig.RoutePriceMultiplier.Value), 0f, 10f));
        AddNumeric(multipliers, "Travel discount", OPConfig.TravelDiscountMultiplier.Value, value => OPConfig.TravelDiscountMultiplier.Value = Clamp(ParseFloat(value, OPConfig.TravelDiscountMultiplier.Value), 0f, 10f));
    }

    private void BuildItemsTab(Transform page)
    {
        if (_draft == null)
            return;

        var container = AddContainer(page, "Store items", "Configure observed item values and runtime item edits.");
        if (_draft.Items.Count == 0)
        {
            AddText(container, "Catalog", "No item catalog is available yet.", _ => { }, true);
            return;
        }

        foreach (var item in _draft.Items)
        {
            var row = AddContainer(container, item.Baseline.DisplayName, "");
            AddToggle(row, "Enabled", item.Enabled, value => item.Enabled = value);
            AddNumeric(row, "Value", item.Value, value => item.Value = ParseInt(value, item.Value));
            AddNumeric(row, "Weight", item.Weight, value => item.Weight = ParseFloat(value, item.Weight));
        }
    }

    private void BuildPerksTab(Transform page)
    {
        var summary = new PerkMenuProvider().BuildReadOnlySummary();
        var container = AddContainer(page, "Perks", "Current perk state (read-only).");
        AddText(container, "Summary", summary, _ => { }, true);
    }

    private void BuildEnemiesTab(Transform page)
    {
        if (_draft == null)
            return;

        var sampleEnemyIds = _draft.EnemyIds.Take(3).ToList();
        var hint = sampleEnemyIds.Count > 0
            ? "Format: EnemyId:rarity, ... (known IDs: " + string.Join(", ", sampleEnemyIds) + (_draft.EnemyIds.Count > sampleEnemyIds.Count ? "..." : "") + ")"
            : "Format: EnemyId:rarity entries separated by commas.";

        foreach (var spawn in _draft.Spawns)
        {
            var container = AddContainer(page, spawn.Baseline.MoonId, hint);
            AddToggle(container, "Inside enabled", spawn.InsideEnabled, value => spawn.InsideEnabled = value);
            AddText(container, "Inside enemies", spawn.InsideEnemies, value => spawn.InsideEnemies = value);
            AddToggle(container, "Outside enabled", spawn.OutsideEnabled, value => spawn.OutsideEnabled = value);
            AddText(container, "Outside enemies", spawn.OutsideEnemies, value => spawn.OutsideEnemies = value);
            AddToggle(container, "Daytime enabled", spawn.DaytimeEnabled, value => spawn.DaytimeEnabled = value);
            AddText(container, "Daytime enemies", spawn.DaytimeEnemies, value => spawn.DaytimeEnemies = value);
        }

        if (_draft.Spawns.Count == 0)
        {
            var empty = AddContainer(page, "Enemies", "No spawn profiles loaded.");
            AddText(empty, "Status", "No spawn catalog available yet.", _ => { }, true);
        }
    }

    private void BuildMoonsTab(Transform page)
    {
        if (_draft == null)
            return;

        if (_draft.Moons.Count == 0)
        {
            var empty = AddContainer(page, "Moons", "No moon catalog loaded.");
            AddText(empty, "Status", "No moon data available yet.", _ => { }, true);
            return;
        }

        foreach (var moon in _draft.Moons)
        {
            var riskLabel = string.IsNullOrWhiteSpace(moon.RiskLabel) ? "None" : moon.RiskLabel;
            var description = string.IsNullOrWhiteSpace(moon.Description)
                ? "Risk: " + riskLabel
                : moon.Description + " [" + riskLabel + "]";
            var container = AddContainer(page, moon.Baseline.DisplayName, description);
            AddToggle(container, "Enabled", moon.Enabled, value => moon.Enabled = value);
            AddNumeric(container, "Route price", moon.RoutePrice, value => moon.RoutePrice = ParseInt(value, moon.RoutePrice));
            AddNumeric(container, "Min scrap", moon.MinScrap, value => moon.MinScrap = ParseInt(value, moon.MinScrap));
            AddNumeric(container, "Max scrap", moon.MaxScrap, value => moon.MaxScrap = ParseInt(value, moon.MaxScrap));
        }
    }

    private Transform AddContainer(Transform parent, string title, string description)
    {
        var panel = CreatePanel(parent, "Section_" + Sanitize(title), SectionColor, true);
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.AddComponent<LayoutElement>().preferredHeight = string.IsNullOrWhiteSpace(description) ? 0f : -1f;

        var header = CreatePanel(panel.transform, "SectionHeader", SectionHeaderColor, true);
        header.AddComponent<LayoutElement>().preferredHeight = 28f;
        var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(10, 10, 6, 6);
        headerLayout.spacing = 6f;
        headerLayout.childControlHeight = true;
        headerLayout.childControlWidth = true;
        headerLayout.childForceExpandWidth = false;

        var dot = CreatePanel(header.transform, "Dot", Accent);
        dot.AddComponent<LayoutElement>().preferredWidth = 5f;

        var titleLabel = CreateLabel(header.transform, title.ToUpperInvariant(), 12f, Text);
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.characterSpacing = 5f;
        titleLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var descLabel = CreateLabel(header.transform, string.IsNullOrWhiteSpace(description) ? "" : ShortDescription(description), 10f, TextDim);
        descLabel.alignment = TextAlignmentOptions.MidlineRight;
        descLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

        var body = new GameObject("SectionBody");
        body.transform.SetParent(panel.transform, false);
        body.AddComponent<RectTransform>();
        var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(10, 10, 8, 8);
        bodyLayout.spacing = 5f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        return body.transform;
    }

    private void AddToggle(Transform parent, string label, bool value, Action<bool> changed)
    {
        var row = CreateRow(parent, "Toggle_" + Sanitize(label), 26f);
        var text = CreateLabel(row.transform, label, 12f, TextMuted);
        text.fontStyle = FontStyles.Bold;
        text.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;

        var box = CreatePanel(row.transform, "Box", Panel);
        box.AddComponent<LayoutElement>().preferredWidth = 28f;
        var toggle = box.AddComponent<Toggle>();
        toggle.targetGraphic = box.GetComponent<Image>();
        var check = CreatePanel(box.transform, "Checkmark", ButtonColor);
        var checkRect = (RectTransform)check.transform;
        checkRect.anchorMin = new Vector2(0.18f, 0.18f);
        checkRect.anchorMax = new Vector2(0.82f, 0.82f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;
        toggle.graphic = check.GetComponent<Image>();
        toggle.SetIsOnWithoutNotify(value);
        toggle.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(changed));
    }

    private void AddNumeric(Transform parent, string label, int value, Action<string> changed) =>
        AddText(parent, label, value.ToString(CultureInfo.InvariantCulture), changed);

    private void AddNumeric(Transform parent, string label, float value, Action<string> changed) =>
        AddText(parent, label, value.ToString(CultureInfo.InvariantCulture), changed);

    private void AddText(Transform parent, string label, string value, Action<string> changed, bool multiline = false)
    {
        var row = CreateRow(parent, "Input_" + Sanitize(label), multiline ? 82f : 26f);
        var text = CreateLabel(row.transform, label + ":", 12f, TextMuted);
        text.fontStyle = FontStyles.Bold;
        text.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
        var input = CreateInput(row.transform, value, multiline);
        input.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(changed));
    }

    private TMP_InputField CreateInput(Transform parent, string value, bool multiline)
    {
        var root = CreatePanel(parent, "InputField", Panel);
        var layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = multiline ? -1f : 90f;
        layout.flexibleWidth = multiline ? 1f : 0f;
        var input = root.AddComponent<TMP_InputField>();
        input.targetGraphic = root.GetComponent<Image>();
        input.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(root.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(6f, 2f);
        viewportRect.offsetMax = new Vector2(-6f, -2f);
        viewport.AddComponent<RectMask2D>();
        input.textViewport = viewportRect;

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(viewport.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 12f;
        text.color = Text;
        text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = multiline;
        input.textComponent = text;
        input.SetTextWithoutNotify(value ?? "");
        return input;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color, bool outline = false)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        var image = obj.AddComponent<Image>();
        image.color = color;
        image.type = Image.Type.Sliced;
        if (outline)
        {
            var line = obj.AddComponent<Outline>();
            line.effectColor = BorderColor;
            line.effectDistance = new Vector2(1f, -1f);
        }
        return obj;
    }

    private static GameObject CreateRow(Transform parent, string name, float height)
    {
        var row = new GameObject(name);
        row.transform.SetParent(parent, false);
        row.AddComponent<RectTransform>();
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        row.AddComponent<LayoutElement>().preferredHeight = height;
        return row;
    }

    private static Button CreateButton(Transform parent, string text, float size, Color color) =>
        CreateButton(parent, text, size, color, Text);

    private static Button CreateButton(Transform parent, string text, float size, Color color, Color textColor)
    {
        var obj = CreatePanel(parent, "Button_" + Sanitize(text), color);
        var button = obj.AddComponent<Button>();
        button.targetGraphic = obj.GetComponent<Image>();
        var label = CreateLabel(obj.transform, text, size, textColor);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 4f;
        label.alignment = TextAlignmentOptions.Center;
        var rect = (RectTransform)label.transform;
        Stretch(rect, 0f);
        return button;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Color color)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        var label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        return label;
    }

    private static ScrollRect CreateScroll(Transform parent, string name)
    {
        var root = CreatePanel(parent, name, new Color(0f, 0f, 0f, 0.18f));
        var scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(root.transform, false);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(12f, 12f);
        viewportRect.offsetMax = new Vector2(-12f, -12f);
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        viewport.AddComponent<RectMask2D>();

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        return scroll;
    }

    private void ContinueClicked()
    {
        if (_busy || _draft == null)
            return;

        if (_waitingForWarningConfirmation)
            WarningsConfirmed?.Invoke(CurrentProfile());
        else
            ContinueRequested?.Invoke(CurrentProfile());
    }

    private OverseerHostProfile CurrentProfile()
    {
        var presetId = _draft?.Lobby.PresetId ?? OPConfig.DefaultPreset;
        return new OverseerHostProfile(presetId, _draft ?? TuningDraftStore.Load());
    }

    private static void ClearChildren(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static void SetButtonText(Button button, string text)
    {
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
    }

    private static string Sanitize(string value) =>
        new(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    private static string ShortDescription(string description)
    {
        if (description.IndexOf("experimental", StringComparison.OrdinalIgnoreCase) >= 0)
            return "EXPERIMENTAL";
        if (description.IndexOf("handshake", StringComparison.OrdinalIgnoreCase) >= 0)
            return "HANDSHAKE";
        if (description.IndexOf("read-only", StringComparison.OrdinalIgnoreCase) >= 0)
            return "READ-ONLY";
        if (description.IndexOf("known IDs", StringComparison.OrdinalIgnoreCase) >= 0)
            return "SPAWN LIST";
        if (description.IndexOf("multipliers", StringComparison.OrdinalIgnoreCase) >= 0)
            return "GLOBAL SCALE";
        if (description.IndexOf("switches", StringComparison.OrdinalIgnoreCase) >= 0)
            return "APPLY BLOCKS";
        if (description.StartsWith("Risk:", StringComparison.OrdinalIgnoreCase))
            return description.ToUpperInvariant();
        return "CONFIG";
    }

    private static Color FromHex(int rgb, float alpha)
    {
        var r = ((rgb >> 16) & 0xFF) / 255f;
        var g = ((rgb >> 8) & 0xFF) / 255f;
        var b = (rgb & 0xFF) / 255f;
        return new Color(r, g, b, alpha);
    }

    private static int ParseInt(string value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static float ParseFloat(string value, float fallback) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

    private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
}
