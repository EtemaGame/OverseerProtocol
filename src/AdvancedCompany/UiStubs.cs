using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseSettings<T> : MonoBehaviour where T : new()
{
    public sealed class Preset
    {
        public Preset(object provider, string name, T configuration, bool canRemove = true, bool canRename = true, bool canBeSaved = true)
        {
            Name = name;
            Configuration = configuration;
            CanBeRemoved = canRemove;
            CanBeRenamed = canRename;
            CanBeSaved = canBeSaved;
        }

        public string Name;
        public T Configuration;
        public bool CanBeRemoved;
        public bool CanBeRenamed;
        public bool CanBeSaved;
        public GameObject Container = null!;
        public GameObject SelectedBackground = null!;
        public TextMeshProUGUI Label = null!;
        public Button PresetButton = null!;
        public Button RemoveButton = null!;
        public Button RenameButton = null!;
    }

    public Transform TabContainer = null!;
    public Transform Container = null!;
    public GameObject PresetTemplate = null!;
    public Transform PresetContainer = null!;
    public Button CreatePreset = null!;
    public AdvancedCompany.RenamePresetWindow RenamePreset = null!;
    public AdvancedCompany.RemovePresetWindow RemovePreset = null!;
    public AdvancedCompany.ConfirmOverrideWindow ConfirmOverride = null!;
    public AdvancedCompany.NewPresetWindow NewPreset = null!;
    public Button SaveAsNewPresetButton = null!;
    public Button SaveButton = null!;
    public Button ContinueButton = null!;
    public Button CancelButton = null!;
    public GameObject NotificationObject = null!;
    public TextMeshProUGUI NotificationText = null!;
}

public sealed class LobbySettings : BaseSettings<AdvancedCompany.Config.LobbyConfiguration>
{
    public AdvancedCompany.ConfigTabContent GeneralTab = null!;
    public AdvancedCompany.ConfigTabContent GameTab = null!;
    public AdvancedCompany.ConfigTabContent ItemsTab = null!;
    public AdvancedCompany.ConfigTabContent PerksTab = null!;
    public AdvancedCompany.ConfigTabContent EnemiesTab = null!;
    public AdvancedCompany.ConfigTabContent MoonsTab = null!;
}

namespace AdvancedCompany.Config
{
    public class Configuration
    {
        public sealed class ConfigField
        {
            public object? Value;
            public void Reset()
            {
            }
        }
    }

    public sealed class LobbyConfiguration
    {
    }
}

namespace AdvancedCompany.Unity.Moons
{
    public sealed class MoonsContainer : MonoBehaviour
    {
    }
}

namespace AdvancedCompany
{
    public sealed class ConfigContainer : MonoBehaviour
    {
        public TextMeshProUGUI TitleField = null!;
        public TextMeshProUGUI DescriptionField = null!;
        public Transform Container = null!;
        public GameObject SliderPrefab = null!;
        public GameObject NumericInputPrefab = null!;
        public GameObject TextInputPrefab = null!;
        public GameObject TogglePrefab = null!;
        public GameObject ItemPrefab = null!;
        public GameObject UnlockablePrefab = null!;
        public GameObject ScrapPrefab = null!;
        public GameObject EnemyPrefab = null!;
        public GameObject PerkPrefab = null!;
        public GameObject WeatherPrefab = null!;
        public GameObject MoonPrefab = null!;
        public Button AddButton = null!;
    }

    public sealed class ConfigTabContent : MonoBehaviour
    {
        public Transform Container = null!;
        public GameObject ContainerPrefab = null!;
        public GameObject ItemContainerPrefab = null!;
        public GameObject UnlockableContainerPrefab = null!;
        public GameObject PerkContainerPrefab = null!;
        public GameObject WeatherContainerPrefab = null!;
        public GameObject MoonContainerPrefab = null!;
        public GameObject ScrapContainerPrefab = null!;
        public GameObject EnemyContainerPrefab = null!;
    }

    public sealed class ConfigSlider : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Slider Slider = null!;
        public TMP_InputField ValueField = null!;
        public LayoutElement ValueLayout = null!;
        public Button ResetButton = null!;
    }

    public sealed class ConfigToggle : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle Toggle = null!;
        public Button ResetButton = null!;
    }

    public sealed class ConfigNumericInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public TextMeshProUGUI Unit = null!;
        public TMP_InputField Input = null!;
        public LayoutElement InputLayout = null!;
        public Button ResetButton = null!;
    }

    public sealed class ConfigTextInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public TMP_InputField Input = null!;
        public LayoutElement InputLayout = null!;
        public Button ResetButton = null!;
    }

    public sealed class ConfigItemInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle ActiveInput = null!;
        public TMP_InputField PriceInput = null!;
        public TMP_InputField WeightInput = null!;
    }

    public sealed class ConfigScrapInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle ActiveInput = null!;
        public TMP_InputField MinValueInput = null!;
        public TMP_InputField MaxValueInput = null!;
        public TMP_InputField WeightInput = null!;
    }

    public sealed class ConfigUnlockableInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle ActiveInput = null!;
        public TMP_InputField PriceInput = null!;
    }

    public sealed class ConfigEnemyInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle ActiveInput = null!;
        public TMP_InputField PowerInput = null!;
        public TMP_InputField MaxCountInput = null!;
    }

    public sealed class ConfigPerkInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle ActiveInput = null!;
    }

    public sealed class ConfigWeatherInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public Toggle ActiveInput = null!;
    }

    public sealed class ConfigMoonInput : MonoBehaviour
    {
        public TextMeshProUGUI Label = null!;
        public TMP_InputField PriceInput = null!;
        public Toggle ActiveInput = null!;
    }

    public sealed class ConfigLootTableItem : MonoBehaviour
    {
        public Toggle OverrideInput = null!;
        public TMP_InputField NameInput = null!;
        public TMP_InputField RarityInput = null!;
    }

    public sealed class NewPresetWindow : MonoBehaviour
    {
        public GameObject Shadow = null!;
        public TMP_InputField NameInput = null!;
        public Button ConfirmButton = null!;
        public Button CancelButton = null!;
        public Action<string>? OnSubmitted;
    }

    public sealed class RenamePresetWindow : MonoBehaviour
    {
        public GameObject Shadow = null!;
        public TMP_InputField NameInput = null!;
        public Button ConfirmButton = null!;
        public Button CancelButton = null!;
        public Action<string>? OnSubmitted;
    }

    public sealed class RemovePresetWindow : MonoBehaviour
    {
        public GameObject Shadow = null!;
        public TextMeshProUGUI Text = null!;
        public Button ConfirmButton = null!;
        public Button CancelButton = null!;
        public Action? OnSubmitted;
    }

    public sealed class ConfirmOverrideWindow : MonoBehaviour
    {
        public GameObject Shadow = null!;
        public TextMeshProUGUI Text = null!;
        public Button ConfirmButton = null!;
        public Button CancelButton = null!;
        public Action<string>? OnSubmitted;
    }
}
