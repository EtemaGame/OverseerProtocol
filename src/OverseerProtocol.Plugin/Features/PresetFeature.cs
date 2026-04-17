using System;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Presets;

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

        var preset = LoadPreset(OPConfig.ActivePresetName);
        if (preset == null)
        {
            OPLog.Info("Presets", "Preset manifest unavailable. Effective multipliers remain cfg values.");
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

    private PresetDefinition? LoadPreset(string presetName)
    {
        var path = OPPaths.GetPresetManifestPath(presetName);
        var preset = JsonFileReader.Read<PresetDefinition>(path);

        if (preset == null)
        {
            OPLog.Warning("Presets", $"Active preset '{presetName}' has no valid preset.json at {path}. Effective multipliers will stay on cfg values.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(preset.Id))
            preset.Id = presetName;

        return preset;
    }

    private static bool IsDefaultMultiplier(float value) =>
        Math.Abs(value - DefaultMultiplier) < Epsilon;
}

public sealed class PresetRuntimeSettings
{
    public float ItemWeightMultiplier { get; set; }
    public float SpawnRarityMultiplier { get; set; }
    public float RoutePriceMultiplier { get; set; }
}
