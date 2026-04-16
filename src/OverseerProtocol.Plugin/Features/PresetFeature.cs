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
            SpawnRarityMultiplier = OPConfig.SpawnRarityMultiplier.Value
        };

        if (string.Equals(OPConfig.ActivePresetName, OPConfig.DefaultPreset, StringComparison.OrdinalIgnoreCase))
            return settings;

        var preset = LoadPreset(OPConfig.ActivePresetName);
        if (preset == null)
            return settings;

        OPLog.Info("Presets", $"Loaded preset '{preset.Id}' ({preset.DisplayName}).");

        if (preset.ItemWeightMultiplier.HasValue && IsDefaultMultiplier(OPConfig.ItemWeightMultiplier.Value))
            settings.ItemWeightMultiplier = preset.ItemWeightMultiplier.Value;

        if (preset.SpawnRarityMultiplier.HasValue && IsDefaultMultiplier(OPConfig.SpawnRarityMultiplier.Value))
            settings.SpawnRarityMultiplier = preset.SpawnRarityMultiplier.Value;

        return settings;
    }

    private PresetDefinition? LoadPreset(string presetName)
    {
        var path = OPPaths.GetPresetManifestPath(presetName);
        var preset = JsonFileReader.Read<PresetDefinition>(path);

        if (preset == null)
        {
            OPLog.Warning("Presets", $"Active preset '{presetName}' has no valid preset.json at {path}. Only folder-based JSON overrides will be used.");
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
}
