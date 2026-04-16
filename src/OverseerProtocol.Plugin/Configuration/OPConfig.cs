using BepInEx.Configuration;

namespace OverseerProtocol.Configuration;

public static class OPConfig
{
    public const string DefaultPreset = "default";

    public static ConfigEntry<bool> EnableDataExport { get; private set; } = null!;
    public static ConfigEntry<bool> EnableItemOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableMoonOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableSpawnOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableRuntimeMultipliers { get; private set; } = null!;
    public static ConfigEntry<bool> EnableProgressionStorage { get; private set; } = null!;
    public static ConfigEntry<bool> EnableLobbyRulesLoading { get; private set; } = null!;
    public static ConfigEntry<bool> StrictValidation { get; private set; } = null!;
    public static ConfigEntry<bool> DryRunOverrides { get; private set; } = null!;
    public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
    public static ConfigEntry<string> AggressionProfile { get; private set; } = null!;
    public static ConfigEntry<float> ItemWeightMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> SpawnRarityMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> RoutePriceMultiplier { get; private set; } = null!;

    public static string ActivePresetName
    {
        get
        {
            var value = ActivePreset?.Value;
            return string.IsNullOrWhiteSpace(value) ? DefaultPreset : value.Trim();
        }
    }

    public static void Bind(ConfigFile config)
    {
        EnableDataExport = config.Bind(
            "General",
            "EnableDataExport",
            true,
            "Exports vanilla catalogs before runtime overrides are applied.");

        ActivePreset = config.Bind(
            "General",
            "ActivePreset",
            DefaultPreset,
            "Preset/profile name. 'default' reads overseer-data/overrides; any other value reads overseer-data/presets/<name>/overrides.");

        EnableItemOverrides = config.Bind(
            "Overrides",
            "EnableItemOverrides",
            true,
            "Applies items.override.json after export.");

        EnableMoonOverrides = config.Bind(
            "Overrides",
            "EnableMoonOverrides",
            true,
            "Applies moons.override.json after export.");

        EnableSpawnOverrides = config.Bind(
            "Overrides",
            "EnableSpawnOverrides",
            true,
            "Applies spawns.override.json after export.");

        EnableRuntimeMultipliers = config.Bind(
            "Multipliers",
            "EnableRuntimeMultipliers",
            true,
            "Applies simple .cfg multipliers after JSON overrides.");

        EnableProgressionStorage = config.Bind(
            "Progression",
            "EnableProgressionStorage",
            true,
            "Creates and loads the progression save file used by future player and ship perks.");

        EnableLobbyRulesLoading = config.Bind(
            "Lobby",
            "EnableLobbyRulesLoading",
            true,
            "Creates and loads lobby-rules.json for future expanded lobby, late join, and sync enforcement.");

        StrictValidation = config.Bind(
            "Validation",
            "StrictValidation",
            false,
            "When true, any validation warning aborts the affected override flow. Errors always abort.");

        DryRunOverrides = config.Bind(
            "Validation",
            "DryRunOverrides",
            false,
            "When true, override files are loaded and validated but no runtime item/spawn mutations are applied.");

        AggressionProfile = config.Bind(
            "SemanticDifficulty",
            "AggressionProfile",
            "Balanced",
            new ConfigDescription(
                "Semantic spawn pressure profile applied on top of SpawnRarityMultiplier.",
                new AcceptableValueList<string>("Calm", "Balanced", "Aggressive", "Nightmare")));

        ItemWeightMultiplier = config.Bind(
            "Multipliers",
            "ItemWeightMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime item weight after JSON item overrides. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        SpawnRarityMultiplier = config.Bind(
            "Multipliers",
            "SpawnRarityMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime spawn pool rarity after JSON spawn overrides. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        RoutePriceMultiplier = config.Bind(
            "Multipliers",
            "RoutePriceMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime moon route price after JSON moon overrides. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));
    }
}
