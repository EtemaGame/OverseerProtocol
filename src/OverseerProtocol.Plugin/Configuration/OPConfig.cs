using BepInEx.Configuration;

namespace OverseerProtocol.Configuration;

public static class OPConfig
{
    public const string DefaultPreset = "default";

    public static ConfigEntry<bool> EnableDataExport { get; private set; } = null!;
    public static ConfigEntry<bool> EnableItemOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableSpawnOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableRuntimeMultipliers { get; private set; } = null!;
    public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
    public static ConfigEntry<string> AggressionProfile { get; private set; } = null!;
    public static ConfigEntry<float> ItemWeightMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> SpawnRarityMultiplier { get; private set; } = null!;

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
    }
}
