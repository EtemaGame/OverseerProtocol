using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features;

public sealed class PresetFeature
{
    private const float DefaultMultiplier = 1f;
    private const float Epsilon = 0.0001f;

    public PresetRuntimeSettings ResolveRuntimeSettings()
    {
        var settings = new PresetRuntimeSettings
        {
            ItemWeightMultiplier = OPConfig.ItemWeightMultiplier.Value,
            SpawnRarityMultiplier = OPConfig.SpawnRarityMultiplier.Value,
            RoutePriceMultiplier = OPConfig.RoutePriceMultiplier.Value
        };

        OPLog.Info(
            "Presets",
            $"Initial cfg multipliers: itemWeight={settings.ItemWeightMultiplier:0.###}, spawnRarity={settings.SpawnRarityMultiplier:0.###}, routePrice={settings.RoutePriceMultiplier:0.###}");

        if (string.Equals(OPConfig.ActivePresetName, OPConfig.DefaultPreset, StringComparison.OrdinalIgnoreCase))
        {
            OPLog.Info("Presets", "Active preset is default. Using profile user tuning and cfg multiplier values.");
            return settings;
        }

        var preset = BuiltInPresetDefinition.Resolve(OPConfig.ActivePresetName);
        if (preset == null)
        {
            OPLog.Warning("Presets", $"Unknown built-in preset '{OPConfig.ActivePresetName}'. Effective multipliers remain cfg values.");
            return settings;
        }

        OPLog.Info("Presets", $"Loaded preset '{preset.Id}' ({preset.DisplayName}).");

        if (preset.ItemWeightMultiplier.HasValue && IsDefaultMultiplier(OPConfig.ItemWeightMultiplier.Value))
        {
            OPLog.Info("Presets", $"Preset item weight multiplier selected because cfg value is default: {preset.ItemWeightMultiplier.Value:0.###}");
            settings.ItemWeightMultiplier = preset.ItemWeightMultiplier.Value;
        }
        else if (preset.ItemWeightMultiplier.HasValue)
        {
            OPLog.Info("Presets", $"Cfg item weight multiplier wins over preset: cfg={OPConfig.ItemWeightMultiplier.Value:0.###}, preset={preset.ItemWeightMultiplier.Value:0.###}");
        }

        if (preset.SpawnRarityMultiplier.HasValue && IsDefaultMultiplier(OPConfig.SpawnRarityMultiplier.Value))
        {
            OPLog.Info("Presets", $"Preset spawn rarity multiplier selected because cfg value is default: {preset.SpawnRarityMultiplier.Value:0.###}");
            settings.SpawnRarityMultiplier = preset.SpawnRarityMultiplier.Value;
        }
        else if (preset.SpawnRarityMultiplier.HasValue)
        {
            OPLog.Info("Presets", $"Cfg spawn rarity multiplier wins over preset: cfg={OPConfig.SpawnRarityMultiplier.Value:0.###}, preset={preset.SpawnRarityMultiplier.Value:0.###}");
        }

        if (preset.RoutePriceMultiplier.HasValue && IsDefaultMultiplier(OPConfig.RoutePriceMultiplier.Value))
        {
            OPLog.Info("Presets", $"Preset route price multiplier selected because cfg value is default: {preset.RoutePriceMultiplier.Value:0.###}");
            settings.RoutePriceMultiplier = preset.RoutePriceMultiplier.Value;
        }
        else if (preset.RoutePriceMultiplier.HasValue)
        {
            OPLog.Info("Presets", $"Cfg route price multiplier wins over preset: cfg={OPConfig.RoutePriceMultiplier.Value:0.###}, preset={preset.RoutePriceMultiplier.Value:0.###}");
        }

        OPLog.Info(
            "Presets",
            $"Effective preset runtime settings: itemWeight={settings.ItemWeightMultiplier:0.###}, spawnRarity={settings.SpawnRarityMultiplier:0.###}, routePrice={settings.RoutePriceMultiplier:0.###}");

        return settings;
    }

    private static bool IsDefaultMultiplier(float value) =>
        Math.Abs(value - DefaultMultiplier) < Epsilon;
}

internal sealed class BuiltInPresetDefinition
{
    public string Id { get; private set; } = "";
    public string DisplayName { get; private set; } = "";
    public float? ItemWeightMultiplier { get; private set; }
    public float? SpawnRarityMultiplier { get; private set; }
    public float? RoutePriceMultiplier { get; private set; }

    public static BuiltInPresetDefinition? Resolve(string presetName)
    {
        var normalized = string.IsNullOrWhiteSpace(presetName)
            ? OPConfig.DefaultPreset
            : presetName.Trim();

        switch (normalized.ToLowerInvariant())
        {
            case OPConfig.DefaultPreset:
                return null;
            case "vanilla-plus":
                return Create("vanilla-plus", "Vanilla Plus", 1f, 1.1f, 1f);
            case "hardcore":
                return Create("hardcore", "Hardcore", 1.1f, 1.35f, 1.1f);
            case "economy-chaos":
                return Create("economy-chaos", "Economy Chaos", 1f, 1f, 1.5f);
            case "outside-nightmare":
                return Create("outside-nightmare", "Outside Nightmare", 1f, 1.25f, 1f);
            case "scrap-heaven":
                return Create("scrap-heaven", "Scrap Heaven", 0.9f, 1f, 0.9f);
            default:
                return null;
        }
    }

    private static BuiltInPresetDefinition Create(
        string id,
        string displayName,
        float itemWeightMultiplier,
        float spawnRarityMultiplier,
        float routePriceMultiplier) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            ItemWeightMultiplier = itemWeightMultiplier,
            SpawnRarityMultiplier = spawnRarityMultiplier,
            RoutePriceMultiplier = routePriceMultiplier
        };
}

public sealed class PresetRuntimeSettings
{
    public float ItemWeightMultiplier { get; set; }
    public float SpawnRarityMultiplier { get; set; }
    public float RoutePriceMultiplier { get; set; }
}
