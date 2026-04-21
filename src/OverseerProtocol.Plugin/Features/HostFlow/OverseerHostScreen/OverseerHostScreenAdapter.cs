using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using OverseerProtocol.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OverseerProtocol.Features.HostFlow.OverseerHostScreen
{
    internal sealed class OverseerHostScreenAdapter : MonoBehaviour, IOverseerHostScreen
    {
        [SerializeField] private OverseerTheme _theme = null!;

        private readonly OverseerHostPresetStore _presetStore = new();

        private readonly List<Button> _tabs = new();
        private readonly List<Image> _tabIndicators = new();
        private readonly List<TextMeshProUGUI> _tabLabels = new();
        private readonly string[] _tabNames = { "Lobby", "Game", "Items", "Perks", "Enemies", "Moons" };

        private GameObject _screenRoot = null!;
        private CanvasGroup _canvasGroup = null!;
        private Transform _presetContainer = null!;
        private Transform _tabContent = null!;
        private Button _continueButton = null!;
        private Button _cancelButton = null!;
        private Button _saveButton = null!;
        private Button _saveAsNewButton = null!;
        private TextMeshProUGUI _statusText = null!;
        private Image _statusDot = null!;

        private TuningDraftStore? _draft;
        private OverseerHostReadModel? _model;
        private bool _waitingForWarningConfirmation;
        private bool _busy;
        private int _currentTabIndex;
        private Coroutine? _fadeCoroutine;

        public event Action<OverseerHostProfile>? ContinueRequested;
        public event Action<OverseerHostProfile>? WarningsConfirmed;
        public event Action? CancelRequested;
        public event Action<OverseerHostProfile>? SaveRequested;

        public void Initialize()
        {
            if (_theme == null)
                _theme = ScriptableObject.CreateInstance<OverseerTheme>();

            var rect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _screenRoot = CreateRoot();
            BuildLayout();
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

            SetButtonText(_continueButton, "□ CONTINUE");
            SetBusy(false);
            RebuildPresets();

            StartCoroutine(SelectTabNextFrame(0));

            if (model.InitialWarnings.Count > 0)
                ShowWarningStatus(string.Join("\n", model.InitialWarnings));
            else
                ShowSuccessStatus("SYSTEM READY - PRESET LOADED.");

            gameObject.SetActive(true);
            _screenRoot.SetActive(true);
            transform.SetAsLastSibling();

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeCanvas(0f, 1f, _theme.fadeInDuration));
        }

        public void Close()
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeOutAndHide(_theme.fadeInDuration));
        }

        private IEnumerator SelectTabNextFrame(int index)
        {
            yield return null;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_screenRoot.transform);
            yield return null;
            SelectTab(index);
        }

        public void SetBusy(bool busy)
        {
            _busy = busy;

            if (_continueButton != null) _continueButton.interactable = !busy;
            if (_cancelButton != null) _cancelButton.interactable = !busy;
            if (_saveButton != null) _saveButton.interactable = !busy;
            if (_saveAsNewButton != null) _saveAsNewButton.interactable = !busy;
        }

        public void ShowError(string message)
        {
            _waitingForWarningConfirmation = false;
            SetButtonText(_continueButton, "□ CONTINUE");
            ShowErrorStatus(message);
        }

        public void ShowStatus(string message) => ShowSuccessStatus(message);

        public void ShowWarnings(IReadOnlyList<string> warnings)
        {
            _waitingForWarningConfirmation = true;
            SetButtonText(_continueButton, "□ CONFIRM");
            ShowWarningStatus(warnings.Count == 0
                ? "CONFIRM WARNINGS TO CONTINUE."
                : string.Join("\n", warnings));
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

            root.AddComponent<Image>().color = _theme.overlayColor;

            _canvasGroup = root.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            return root;
        }

        private void BuildLayout()
        {
            var frame = CreatePanel(_screenRoot.transform, "Frame", _theme.frameColor, outline: true, shadow: true);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = new Vector2(12f, 10f);
            frameRect.offsetMax = new Vector2(-12f, -10f);

            BuildScanlineOverlay(frame.transform);

            var headerSlot = new GameObject("HeaderSlot");
            headerSlot.transform.SetParent(frame.transform, false);
            var headerSlotRect = headerSlot.AddComponent<RectTransform>();
            headerSlotRect.anchorMin = new Vector2(0f, 1f);
            headerSlotRect.anchorMax = new Vector2(1f, 1f);
            headerSlotRect.pivot = new Vector2(0.5f, 1f);
            headerSlotRect.anchoredPosition = Vector2.zero;
            headerSlotRect.sizeDelta = new Vector2(0f, 22f);

            var footerSlot = new GameObject("FooterSlot");
            footerSlot.transform.SetParent(frame.transform, false);
            var footerSlotRect = footerSlot.AddComponent<RectTransform>();
            footerSlotRect.anchorMin = new Vector2(0f, 0f);
            footerSlotRect.anchorMax = new Vector2(1f, 0f);
            footerSlotRect.pivot = new Vector2(0.5f, 0f);
            footerSlotRect.anchoredPosition = Vector2.zero;
            footerSlotRect.sizeDelta = new Vector2(0f, 24f);

            var body = new GameObject("Body");
            body.transform.SetParent(frame.transform, false);
            var bodyRect = body.AddComponent<RectTransform>();
            StretchWithOffsets(bodyRect, 0f, 0f, 22f, 24f);

            const float sidebarWidth = 96f;
            const float gutter = 1f;

            var sidebarSlot = new GameObject("SidebarSlot");
            sidebarSlot.transform.SetParent(body.transform, false);
            var sidebarRect = sidebarSlot.AddComponent<RectTransform>();
            sidebarRect.anchorMin = new Vector2(0f, 0f);
            sidebarRect.anchorMax = new Vector2(0f, 1f);
            sidebarRect.pivot = new Vector2(0f, 0.5f);
            sidebarRect.anchoredPosition = Vector2.zero;
            sidebarRect.sizeDelta = new Vector2(sidebarWidth, 0f);

            var mainSlot = new GameObject("MainSlot");
            mainSlot.transform.SetParent(body.transform, false);
            var mainRect = mainSlot.AddComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0f, 0f);
            mainRect.anchorMax = new Vector2(1f, 1f);
            mainRect.offsetMin = new Vector2(sidebarWidth + gutter, 0f);
            mainRect.offsetMax = Vector2.zero;

            BuildHeader(headerSlot.transform);
            BuildPresetPanel(sidebarSlot.transform);
            BuildMainPanel(mainSlot.transform);
            BuildFooter(footerSlot.transform);
        }

        private void BuildScanlineOverlay(Transform parent)
        {
            var overlay = new GameObject("ScanlineOverlay");
            overlay.transform.SetParent(parent, false);

            var rect = overlay.AddComponent<RectTransform>();
            Stretch(rect, 0f);

            var tex = new Texture2D(1, 2, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point
            };

            tex.SetPixel(0, 0, new Color(_theme.scanlineColor.r, _theme.scanlineColor.g, _theme.scanlineColor.b, 0f));
            tex.SetPixel(0, 1, new Color(_theme.scanlineColor.r, _theme.scanlineColor.g, _theme.scanlineColor.b, _theme.scanlineAlpha));
            tex.Apply();

            var raw = overlay.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = Color.white;
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0f, 0f, 1f, 380f);
        }

        private void BuildHeader(Transform parent)
        {
            var header = CreatePanel(parent, "Header", _theme.headerColor, outline: true);
            var headerRect = (RectTransform)header.transform;
            Stretch(headerRect, 0f);

            var layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var iconPanel = CreatePanel(header.transform, "HeaderIcon", _theme.panelDark, outline: true);
            var iconLE = iconPanel.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 10f;
            iconLE.preferredHeight = 10f;
            iconLE.flexibleHeight = 0f;

            var iconLabel = CreateLabel(iconPanel.transform, "●", 5.5f, _theme.accent);
            iconLabel.alignment = TextAlignmentOptions.Center;
            iconLabel.fontStyle = FontStyles.Bold;
            Stretch(iconLabel.rectTransform, 0f);

            var title = CreateLabel(header.transform, "OVERSEERPROTOCOL — HOST SETUP", 7.2f, _theme.text);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 0.6f;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var versionBadge = CreatePanel(header.transform, "VersionBadge", _theme.panelDark, outline: true);
            var versionLE = versionBadge.AddComponent<LayoutElement>();
            versionLE.preferredWidth = 26f;
            versionLE.preferredHeight = 10f;
            versionLE.flexibleHeight = 0f;

            var versionLabel = CreateLabel(versionBadge.transform, "v81", 5.5f, _theme.textDim);
            versionLabel.alignment = TextAlignmentOptions.Center;
            versionLabel.fontStyle = FontStyles.Bold;
            Stretch(versionLabel.rectTransform, 0f);
        }

        private void BuildPresetPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "PresetsPanel", _theme.sidebarColor, outline: true);
            var panelRect = (RectTransform)panel.transform;
            Stretch(panelRect, 0f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 3);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var title = CreateLabel(panel.transform, "PRESETS", 6.2f, _theme.textDim);
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 1.4f;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 8f;

            CreateDivider(panel.transform, 1f);

            var scroll = CreateScroll(panel.transform, "PresetScroll", 2f);
            scroll.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            _presetContainer = scroll.content;

            var createButton = CreateButton(
                panel.transform,
                "+ CREATE\nNEW",
                6.8f,
                _theme.panelDark,
                _theme.text,
                outline: true);

            createButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            createButton.onClick.AddListener(new UnityEngine.Events.UnityAction(SaveAsPreset));
        }

        private void BuildMainPanel(Transform parent)
        {
            var panel = CreatePanel(parent, "MainPanel", _theme.contentColor);
            var panelRect = (RectTransform)panel.transform;
            Stretch(panelRect, 0f);

            const float tabsHeight = 14f;
            const float statusHeight = 12f;

            var tabsSlot = new GameObject("TabsSlot");
            tabsSlot.transform.SetParent(panel.transform, false);
            var tabsSlotRect = tabsSlot.AddComponent<RectTransform>();
            tabsSlotRect.anchorMin = new Vector2(0f, 1f);
            tabsSlotRect.anchorMax = new Vector2(1f, 1f);
            tabsSlotRect.pivot = new Vector2(0.5f, 1f);
            tabsSlotRect.anchoredPosition = Vector2.zero;
            tabsSlotRect.sizeDelta = new Vector2(0f, tabsHeight);

            var statusSlot = new GameObject("StatusSlot");
            statusSlot.transform.SetParent(panel.transform, false);
            var statusSlotRect = statusSlot.AddComponent<RectTransform>();
            statusSlotRect.anchorMin = new Vector2(0f, 0f);
            statusSlotRect.anchorMax = new Vector2(1f, 0f);
            statusSlotRect.pivot = new Vector2(0.5f, 0f);
            statusSlotRect.anchoredPosition = Vector2.zero;
            statusSlotRect.sizeDelta = new Vector2(0f, statusHeight);

            var contentSlot = new GameObject("ContentSlot");
            contentSlot.transform.SetParent(panel.transform, false);
            var contentSlotRect = contentSlot.AddComponent<RectTransform>();
            StretchWithOffsets(contentSlotRect, 0f, 0f, tabsHeight, statusHeight);

            BuildTabBar(tabsSlot.transform);

            var scroll = CreateScroll(contentSlot.transform, "ContentScroll", 4f);
            var scrollRect = (RectTransform)scroll.transform;
            Stretch(scrollRect, 0f);
            _tabContent = scroll.content;

            BuildStatusBar(statusSlot.transform);
        }

        private void BuildTabBar(Transform parent)
        {
            var tabBar = CreatePanel(parent, "Tabs", _theme.panelDark, outline: true);
            var tabBarRect = (RectTransform)tabBar.transform;
            Stretch(tabBarRect, 0f);

            var layout = tabBar.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(2, 2, 0, 0);
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            _tabs.Clear();
            _tabIndicators.Clear();
            _tabLabels.Clear();

            for (var i = 0; i < _tabNames.Length; i++)
            {
                var index = i;

                var tab = CreatePanel(tabBar.transform, "Tab_" + _tabNames[i], _theme.panelDark);
                tab.AddComponent<LayoutElement>().preferredWidth = 34f;

                var button = tab.AddComponent<Button>();
                button.targetGraphic = tab.GetComponent<Image>();
                button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectTab(index)));

                var label = CreateLabel(tab.transform, _tabNames[i].ToUpperInvariant(), 5.8f, _theme.textDim);
                label.alignment = TextAlignmentOptions.Center;
                label.fontStyle = FontStyles.Normal;
                label.characterSpacing = 0f;
                label.overflowMode = TextOverflowModes.Ellipsis;
                Stretch(label.rectTransform, 0f);

                var indicator = CreatePanel(tab.transform, "Indicator", _theme.accent);
                var indicatorRect = (RectTransform)indicator.transform;
                indicatorRect.anchorMin = new Vector2(0f, 0f);
                indicatorRect.anchorMax = new Vector2(1f, 0f);
                indicatorRect.pivot = new Vector2(0.5f, 0f);
                indicatorRect.anchoredPosition = Vector2.zero;
                indicatorRect.sizeDelta = new Vector2(0f, 1f);
                indicator.SetActive(false);

                _tabs.Add(button);
                _tabIndicators.Add(indicator.GetComponent<Image>());
                _tabLabels.Add(label);
            }

            var spacer = new GameObject("TabSpacer");
            spacer.transform.SetParent(tabBar.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void BuildStatusBar(Transform parent)
        {
            var status = CreatePanel(parent, "StatusBar", _theme.panelDark, outline: true);
            var statusRect = (RectTransform)status.transform;
            Stretch(statusRect, 0f);

            var layout = status.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 2, 2);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;

            var dot = CreatePanel(status.transform, "StatusDot", _theme.statusGreen);
            var dotLayout = dot.AddComponent<LayoutElement>();
            dotLayout.preferredWidth = 3f;
            dotLayout.preferredHeight = 3f;
            dotLayout.flexibleHeight = 0f;
            _statusDot = dot.GetComponent<Image>();

            _statusText = CreateLabel(status.transform, "", 5.8f, _theme.statusGreen);
            _statusText.fontStyle = FontStyles.Bold;
            _statusText.enableWordWrapping = false;
            _statusText.overflowMode = TextOverflowModes.Ellipsis;
            _statusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void BuildFooter(Transform parent)
        {
            var footer = CreatePanel(parent, "Footer", _theme.panelDark, outline: true);
            var footerRect = (RectTransform)footer.transform;
            Stretch(footerRect, 0f);

            var layout = footer.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleRight;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(footer.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            _cancelButton = CreateButton(footer.transform, "□ CANCEL", 6.8f, _theme.panelDark, _theme.text, outline: true);
            _saveAsNewButton = CreateButton(footer.transform, "□ SAVE AS NEW PRESET", 6.8f, _theme.panelDark, _theme.text, outline: true);
            _saveButton = CreateButton(footer.transform, "□ SAVE", 6.8f, _theme.panelDark, _theme.text, outline: true);
            _continueButton = CreateButton(footer.transform, "□ CONTINUE", 6.8f, _theme.panelDark, _theme.text, outline: true);

            SetButtonSize(_cancelButton, 56f, 16f);
            SetButtonSize(_saveAsNewButton, 104f, 16f);
            SetButtonSize(_saveButton, 44f, 16f);
            SetButtonSize(_continueButton, 68f, 16f);
        }

        private static void SetButtonSize(Button button, float width, float height)
        {
            var le = button.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
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
                StyleTab(i, i == index);

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

        private void StyleTab(int index, bool active)
        {
            if (index < 0 || index >= _tabs.Count)
                return;

            if (_tabs[index].targetGraphic is Image image)
                image.color = active ? _theme.tabActive : _theme.panelDark;

            if (index < _tabLabels.Count)
            {
                _tabLabels[index].color = active ? _theme.text : _theme.textDim;
                _tabLabels[index].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }

            if (index < _tabIndicators.Count)
            {
                _tabIndicators[index].color = _theme.accent;
                _tabIndicators[index].gameObject.SetActive(active);
            }
        }

        private void RebuildPresets()
        {
            ClearChildren(_presetContainer);

            if (_model == null || _draft == null)
                return;

            var presets = new Dictionary<string, PresetSummary>(StringComparer.OrdinalIgnoreCase);
            foreach (var preset in _model.BuiltInPresets) presets[preset.Id] = preset;
            foreach (var preset in _model.CustomPresets) presets[preset.Id] = preset;
            foreach (var preset in _presetStore.LoadSummaries()) presets[preset.Id] = preset;

            foreach (var preset in presets.Values
                         .OrderBy(p => p.IsBuiltIn ? 0 : 1)
                         .ThenBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                AddPresetButton(preset);
            }
        }

        private void AddPresetButton(PresetSummary preset)
        {
            var selected = IsSelected(preset);
            var bg = selected ? _theme.presetSelected : _theme.presetIdle;

            var button = CreateButton(
                _presetContainer,
                (selected ? "▶ " : "") + preset.DisplayName,
                6.2f,
                bg,
                selected ? _theme.text : _theme.textMuted,
                outline: true);

            var le = button.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 12f;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.overflowMode = TextOverflowModes.Ellipsis;
                label.enableWordWrapping = false;
                label.margin = new Vector4(1f, 0f, 1f, 0f);
            }

            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectPreset(preset)));
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
            RebuildPresets();
            SelectTab(_currentTabIndex);
            ShowSuccessStatus("PRESET LOADED - " + preset.DisplayName.ToUpperInvariant());
        }

        private bool IsSelected(PresetSummary preset) =>
            _draft != null &&
            string.Equals(_draft.Lobby.PresetId, preset.Id, StringComparison.OrdinalIgnoreCase);

        private void SaveAsPreset()
        {
            if (_draft == null)
                return;

            var displayName = CurrentPresetDisplayName();
            var summary = _presetStore.SaveAs(displayName, _draft);
            _draft.Lobby.PresetId = summary.Id;

            RebuildPresets();
            ShowSuccessStatus("PRESET CREATED.");
            SaveRequested?.Invoke(CurrentProfile());
        }

        private void SavePreset()
        {
            if (_draft == null)
                return;

            if (!_presetStore.IsCustom(_draft.Lobby.PresetId))
            {
                ShowWarningStatus("BUILT-IN PRESETS CANNOT BE OVERWRITTEN. USE SAVE AS NEW PRESET.");
                return;
            }

            _presetStore.Save(_draft.Lobby.PresetId, CurrentPresetDisplayName(), _draft);
            RebuildPresets();
            ShowSuccessStatus("PRESET SAVED.");
            SaveRequested?.Invoke(CurrentProfile());
        }

        private string CurrentPresetDisplayName()
        {
            if (_draft == null)
                return "New preset";

            var selected = _model?.BuiltInPresets
                .Concat(_model.CustomPresets)
                .Concat(_presetStore.LoadSummaries())
                .FirstOrDefault(p => string.Equals(p.Id, _draft.Lobby.PresetId, StringComparison.OrdinalIgnoreCase));

            return selected?.DisplayName ?? "New preset";
        }

        private void BuildLobbyTab(Transform page)
        {
            if (_draft == null)
                return;

            var lobby = AddSection(page, "Lobby", "BASIC CONFIG");
            AddToggleRow(lobby, "Enable multiplayer",
                _draft.Lobby.EnableMultiplayer,
                v => _draft.Lobby.EnableMultiplayer = v);

            AddToggleRow(lobby, "Late join",
                _draft.Lobby.EnableLateJoin,
                v => _draft.Lobby.EnableLateJoin = v);

            AddNumericField(lobby, "Lobby size",
                _draft.Lobby.MaxPlayers,
                v => _draft.Lobby.MaxPlayers = Clamp(ParseInt(v, _draft.Lobby.MaxPlayers), 1, 64),
                86f);

            var compatibility = AddSection(page, "Compatibility", "HANDSHAKE");
            AddToggleRow(compatibility, "Require same mod version",
                _draft.Lobby.RequireSameModVersion,
                v => _draft.Lobby.RequireSameModVersion = v);

            AddToggleRow(compatibility, "Require same config hash",
                _draft.Lobby.RequireSameConfigHash,
                v => _draft.Lobby.RequireSameConfigHash = v);

            if (_draft.Lobby.EnableLateJoin)
            {
                var advanced = AddSection(page, "Late join rules", "ADVANCED");
                AddToggleRow(advanced, "Allow when in orbit",
                    _draft.Lobby.LateJoinInOrbit,
                    v => _draft.Lobby.LateJoinInOrbit = v);

                AddToggleRow(advanced, "Allow when landed",
                    _draft.Lobby.LateJoinOnMoonAsSpectator,
                    v => _draft.Lobby.LateJoinOnMoonAsSpectator = v);
            }
        }

        private void BuildGameTab(Transform page)
        {
            var runtime = AddSection(page, "Runtime toggles", "APPLY BLOCKS");
            AddToggleRow(runtime, "Item edits", OPConfig.EnableItemOverrides.Value, v => OPConfig.EnableItemOverrides.Value = v);
            AddToggleRow(runtime, "Moon edits", OPConfig.EnableMoonOverrides.Value, v => OPConfig.EnableMoonOverrides.Value = v);
            AddToggleRow(runtime, "Enemy spawn edits", OPConfig.EnableSpawnOverrides.Value, v => OPConfig.EnableSpawnOverrides.Value = v);
            AddToggleRow(runtime, "Runtime multipliers", OPConfig.EnableRuntimeMultipliers.Value, v => OPConfig.EnableRuntimeMultipliers.Value = v);

            var multipliers = AddSection(page, "Multipliers", "GLOBAL SCALE");
            AddNumericField(multipliers, "Item weight", OPConfig.ItemWeightMultiplier.Value,
                v => OPConfig.ItemWeightMultiplier.Value = Clamp(ParseFloat(v, OPConfig.ItemWeightMultiplier.Value), 0f, 10f), 42f);

            AddNumericField(multipliers, "Spawn rarity", OPConfig.SpawnRarityMultiplier.Value,
                v => OPConfig.SpawnRarityMultiplier.Value = Clamp(ParseFloat(v, OPConfig.SpawnRarityMultiplier.Value), 0f, 10f), 42f);

            AddNumericField(multipliers, "Route price", OPConfig.RoutePriceMultiplier.Value,
                v => OPConfig.RoutePriceMultiplier.Value = Clamp(ParseFloat(v, OPConfig.RoutePriceMultiplier.Value), 0f, 10f), 42f);

            AddNumericField(multipliers, "Travel discount", OPConfig.TravelDiscountMultiplier.Value,
                v => OPConfig.TravelDiscountMultiplier.Value = Clamp(ParseFloat(v, OPConfig.TravelDiscountMultiplier.Value), 0f, 10f), 42f);
        }

        private void BuildItemsTab(Transform page)
        {
            if (_draft == null)
                return;

            var container = AddSection(page, "Store items", "CATALOG");
            if (_draft.Items.Count == 0)
            {
                AddReadOnlyText(container, "Status", "No item catalog is available yet.");
                return;
            }

            foreach (var item in _draft.Items.OrderBy(e => e.Baseline.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                AddCompactItemRow(container,
                    item.Baseline.DisplayName,
                    item.Enabled, v => item.Enabled = v,
                    item.Value, v => item.Value = ParseInt(v, item.Value),
                    item.Weight, v => item.Weight = ParseFloat(v, item.Weight));
            }
        }

        private void BuildPerksTab(Transform page)
        {
            var summary = new PerkMenuProvider().BuildReadOnlySummary();
            var container = AddSection(page, "Perks", "READ-ONLY");
            AddReadOnlyText(container, "Summary", summary);
        }

        private void BuildEnemiesTab(Transform page)
        {
            if (_draft == null)
                return;

            if (_draft.Spawns.Count == 0)
            {
                var empty = AddSection(page, "Spawn overrides", "PER MOON");
                AddReadOnlyText(empty, "Status", "No spawn catalog available yet.");
                return;
            }

            var sampleIds = _draft.EnemyIds.Take(4).ToList();
            var hint = sampleIds.Count > 0
                ? "KNOWN IDS: " + string.Join(", ", sampleIds) + (_draft.EnemyIds.Count > sampleIds.Count ? "..." : "")
                : "FORMAT: EnemyId:rarity";

            foreach (var spawn in _draft.Spawns.OrderBy(e => e.Baseline.MoonId, StringComparer.OrdinalIgnoreCase))
            {
                var container = AddSection(page, spawn.Baseline.MoonId, hint);
                AddToggleRow(container, "Inside enabled", spawn.InsideEnabled, v => spawn.InsideEnabled = v);
                AddTextField(container, "Inside enemies", spawn.InsideEnemies, v => spawn.InsideEnemies = v, true);
                AddToggleRow(container, "Outside enabled", spawn.OutsideEnabled, v => spawn.OutsideEnabled = v);
                AddTextField(container, "Outside enemies", spawn.OutsideEnemies, v => spawn.OutsideEnemies = v, true);
                AddToggleRow(container, "Daytime enabled", spawn.DaytimeEnabled, v => spawn.DaytimeEnabled = v);
                AddTextField(container, "Daytime enemies", spawn.DaytimeEnemies, v => spawn.DaytimeEnemies = v, true);
            }
        }

        private void BuildMoonsTab(Transform page)
        {
            if (_draft == null)
                return;

            if (_draft.Moons.Count == 0)
            {
                var empty = AddSection(page, "Moons", "CATALOG");
                AddReadOnlyText(empty, "Status", "No moon data available yet.");
                return;
            }

            foreach (var moon in _draft.Moons.OrderBy(e => e.Baseline.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                var riskLabel = string.IsNullOrWhiteSpace(moon.RiskLabel) ? "NONE" : moon.RiskLabel.ToUpperInvariant();
                var container = AddSection(page, moon.Baseline.DisplayName, "RISK: " + riskLabel);

                AddToggleRow(container, "Enabled", moon.Enabled, v => moon.Enabled = v);
                AddNumericField(container, "Route price", moon.RoutePrice, v => moon.RoutePrice = ParseInt(v, moon.RoutePrice), 42f);
                AddNumericField(container, "Min scrap", moon.MinScrap, v => moon.MinScrap = ParseInt(v, moon.MinScrap), 42f);
                AddNumericField(container, "Max scrap", moon.MaxScrap, v => moon.MaxScrap = ParseInt(v, moon.MaxScrap), 42f);
                AddNumericField(container, "Min total value", moon.MinTotalScrapValue, v => moon.MinTotalScrapValue = ParseInt(v, moon.MinTotalScrapValue), 42f);
                AddNumericField(container, "Max total value", moon.MaxTotalScrapValue, v => moon.MaxTotalScrapValue = ParseInt(v, moon.MaxTotalScrapValue), 42f);
            }
        }

        private Transform AddSection(Transform parent, string title, string badge)
        {
            var panel = CreatePanel(parent, "Section_" + Sanitize(title), _theme.panelColor, outline: true);

            var panelLE = panel.AddComponent<LayoutElement>();
            panelLE.flexibleWidth = 1f;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var header = CreatePanel(panel.transform, "Header", _theme.panelHeaderColor, outline: true);
            header.AddComponent<LayoutElement>().preferredHeight = 13f;

            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(4, 4, 2, 2);
            headerLayout.spacing = 2f;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            var dot = CreatePanel(header.transform, "Dot", _theme.accent);
            var dotLE = dot.AddComponent<LayoutElement>();
            dotLE.preferredWidth = 2f;
            dotLE.preferredHeight = 7f;
            dotLE.flexibleHeight = 0f;

            var titleLabel = CreateLabel(header.transform, title.ToUpperInvariant(), 6.2f, _theme.text);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.characterSpacing = 0f;
            titleLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var badgeLabel = CreateLabel(header.transform, badge, 5.3f, _theme.textDim);
            badgeLabel.alignment = TextAlignmentOptions.MidlineRight;
            badgeLabel.fontStyle = FontStyles.Bold;
            badgeLabel.enableWordWrapping = false;
            badgeLabel.overflowMode = TextOverflowModes.Ellipsis;
            badgeLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 62f;

            var body = new GameObject("Body");
            body.transform.SetParent(panel.transform, false);
            body.AddComponent<RectTransform>();

            var bodyLayout = body.AddComponent<VerticalLayoutGroup>();
            bodyLayout.padding = new RectOffset(4, 4, 2, 2);
            bodyLayout.spacing = 1f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = false;

            return body.transform;
        }

        private void AddToggleRow(Transform parent, string label, bool value, Action<bool> changed)
        {
            var row = CreateRow(parent, "Toggle_" + Sanitize(label), 11f, 2f);

            var labelText = CreateLabel(row.transform, label, 6.4f, _theme.textLabel);
            labelText.fontStyle = FontStyles.Normal;
            labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 74f;

            AddTogglePill(row.transform, value, changed, showStateText: true);

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void AddNumericField(Transform parent, string label, int value, Action<string> changed, float width) =>
            AddTextField(parent, label, value.ToString(CultureInfo.InvariantCulture), changed, false, width);

        private void AddNumericField(Transform parent, string label, float value, Action<string> changed, float width) =>
            AddTextField(parent, label, value.ToString(CultureInfo.InvariantCulture), changed, false, width);

        private void AddTextField(Transform parent, string label, string value, Action<string> changed, bool multiline, float width = 42f)
        {
            var row = CreateRow(parent, "Input_" + Sanitize(label), multiline ? 30f : 13f, 2f);

            var labelText = CreateLabel(row.transform, label, 6.4f, _theme.textLabel);
            labelText.fontStyle = FontStyles.Normal;
            labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 74f;

            var input = CreateInput(row.transform, value, multiline, width);
            input.onEndEdit.AddListener(new UnityEngine.Events.UnityAction<string>(changed));

            if (!multiline)
            {
                var spacer = new GameObject("Spacer");
                spacer.transform.SetParent(row.transform, false);
                spacer.AddComponent<RectTransform>();
                spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }
        }

        private void AddReadOnlyText(Transform parent, string label, string value)
        {
            var row = CreateRow(parent, "ReadOnly_" + Sanitize(label), 48f, 2f);

            var labelText = CreateLabel(row.transform, label, 6.4f, _theme.textLabel);
            labelText.fontStyle = FontStyles.Normal;
            labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 74f;

            var box = CreatePanel(row.transform, "ReadOnlyValue", _theme.panelDark, outline: true);
            box.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var content = CreateLabel(box.transform, value, 5.8f, _theme.textMuted);
            content.enableWordWrapping = true;
            content.alignment = TextAlignmentOptions.TopLeft;
            Stretch(content.rectTransform, 3f);
        }

        private void AddCompactItemRow(
            Transform parent,
            string itemName,
            bool enabled, Action<bool> enabledChanged,
            int value, Action<string> valueChanged,
            float weight, Action<string> weightChanged)
        {
            var row = CreateRow(parent, "Item_" + Sanitize(itemName), 13f, 3f);

            var nameLabel = CreateLabel(row.transform, itemName, 6.0f, _theme.textMuted);
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.overflowMode = TextOverflowModes.Ellipsis;
            nameLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 66f;

            AddTogglePill(row.transform, enabled, enabledChanged, showStateText: false);
            AddMiniField(row.transform, "VAL", value.ToString(CultureInfo.InvariantCulture), valueChanged, 24f);
            AddMiniField(row.transform, "WT", weight.ToString(CultureInfo.InvariantCulture), weightChanged, 24f);

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(row.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void AddMiniField(Transform parent, string label, string value, Action<string> changed, float width)
        {
            var miniLabel = CreateLabel(parent, label, 5.2f, _theme.textDim);
            miniLabel.fontStyle = FontStyles.Bold;
            miniLabel.alignment = TextAlignmentOptions.Center;
            miniLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 12f;

            var input = CreateInput(parent, value, false, width);
            input.onEndEdit.AddListener(new UnityEngine.Events.UnityAction<string>(changed));
        }

        private void AddTogglePill(Transform parent, bool initialValue, Action<bool> changed, bool showStateText)
        {
            var buttonObj = CreatePanel(
                parent,
                "TogglePill",
                initialValue ? _theme.accentSoft : _theme.panelDark,
                outline: true);

            var layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 14f;
            layout.preferredHeight = 8f;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonObj.GetComponent<Image>();

            var knob = CreatePanel(buttonObj.transform, "Knob", initialValue ? _theme.text : _theme.textDim);
            var knobRect = (RectTransform)knob.transform;
            knobRect.sizeDelta = new Vector2(4f, 4f);

            TextMeshProUGUI? stateLabel = null;
            if (showStateText)
            {
                stateLabel = CreateLabel(parent, initialValue ? "ON" : "OFF", 5.4f,
                    initialValue ? _theme.text : _theme.textDim);
                stateLabel.fontStyle = FontStyles.Bold;
                stateLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 11f;
            }

            var currentValue = initialValue;
            ApplyToggleVisual(buttonObj.GetComponent<Image>(), knob.GetComponent<Image>(), knobRect, stateLabel, currentValue);

            button.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                currentValue = !currentValue;
                ApplyToggleVisual(buttonObj.GetComponent<Image>(), knob.GetComponent<Image>(), knobRect, stateLabel, currentValue);
                changed(currentValue);
            }));
        }

        private void ApplyToggleVisual(Image background, Image knob, RectTransform knobRect, TextMeshProUGUI? stateLabel, bool enabled)
        {
            background.color = enabled ? _theme.accentSoft : _theme.panelDark;
            knob.color = enabled ? _theme.text : _theme.textDim;

            knobRect.anchorMin = knobRect.anchorMax = new Vector2(enabled ? 1f : 0f, 0.5f);
            knobRect.pivot = new Vector2(enabled ? 1f : 0f, 0.5f);
            knobRect.anchoredPosition = new Vector2(enabled ? -1f : 1f, 0f);

            if (stateLabel != null)
            {
                stateLabel.text = enabled ? "ON" : "OFF";
                stateLabel.color = enabled ? _theme.text : _theme.textDim;
            }
        }

        private TMP_InputField CreateInput(Transform parent, string value, bool multiline, float width)
        {
            var root = CreatePanel(parent, "InputField", _theme.panelDark, outline: true);
            var layout = root.AddComponent<LayoutElement>();

            if (multiline)
            {
                layout.flexibleWidth = 1f;
                layout.preferredHeight = 30f;
            }
            else
            {
                layout.preferredWidth = width;
                layout.preferredHeight = 13f;
            }

            var input = root.AddComponent<TMP_InputField>();
            input.targetGraphic = root.GetComponent<Image>();
            input.lineType = multiline
                ? TMP_InputField.LineType.MultiLineNewline
                : TMP_InputField.LineType.SingleLine;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(3f, 1f);
            viewportRect.offsetMax = new Vector2(-3f, -1f);
            viewport.AddComponent<RectMask2D>();

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(viewport.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 6.4f;
            text.fontStyle = FontStyles.Bold;
            text.color = _theme.text;
            text.enableWordWrapping = multiline;
            text.alignment = multiline ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.MidlineLeft;

            input.textViewport = viewportRect;
            input.textComponent = text;
            input.SetTextWithoutNotify(value ?? string.Empty);

            return input;
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

        private void ShowSuccessStatus(string message) => SetStatus(message, _theme.statusGreen);
        private void ShowWarningStatus(string message) => SetStatus(message, _theme.statusAmber);
        private void ShowErrorStatus(string message) => SetStatus(message, _theme.statusRed);

        private void SetStatus(string message, Color color)
        {
            if (_statusText == null || _statusDot == null)
                return;

            _statusText.text = (message ?? string.Empty).ToUpperInvariant();
            _statusText.color = color;
            _statusDot.color = color;
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            _canvasGroup.alpha = from;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = to;
        }

        private IEnumerator FadeOutAndHide(float duration)
        {
            yield return FadeCanvas(1f, 0f, duration);

            if (_screenRoot != null)
                _screenRoot.SetActive(false);

            gameObject.SetActive(false);
        }

        private static void ClearChildren(Transform parent)
        {
#if UNITY_EDITOR
            for (var i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
#else
            for (var i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
#endif
        }

        private static void SetButtonText(Button button, string text)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = text;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color, bool outline = false, bool shadow = false)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            var image = obj.AddComponent<Image>();
            image.color = color;

            if (outline)
            {
                var line = obj.AddComponent<Outline>();
                line.effectColor = _theme.borderColor;
                line.effectDistance = new Vector2(1f, -1f);
                line.useGraphicAlpha = true;
            }

            if (shadow)
            {
                var dropShadow = obj.AddComponent<Shadow>();
                dropShadow.effectColor = _theme.shadowColor;
                dropShadow.effectDistance = new Vector2(0f, -1f);
                dropShadow.useGraphicAlpha = true;
            }

            return obj;
        }

        private static GameObject CreateRow(Transform parent, string name, float height, float spacing)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;

            row.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private Button CreateButton(Transform parent, string text, float size, Color color, Color textColor, bool outline)
        {
            var obj = CreatePanel(parent, "Button_" + Sanitize(text), color, outline: outline);
            var button = obj.AddComponent<Button>();
            button.targetGraphic = obj.GetComponent<Image>();

            var label = CreateLabel(obj.transform, text, size, textColor);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 0f;
            label.enableWordWrapping = text.Contains('\n');
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform, 0f);

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
            label.overflowMode = TextOverflowModes.Overflow;

            return label;
        }

        private ScrollRect CreateScroll(Transform parent, string name, float viewportInset)
        {
            var root = CreatePanel(parent, name, _theme.scrollColor);
            var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(viewportInset, viewportInset);
            viewportRect.offsetMax = new Vector2(-viewportInset, -viewportInset);
            viewport.AddComponent<Image>().color = _theme.maskColor;
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
            layout.spacing = 2f;
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

        private void CreateDivider(Transform parent, float height)
        {
            var divider = CreatePanel(parent, "Divider", _theme.borderColor);
            divider.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void StretchWithOffsets(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static string Sanitize(string value) =>
            new string(value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());

        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

        private static float ParseFloat(string value, float fallback) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;

        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}