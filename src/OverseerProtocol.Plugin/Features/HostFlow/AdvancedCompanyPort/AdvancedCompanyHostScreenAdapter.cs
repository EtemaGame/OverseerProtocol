using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TMPro;
using OverseerProtocol.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OverseerProtocol.Features.HostFlow.AdvancedCompanyPort;

internal sealed class AdvancedCompanyHostScreenAdapter : MonoBehaviour, IOverseerHostScreen
{
    private readonly OverseerHostPresetStore _presetStore = new();
    private readonly List<Button> _tabs = new();
    private readonly List<GameObject> _tabPages = new();
    private GameObject _screenRoot = null!;
    private Component _settings = null!;
    private Transform _presetContainer = null!;
    private GameObject _presetTemplate = null!;
    private Button _continueButton = null!;
    private Button _cancelButton = null!;
    private Button _saveButton = null!;
    private Button _saveAsNewButton = null!;
    private GameObject? _notificationObject;
    private TextMeshProUGUI? _notificationText;
    private TuningDraftStore? _draft;
    private OverseerHostReadModel? _model;
    private bool _waitingForWarningConfirmation;
    private bool _busy;

    public event Action<OverseerHostProfile>? ContinueRequested;
    public event Action<OverseerHostProfile>? WarningsConfirmed;
    public event Action? CancelRequested;
    public event Action<OverseerHostProfile>? SaveRequested;

    public void Initialize(GameObject? advancedCompanyPrefab)
    {
        if (advancedCompanyPrefab == null)
            throw new InvalidOperationException("AdvancedCompany lobby screen prefab is missing.");

        var rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        _screenRoot = Instantiate(advancedCompanyPrefab, transform);
        _screenRoot.name = "AdvancedCompanyLobbyScreen";
        _screenRoot.transform.localScale = Vector3.one;
        _settings = FindComponent(_screenRoot, "LobbySettings")
            ?? throw new InvalidOperationException("LobbyScreen prefab does not contain LobbySettings.");

        _presetContainer = GetField<Transform>(_settings, "PresetContainer")
            ?? throw new InvalidOperationException("LobbySettings.PresetContainer is missing.");
        _presetTemplate = GetField<GameObject>(_settings, "PresetTemplate")
            ?? throw new InvalidOperationException("LobbySettings.PresetTemplate is missing.");
        _continueButton = GetField<Button>(_settings, "ContinueButton")
            ?? throw new InvalidOperationException("LobbySettings.ContinueButton is missing.");
        _cancelButton = GetField<Button>(_settings, "CancelButton")
            ?? throw new InvalidOperationException("LobbySettings.CancelButton is missing.");
        _saveButton = GetField<Button>(_settings, "SaveButton")
            ?? throw new InvalidOperationException("LobbySettings.SaveButton is missing.");
        _saveAsNewButton = GetField<Button>(_settings, "SaveAsNewPresetButton")
            ?? throw new InvalidOperationException("LobbySettings.SaveAsNewPresetButton is missing.");
        _notificationObject = GetField<GameObject>(_settings, "NotificationObject");
        _notificationText = GetField<TextMeshProUGUI>(_settings, "NotificationText");

        WireTabs();
        WireFooterButtons();
        _screenRoot.SetActive(false);
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
        if (_notificationObject != null)
            _notificationObject.SetActive(true);
        if (_notificationText != null)
            _notificationText.text = message;
    }

    public void ShowWarnings(IReadOnlyList<string> warnings)
    {
        _waitingForWarningConfirmation = true;
        SetButtonText(_continueButton, "> Confirm");
        ShowStatus(warnings.Count == 0 ? "Confirm warnings to continue." : string.Join("\n", warnings));
    }

    private void WireTabs()
    {
        var tabContainer = GetField<Transform>(_settings, "TabContainer");
        var pageContainer = GetField<Transform>(_settings, "Container");
        if (tabContainer == null || pageContainer == null)
            return;

        for (var i = 0; i < pageContainer.childCount; i++)
            _tabPages.Add(pageContainer.GetChild(i).gameObject);

        for (var i = 0; i < tabContainer.childCount; i++)
        {
            var button = tabContainer.GetChild(i).GetComponent<Button>();
            if (button == null)
                continue;

            var index = _tabs.Count;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectTab(index)));
            _tabs.Add(button);
        }
    }

    private void WireFooterButtons()
    {
        _continueButton.onClick.RemoveAllListeners();
        _continueButton.onClick.AddListener(new UnityEngine.Events.UnityAction(ContinueClicked));
        _cancelButton.onClick.RemoveAllListeners();
        _cancelButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => CancelRequested?.Invoke()));
        _saveButton.onClick.RemoveAllListeners();
        _saveButton.onClick.AddListener(new UnityEngine.Events.UnityAction(SavePreset));
        _saveAsNewButton.onClick.RemoveAllListeners();
        _saveAsNewButton.onClick.AddListener(new UnityEngine.Events.UnityAction(SaveAsPreset));
    }

    private void SelectTab(int index)
    {
        if (_draft == null)
            return;

        for (var i = 0; i < _tabPages.Count; i++)
            _tabPages[i].SetActive(i == index);

        for (var i = 0; i < _tabs.Count; i++)
            StyleTab(_tabs[i], i == index);

        if (index < _tabPages.Count)
            RebuildTabPage(index, _tabPages[index].transform);
    }

    private static void StyleTab(Button button, bool active)
    {
        if (button.targetGraphic is Image image)
            image.color = active ? new Color(0.30f, 0.02f, 0.01f, 0.95f) : new Color(0.08f, 0f, 0f, 0.95f);

        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.color = active ? new Color(254f / 255f, 101f / 255f, 22f / 255f) : new Color(176f / 255f, 69f / 255f, 14f / 255f);
            label.fontSize = active ? 25f : 20f;
        }

        var layout = button.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
            layout.padding = active ? new RectOffset(20, 20, 0, 0) : new RectOffset(10, 10, 10, 0);
    }

    private void RebuildTabPage(int index, Transform page)
    {
        ClearRuntimeChildren(page);
        var tabName = GetTabName(index);
        switch (tabName)
        {
            case "Lobby":
                BuildLobbyTab(page);
                break;
            case "Items":
                BuildItemsTab(page);
                break;
            case "Perks":
                BuildPerksTab(page);
                break;
            case "Enemies":
                BuildEnemiesTab(page);
                break;
            case "Moons":
                BuildMoonsTab(page);
                break;
        }
    }

    private string GetTabName(int index)
    {
        if (index < _tabs.Count)
        {
            var text = _tabs[index].GetComponentInChildren<TextMeshProUGUI>()?.text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return index switch
        {
            0 => "Lobby",
            1 => "Items",
            2 => "Perks",
            3 => "Enemies",
            4 => "Moons",
            _ => ""
        };
    }

    private void RebuildPresets()
    {
        ClearPresetContainer();
        if (_model == null || _draft == null)
            return;

        foreach (var preset in _model.BuiltInPresets.Concat(_presetStore.LoadSummaries()))
            AddPresetButton(preset);
    }

    private void AddPresetButton(PresetSummary preset)
    {
        var obj = Instantiate(_presetTemplate, _presetContainer);
        obj.name = "OverseerPreset_" + preset.Id;
        obj.SetActive(true);
        if (obj.transform.childCount > 0)
            obj.transform.GetChild(0).gameObject.SetActive(IsSelected(preset));

        if (obj.transform.childCount > 1 && obj.transform.GetChild(1).TryGetComponent<TextMeshProUGUI>(out var label))
            label.text = preset.DisplayName;

        var mainButton = obj.GetComponent<Button>();
        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
            mainButton.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectPreset(preset)));
        }

        if (obj.transform.childCount > 2 && obj.transform.GetChild(2).TryGetComponent<Button>(out var remove))
        {
            remove.gameObject.SetActive(!preset.IsBuiltIn);
            remove.onClick.RemoveAllListeners();
            remove.onClick.AddListener(new UnityEngine.Events.UnityAction(DeletePreset));
        }

        if (obj.transform.childCount > 3 && obj.transform.GetChild(3).TryGetComponent<Button>(out var rename))
        {
            rename.gameObject.SetActive(!preset.IsBuiltIn);
            rename.onClick.RemoveAllListeners();
            rename.onClick.AddListener(new UnityEngine.Events.UnityAction(RenamePreset));
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
        ShowStatus("Preset selected: " + preset.DisplayName);
        RebuildPresets();
        SelectTab(CurrentTabIndex());
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

    private void RenamePreset()
    {
        if (_draft == null)
            return;

        if (!_presetStore.IsCustom(_draft.Lobby.PresetId))
        {
            ShowStatus("Built-in presets cannot be renamed.");
            return;
        }

        var summary = _presetStore.Rename(_draft.Lobby.PresetId, CurrentPresetDisplayName(), _draft);
        _draft.Lobby.PresetId = summary.Id;
        RebuildPresets();
        ShowStatus("Preset renamed.");
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
        RebuildPresets();
        SelectTab(CurrentTabIndex());
        ShowStatus("Preset deleted.");
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
        AddNumeric(lobby, "Lobby size", _draft.Lobby.MaxPlayers, value => _draft.Lobby.MaxPlayers = Clamp(ParseInt(value, _draft.Lobby.MaxPlayers), 1, 64));

        var keepOpen = AddContainer(page, "Keep open - EXPERIMENTAL", "When activated your Steam lobby policy is managed by OverseerProtocol.");
        AddToggle(keepOpen, "Enabled", _draft.Lobby.EnableLateJoin, value => _draft.Lobby.EnableLateJoin = value);
        AddToggle(keepOpen, "Allow when in orbit", _draft.Lobby.LateJoinInOrbit, value => _draft.Lobby.LateJoinInOrbit = value);
        AddToggle(keepOpen, "Allow when landed", _draft.Lobby.LateJoinOnMoonAsSpectator, value => _draft.Lobby.LateJoinOnMoonAsSpectator = value);

        var compatibility = AddContainer(page, "Compatibility", "Host/client restrictions exposed in multiplayer status and handshake checks.");
        AddToggle(compatibility, "Enable multiplayer", _draft.Lobby.EnableMultiplayer, value => _draft.Lobby.EnableMultiplayer = value);
        AddToggle(compatibility, "Require same mod version", _draft.Lobby.RequireSameModVersion, value => _draft.Lobby.RequireSameModVersion = value);
        AddToggle(compatibility, "Require same config hash", _draft.Lobby.RequireSameConfigHash, value => _draft.Lobby.RequireSameConfigHash = value);
    }

    private void BuildItemsTab(Transform page)
    {
        if (_draft == null)
            return;

        var container = AddContainer(page, "Store items", "Configure observed item values and runtime item edits.");
        foreach (var item in _draft.Items)
        {
            var row = AddContainer(page, item.Baseline.DisplayName, "");
            AddToggle(row, "Enabled", item.Enabled, value => item.Enabled = value);
            AddNumeric(row, "Value", item.Value, value => item.Value = ParseInt(value, item.Value));
            AddNumeric(row, "Weight", item.Weight, value => item.Weight = ParseFloat(value, item.Weight));
        }

        if (_draft.Items.Count == 0)
            AddText(container, "Catalog", "No item catalog is available yet.", _ => { });
    }

    private void BuildPerksTab(Transform page)
    {
        var container = AddContainer(page, "Perks", "Runtime perk catalogs stay owned by OverseerProtocol.");
        AddText(container, "Status", "Perk editing will map onto existing perk services in a later pass.", _ => { });
    }

    private void BuildEnemiesTab(Transform page)
    {
        if (_draft == null)
            return;

        foreach (var spawn in _draft.Spawns)
        {
            var container = AddContainer(page, spawn.Baseline.MoonId, "EnemyId:rarity entries separated by commas.");
            AddToggle(container, "Inside enabled", spawn.InsideEnabled, value => spawn.InsideEnabled = value);
            AddText(container, "Inside enemies", spawn.InsideEnemies, value => spawn.InsideEnemies = value);
            AddToggle(container, "Outside enabled", spawn.OutsideEnabled, value => spawn.OutsideEnabled = value);
            AddText(container, "Outside enemies", spawn.OutsideEnemies, value => spawn.OutsideEnemies = value);
            AddToggle(container, "Daytime enabled", spawn.DaytimeEnabled, value => spawn.DaytimeEnabled = value);
            AddText(container, "Daytime enemies", spawn.DaytimeEnemies, value => spawn.DaytimeEnemies = value);
        }
    }

    private void BuildMoonsTab(Transform page)
    {
        if (_draft == null)
            return;

        foreach (var moon in _draft.Moons)
        {
            var container = AddContainer(page, moon.Baseline.DisplayName, "");
            AddToggle(container, "Enabled", moon.Enabled, value => moon.Enabled = value);
            AddNumeric(container, "Route price", moon.RoutePrice, value => moon.RoutePrice = ParseInt(value, moon.RoutePrice));
            AddNumeric(container, "Min scrap", moon.MinScrap, value => moon.MinScrap = ParseInt(value, moon.MinScrap));
            AddNumeric(container, "Max scrap", moon.MaxScrap, value => moon.MaxScrap = ParseInt(value, moon.MaxScrap));
        }
    }

    private Transform AddContainer(Transform page, string title, string description)
    {
        var tabContent = page.GetComponent<AdvancedCompany.ConfigTabContent>();
        var containerPrefab = tabContent != null ? tabContent.ContainerPrefab : null;
        var parent = tabContent != null && tabContent.Container != null ? tabContent.Container : page;
        GameObject obj;
        if (containerPrefab != null)
        {
            obj = Instantiate(containerPrefab, parent);
            obj.name = "Overseer_" + title;
        }
        else
        {
            obj = new GameObject("Overseer_" + title);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            obj.AddComponent<VerticalLayoutGroup>().spacing = 4f;
        }

        obj.SetActive(true);
        obj.tag = "Untagged";
        var marker = obj.AddComponent<OverseerRuntimeUiMarker>();
        marker.Owner = this;

        var container = obj.GetComponent<AdvancedCompany.ConfigContainer>();
        if (container != null)
        {
            if (container.TitleField != null)
                container.TitleField.text = title;
            if (container.DescriptionField != null)
                container.DescriptionField.text = description;
            return container.Container != null ? container.Container : obj.transform;
        }

        CreateFallbackLabel(obj.transform, title, 18f);
        if (!string.IsNullOrWhiteSpace(description))
            CreateFallbackLabel(obj.transform, description, 12f);
        return obj.transform;
    }

    private void AddToggle(Transform parent, string label, bool value, Action<bool> changed)
    {
        var prefab = parent.GetComponentInParent<AdvancedCompany.ConfigContainer>()?.TogglePrefab;
        var obj = prefab != null ? Instantiate(prefab, parent) : CreateFallbackRow(parent);
        obj.AddComponent<OverseerRuntimeUiMarker>().Owner = this;
        obj.SetActive(true);
        var toggle = obj.GetComponent<AdvancedCompany.ConfigToggle>();
        if (toggle != null)
        {
            if (toggle.Label != null)
                toggle.Label.text = label + (label.EndsWith(":") ? "" : ":");
            if (toggle.ResetButton != null)
                toggle.ResetButton.gameObject.SetActive(false);
            if (toggle.Toggle != null)
            {
                toggle.Toggle.SetIsOnWithoutNotify(value);
                toggle.Toggle.onValueChanged.RemoveAllListeners();
                toggle.Toggle.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<bool>(changed));
            }
            return;
        }

        CreateFallbackLabel(obj.transform, label, 13f);
    }

    private void AddNumeric(Transform parent, string label, int value, Action<string> changed) =>
        AddNumeric(parent, label, value.ToString(CultureInfo.InvariantCulture), changed);

    private void AddNumeric(Transform parent, string label, float value, Action<string> changed) =>
        AddNumeric(parent, label, value.ToString(CultureInfo.InvariantCulture), changed);

    private void AddNumeric(Transform parent, string label, string value, Action<string> changed)
    {
        var prefab = parent.GetComponentInParent<AdvancedCompany.ConfigContainer>()?.NumericInputPrefab;
        var obj = prefab != null ? Instantiate(prefab, parent) : CreateFallbackRow(parent);
        obj.AddComponent<OverseerRuntimeUiMarker>().Owner = this;
        obj.SetActive(true);
        var input = obj.GetComponent<AdvancedCompany.ConfigNumericInput>();
        if (input != null)
        {
            if (input.Label != null)
                input.Label.text = label + (label.EndsWith(":") ? "" : ":");
            if (input.Unit != null)
                input.Unit.text = "";
            if (input.ResetButton != null)
                input.ResetButton.gameObject.SetActive(false);
            if (input.Input != null)
            {
                input.Input.SetTextWithoutNotify(value);
                input.Input.onValueChanged.RemoveAllListeners();
                input.Input.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(changed));
            }
            return;
        }

        CreateFallbackLabel(obj.transform, label + ": " + value, 13f);
    }

    private void AddText(Transform parent, string label, string value, Action<string> changed)
    {
        var prefab = parent.GetComponentInParent<AdvancedCompany.ConfigContainer>()?.TextInputPrefab;
        var obj = prefab != null ? Instantiate(prefab, parent) : CreateFallbackRow(parent);
        obj.AddComponent<OverseerRuntimeUiMarker>().Owner = this;
        obj.SetActive(true);
        var input = obj.GetComponent<AdvancedCompany.ConfigTextInput>();
        if (input != null)
        {
            if (input.Label != null)
                input.Label.text = label + (label.EndsWith(":") ? "" : ":");
            if (input.ResetButton != null)
                input.ResetButton.gameObject.SetActive(false);
            if (input.Input != null)
            {
                input.Input.SetTextWithoutNotify(value);
                input.Input.onValueChanged.RemoveAllListeners();
                input.Input.onValueChanged.AddListener(new UnityEngine.Events.UnityAction<string>(changed));
            }
            return;
        }

        CreateFallbackLabel(obj.transform, label + ": " + value, 13f);
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

    private int CurrentTabIndex()
    {
        for (var i = 0; i < _tabPages.Count; i++)
        {
            if (_tabPages[i].activeSelf)
                return i;
        }

        return 0;
    }

    private void ClearPresetContainer()
    {
        for (var i = _presetContainer.childCount - 1; i >= 0; i--)
        {
            var child = _presetContainer.GetChild(i).gameObject;
            if (child == _presetTemplate)
                continue;

            Destroy(child);
        }
    }

    private static void ClearRuntimeChildren(Transform parent)
    {
        foreach (var marker in parent.GetComponentsInChildren<OverseerRuntimeUiMarker>(true))
            Destroy(marker.gameObject);
    }

    private static Component? FindComponent(GameObject root, string typeName)
    {
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
            if (component != null && component.GetType().Name == typeName)
                return component;
        }

        return null;
    }

    private static T? GetField<T>(Component component, string name) where T : class
    {
        var type = component.GetType();
        while (type != null)
        {
            var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return field.GetValue(component) as T;
            type = type.BaseType;
        }

        return null;
    }

    private static void SetButtonText(Button button, string text)
    {
        var label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = text;
    }

    private static GameObject CreateFallbackRow(Transform parent)
    {
        var obj = new GameObject("OverseerFallbackRow");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        var layout = obj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        return obj;
    }

    private static void CreateFallbackLabel(Transform parent, string text, float size)
    {
        var obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        var label = obj.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = new Color(1f, 0.58f, 0.26f);
    }

    private static int ParseInt(string value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static float ParseFloat(string value, float fallback) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

    private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

    private sealed class OverseerRuntimeUiMarker : MonoBehaviour
    {
        public AdvancedCompanyHostScreenAdapter Owner = null!;
    }
}
