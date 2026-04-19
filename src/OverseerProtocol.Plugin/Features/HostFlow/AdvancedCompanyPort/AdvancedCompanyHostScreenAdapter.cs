using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using OverseerProtocol.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OverseerProtocol.Features.HostFlow.AdvancedCompanyPort;

internal sealed class AdvancedCompanyHostScreenAdapter : MonoBehaviour, IOverseerHostScreen
{
    private readonly OverseerHostPresetStore _presetStore = new();
    private OverseerHostReadModel? _model;
    private TuningDraftStore? _draft;
    private RectTransform _presetList = null!;
    private RectTransform _content = null!;
    private TMP_InputField _presetName = null!;
    private TextMeshProUGUI _status = null!;
    private TextMeshProUGUI _title = null!;
    private Button _continueButton = null!;
    private Button _cancelButton = null!;
    private Button _saveButton = null!;
    private Button _renameButton = null!;
    private Button _deleteButton = null!;
    private readonly List<Button> _tabs = new();
    private string _activeTab = "Lobby";
    private bool _waitingForWarningConfirmation;
    private bool _busy;

    public event Action<OverseerHostProfile>? ContinueRequested;
    public event Action<OverseerHostProfile>? WarningsConfirmed;
    public event Action? CancelRequested;
    public event Action<OverseerHostProfile>? SaveRequested;

    public void Initialize(GameObject? advancedCompanyPrefab)
    {
        var rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var overlay = gameObject.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0f, 0f, 0.78f);

        if (advancedCompanyPrefab != null)
        {
            var probe = Instantiate(advancedCompanyPrefab, transform);
            probe.name = "AdvancedCompanyLobbyScreenPrefabProbe";
            probe.SetActive(false);
        }

        BuildShell();
        gameObject.SetActive(false);
    }

    public void Open(OverseerHostReadModel model)
    {
        _model = model;
        _draft = model.InitialDraft.Draft;
        _waitingForWarningConfirmation = false;
        _busy = false;
        _activeTab = "Lobby";
        _continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "> Continue";
        _title.text = "Lobby";
        _presetName.text = model.ActivePresetId;
        RebuildPresets();
        RebuildTabs();
        RebuildContent();
        ShowStatus(model.InitialWarnings.Count == 0 ? "Ready." : string.Join("\n", model.InitialWarnings));
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close() => gameObject.SetActive(false);

    public void SetBusy(bool busy)
    {
        _busy = busy;
        _continueButton.interactable = !busy;
        _cancelButton.interactable = !busy;
        _saveButton.interactable = !busy;
        _renameButton.interactable = !busy;
        _deleteButton.interactable = !busy;
    }

    public void ShowError(string message)
    {
        _waitingForWarningConfirmation = false;
        _continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "> Continue";
        ShowStatus(message);
    }

    public void ShowStatus(string message)
    {
        _status.text = message;
    }

    public void ShowWarnings(IReadOnlyList<string> warnings)
    {
        _waitingForWarningConfirmation = true;
        _continueButton.GetComponentInChildren<TextMeshProUGUI>().text = "> Confirm";
        ShowStatus(warnings.Count == 0 ? "Confirm warnings to continue." : string.Join("\n", warnings));
    }

    private void BuildShell()
    {
        var root = CreatePanel("AdvancedCompanyPortRoot", transform, new Color(0.12f, 0f, 0f, 0.78f));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.08f, 0.12f);
        rootRect.anchorMax = new Vector2(0.92f, 0.94f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        var rootLayout = root.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(8, 8, 8, 8);
        rootLayout.spacing = 8f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;

        var body = CreateObject("Body", root.transform);
        body.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 10f;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandHeight = true;
        bodyLayout.childControlWidth = true;
        bodyLayout.childForceExpandWidth = false;

        BuildPresetSidebar(body.transform);
        BuildMainPanel(body.transform);
        BuildFooter(root.transform);
    }

    private void BuildPresetSidebar(Transform parent)
    {
        var panel = CreatePanel("Presets", parent, new Color(0.16f, 0f, 0f, 0.92f));
        panel.AddComponent<LayoutElement>().preferredWidth = 240f;
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 10, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;

        CreateLabel(panel.transform, "Presets", 21, AcOrange(), 30f, TextAlignmentOptions.Left);
        _presetName = CreateInput(panel.transform, "Preset name", 30f, _ => { });

        var scroll = CreateScroll(panel.transform);
        scroll.AddComponent<LayoutElement>().flexibleHeight = 1f;
        _presetList = scroll.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();

        var buttons = CreateObject("PresetButtons", panel.transform);
        buttons.AddComponent<LayoutElement>().preferredHeight = 100f;
        var buttonLayout = buttons.AddComponent<VerticalLayoutGroup>();
        buttonLayout.spacing = 4f;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandHeight = true;
        buttonLayout.childControlWidth = true;
        buttonLayout.childForceExpandWidth = true;

        CreateButton(buttons.transform, "> Create new", SaveAsPreset, 28f);
        _saveButton = CreateButton(buttons.transform, "> Save", SavePreset, 28f);
        _renameButton = CreateButton(buttons.transform, "> Rename", RenamePreset, 28f);
        _deleteButton = CreateButton(buttons.transform, "> Delete", DeletePreset, 28f);
    }

    private void BuildMainPanel(Transform parent)
    {
        var panel = CreatePanel("Main", parent, new Color(0.14f, 0f, 0f, 0.92f));
        panel.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 0, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;

        var tabBar = CreateObject("Tabs", panel.transform);
        tabBar.AddComponent<LayoutElement>().preferredHeight = 42f;
        var tabsLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 0f;
        tabsLayout.childControlWidth = false;
        tabsLayout.childForceExpandWidth = false;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandHeight = true;

        foreach (var tab in new[] { "Lobby", "Items", "Perks", "Enemies", "Moons" })
        {
            var localTab = tab;
            _tabs.Add(CreateButton(tabBar.transform, tab, () =>
            {
                _activeTab = localTab;
                RebuildTabs();
                RebuildContent();
            }, 104f, 40f));
        }

        _title = CreateLabel(panel.transform, "Lobby", 24, AcOrange(), 36f, TextAlignmentOptions.Left);
        var scroll = CreateScroll(panel.transform);
        scroll.AddComponent<LayoutElement>().flexibleHeight = 1f;
        _content = scroll.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
        _status = CreateLabel(panel.transform, "Ready.", 14, new Color(1f, 0.62f, 0.26f), 52f, TextAlignmentOptions.Left);
    }

    private void BuildFooter(Transform parent)
    {
        var footer = CreateObject("Footer", parent);
        footer.AddComponent<LayoutElement>().preferredHeight = 44f;
        var layout = footer.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;

        var spacer = CreateObject("Spacer", footer.transform);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _cancelButton = CreateButton(footer.transform, "> Cancel", () => CancelRequested?.Invoke(), 130f, 40f);
        _continueButton = CreateButton(footer.transform, "> Continue", ContinueClicked, 160f, 40f);
    }

    private void RebuildPresets()
    {
        Clear(_presetList);
        if (_model == null || _draft == null)
            return;

        foreach (var preset in _model.BuiltInPresets.Concat(_presetStore.LoadSummaries()))
        {
            var localPreset = preset;
            var button = CreateButton(_presetList, preset.DisplayName, () => SelectPreset(localPreset), 184f, 30f);
            button.GetComponent<Image>().color = IsSelected(preset)
                ? new Color(0.48f, 0.04f, 0.02f, 0.98f)
                : new Color(0.18f, 0.01f, 0f, 0.85f);
            button.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        }
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
        _presetName.text = preset.DisplayName;
        ShowStatus("Preset selected: " + preset.DisplayName);
        RebuildPresets();
        RebuildContent();
    }

    private bool IsSelected(PresetSummary preset) =>
        _draft != null && string.Equals(_draft.Lobby.PresetId, preset.Id, StringComparison.OrdinalIgnoreCase);

    private void SaveAsPreset()
    {
        if (_draft == null)
            return;

        var summary = _presetStore.SaveAs(_presetName.text, _draft);
        _draft.Lobby.PresetId = summary.Id;
        _presetName.text = summary.DisplayName;
        ShowStatus("Preset created.");
        RebuildPresets();
        SaveRequested?.Invoke(CurrentProfile());
    }

    private void SavePreset()
    {
        if (_draft == null)
            return;

        if (!_presetStore.IsCustom(_draft.Lobby.PresetId))
        {
            ShowStatus("Built-in presets cannot be overwritten. Use Create new.");
            return;
        }

        _presetStore.Save(_draft.Lobby.PresetId, _presetName.text, _draft);
        ShowStatus("Preset saved.");
        RebuildPresets();
        SaveRequested?.Invoke(CurrentProfile());
    }

    private void RenamePreset()
    {
        if (_draft == null)
            return;

        if (!_presetStore.IsCustom(_draft.Lobby.PresetId))
        {
            ShowStatus("Built-in presets cannot be renamed.");
            return;
        }

        var summary = _presetStore.Rename(_draft.Lobby.PresetId, _presetName.text, _draft);
        _draft.Lobby.PresetId = summary.Id;
        _presetName.text = summary.DisplayName;
        ShowStatus("Preset renamed.");
        RebuildPresets();
    }

    private void DeletePreset()
    {
        if (_draft == null)
            return;

        if (!_presetStore.IsCustom(_draft.Lobby.PresetId))
        {
            ShowStatus("Built-in presets cannot be deleted.");
            return;
        }

        _presetStore.Delete(_draft.Lobby.PresetId);
        var fallback = LobbyPresetDefinition.Resolve(OPConfig.DefaultPreset);
        _draft.ApplyPreset(fallback);
        _presetName.text = fallback.DisplayName;
        ShowStatus("Preset deleted.");
        RebuildPresets();
        RebuildContent();
    }

    private void RebuildTabs()
    {
        foreach (var button in _tabs)
        {
            var active = button.GetComponentInChildren<TextMeshProUGUI>().text == _activeTab;
            button.GetComponent<Image>().color = active ? new Color(0.25f, 0.02f, 0.01f, 1f) : new Color(0.08f, 0f, 0f, 0.95f);
            button.GetComponentInChildren<TextMeshProUGUI>().fontSize = active ? 22f : 18f;
        }
    }

    private void RebuildContent()
    {
        Clear(_content);
        _title.text = _activeTab;
        if (_draft == null)
            return;

        switch (_activeTab)
        {
            case "Lobby":
                BuildLobbyTab();
                break;
            case "Items":
                BuildItemsTab();
                break;
            case "Perks":
                BuildPerksTab();
                break;
            case "Enemies":
                BuildEnemiesTab();
                break;
            case "Moons":
                BuildMoonsTab();
                break;
        }
    }

    private void BuildLobbyTab()
    {
        if (_draft == null)
            return;

        AddSection("Lobby", "");
        AddIntRow("Lobby size", _draft.Lobby.MaxPlayers, 1, 64, value => _draft.Lobby.MaxPlayers = value);
        AddSection("Keep open - EXPERIMENTAL", "Late join policy is applied by OverseerProtocol after host start.");
        AddToggleRow("Enabled", _draft.Lobby.EnableLateJoin, value => _draft.Lobby.EnableLateJoin = value);
        AddToggleRow("Allow when in orbit", _draft.Lobby.LateJoinInOrbit, value => _draft.Lobby.LateJoinInOrbit = value);
        AddToggleRow("Allow when landed", _draft.Lobby.LateJoinOnMoonAsSpectator, value => _draft.Lobby.LateJoinOnMoonAsSpectator = value);
        AddSection("Compatibility", "Host/client restrictions exposed in multiplayer status and handshake checks.");
        AddToggleRow("Enable multiplayer", _draft.Lobby.EnableMultiplayer, value => _draft.Lobby.EnableMultiplayer = value);
        AddToggleRow("Require same mod version", _draft.Lobby.RequireSameModVersion, value => _draft.Lobby.RequireSameModVersion = value);
        AddToggleRow("Require same config hash", _draft.Lobby.RequireSameConfigHash, value => _draft.Lobby.RequireSameConfigHash = value);
    }

    private void BuildItemsTab()
    {
        if (_draft == null)
            return;

        AddSection("Store items", "Configure observed item values and runtime item edits.");
        foreach (var item in _draft.Items)
        {
            AddCompactRow(item.Baseline.DisplayName, row =>
            {
                AddToggle(row, "Enabled", item.Enabled, value => item.Enabled = value);
                AddMiniInput(row, item.Value.ToString(CultureInfo.InvariantCulture), value => item.Value = ParseInt(value, item.Value));
                AddMiniInput(row, item.Weight.ToString(CultureInfo.InvariantCulture), value => item.Weight = ParseFloat(value, item.Weight));
            });
        }
    }

    private void BuildPerksTab()
    {
        AddSection("Perks", "Runtime perk catalogs stay owned by OverseerProtocol. This tab is reserved for the AssetBundle parity pass.");
        CreateLabel(_content, "Player and ship perks are currently available through the Overseer panel/runtime catalog. Host-screen perk editing will map onto the existing perk services in the next pass.", 15, AcText(), 90f, TextAlignmentOptions.Left);
    }

    private void BuildEnemiesTab()
    {
        if (_draft == null)
            return;

        AddSection("Enemies", "Enemy spawn lists use EnemyId:rarity entries separated by commas.");
        foreach (var spawn in _draft.Spawns)
        {
            AddSection(spawn.Baseline.MoonId, spawn.Baseline.MoonId);
            AddToggleRow("Inside enabled", spawn.InsideEnabled, value => spawn.InsideEnabled = value);
            AddTextRow("Inside enemies", spawn.InsideEnemies, value => spawn.InsideEnemies = value);
            AddToggleRow("Outside enabled", spawn.OutsideEnabled, value => spawn.OutsideEnabled = value);
            AddTextRow("Outside enemies", spawn.OutsideEnemies, value => spawn.OutsideEnemies = value);
            AddToggleRow("Daytime enabled", spawn.DaytimeEnabled, value => spawn.DaytimeEnabled = value);
            AddTextRow("Daytime enemies", spawn.DaytimeEnemies, value => spawn.DaytimeEnemies = value);
        }
    }

    private void BuildMoonsTab()
    {
        if (_draft == null)
            return;

        AddSection("Moons", "Configure observed moons, route price and scrap ranges.");
        foreach (var moon in _draft.Moons)
        {
            AddCompactRow(moon.Baseline.DisplayName, row =>
            {
                AddToggle(row, "Enabled", moon.Enabled, value => moon.Enabled = value);
                AddMiniInput(row, moon.RoutePrice.ToString(CultureInfo.InvariantCulture), value => moon.RoutePrice = ParseInt(value, moon.RoutePrice));
                AddMiniInput(row, moon.MinScrap.ToString(CultureInfo.InvariantCulture), value => moon.MinScrap = ParseInt(value, moon.MinScrap));
                AddMiniInput(row, moon.MaxScrap.ToString(CultureInfo.InvariantCulture), value => moon.MaxScrap = ParseInt(value, moon.MaxScrap));
            });
        }
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

    private void AddSection(string title, string description)
    {
        var section = CreateObject("Section", _content);
        section.AddComponent<LayoutElement>().preferredHeight = string.IsNullOrWhiteSpace(description) ? 34f : 74f;
        var layout = section.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.spacing = 2f;
        CreateLabel(section.transform, title, 21, AcOrange(), 28f, TextAlignmentOptions.Left);
        if (!string.IsNullOrWhiteSpace(description))
            CreateLabel(section.transform, description, 13, AcText(), 36f, TextAlignmentOptions.Left);
    }

    private void AddToggleRow(string label, bool value, Action<bool> changed)
    {
        AddCompactRow(label, row => AddToggle(row, "", value, changed));
    }

    private void AddIntRow(string label, int value, int min, int max, Action<int> changed)
    {
        AddCompactRow(label, row => AddMiniInput(row, value.ToString(CultureInfo.InvariantCulture), text =>
        {
            var parsed = Math.Max(min, Math.Min(max, ParseInt(text, value)));
            changed(parsed);
        }));
    }

    private void AddTextRow(string label, string value, Action<string> changed)
    {
        AddCompactRow(label, row =>
        {
            var input = CreateInput(row, value, 30f, changed);
            input.GetComponent<LayoutElement>().preferredWidth = 480f;
        });
    }

    private void AddCompactRow(string label, Action<Transform> buildRight)
    {
        var row = CreateObject("Row", _content);
        row.AddComponent<LayoutElement>().preferredHeight = 34f;
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        CreateLabel(row.transform, label, 14, AcText(), 30f, TextAlignmentOptions.Left).GetComponent<LayoutElement>().preferredWidth = 260f;
        buildRight(row.transform);
    }

    private static Toggle AddToggle(Transform parent, string label, bool value, Action<bool> changed)
    {
        var container = CreateObject("Toggle", parent);
        container.AddComponent<LayoutElement>().preferredWidth = string.IsNullOrEmpty(label) ? 34f : 126f;
        var layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5f;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        var box = CreatePanel("Box", container.transform, new Color(0.65f, 0.22f, 0.02f, 1f));
        box.AddComponent<LayoutElement>().preferredWidth = 24f;
        var toggle = box.AddComponent<Toggle>();
        toggle.isOn = value;
        toggle.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(changed));
        if (!string.IsNullOrEmpty(label))
            CreateLabel(container.transform, label, 13, AcText(), 24f, TextAlignmentOptions.Left);
        return toggle;
    }

    private static TMP_InputField AddMiniInput(Transform parent, string value, Action<string> changed)
    {
        var input = CreateInput(parent, value, 28f, changed);
        input.GetComponent<LayoutElement>().preferredWidth = 72f;
        return input;
    }

    private static GameObject CreateScroll(Transform parent)
    {
        var scrollObj = CreateObject("ScrollView", parent);
        var image = scrollObj.AddComponent<Image>();
        image.color = new Color(0.05f, 0f, 0f, 0.35f);
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var viewport = CreateObject("Viewport", scrollObj.transform);
        var viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewportRect;

        var content = CreateObject("Content", viewport.transform);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        var layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
        scroll.content = contentRect;
        return scrollObj;
    }

    private static TMP_InputField CreateInput(Transform parent, string value, float height, Action<string> changed)
    {
        var obj = CreatePanel("Input", parent, new Color(0.44f, 0.07f, 0.02f, 0.94f));
        obj.AddComponent<LayoutElement>().preferredHeight = height;
        var textObj = CreateObject("Text", obj.transform);
        var rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(6f, 0f);
        rect.offsetMax = new Vector2(-6f, 0f);
        var text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = 14f;
        text.color = new Color(1f, 0.67f, 0.30f);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        var input = obj.AddComponent<TMP_InputField>();
        input.textComponent = text;
        input.text = value;
        input.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(changed));
        return input;
    }

    private static Button CreateButton(Transform parent, string label, Action clicked, float width, float height = 30f)
    {
        var obj = CreatePanel(label + "Button", parent, new Color(0.48f, 0.04f, 0.02f, 0.96f));
        obj.AddComponent<LayoutElement>().preferredWidth = width;
        obj.GetComponent<LayoutElement>().preferredHeight = height;
        var button = obj.AddComponent<Button>();
        button.onClick.AddListener(new UnityEngine.Events.UnityAction(clicked));
        CreateLabel(obj.transform, label, 15, AcOrange(), height, TextAlignmentOptions.Center);
        return button;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string text, float size, Color color, float height, TextAlignmentOptions align)
    {
        var obj = CreateObject("Label", parent);
        obj.AddComponent<LayoutElement>().preferredHeight = height;
        var label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = align;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static GameObject CreatePanel(string name, Transform parent, Color color)
    {
        var obj = CreateObject(name, parent);
        obj.AddComponent<RectTransform>();
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static void Clear(Transform parent)
    {
        for (var i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static int ParseInt(string value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static float ParseFloat(string value, float fallback) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static Color AcOrange() => new(1f, 0.38f, 0.08f);

    private static Color AcText() => new(1f, 0.58f, 0.26f);
}
