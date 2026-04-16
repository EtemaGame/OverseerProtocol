using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Presets;

namespace OverseerProtocol.Features;

public static class PresetBootstrapFeature
{
    public static void EnsureBuiltInPresets()
    {
        EnsurePreset(new PresetDefinition
        {
            Id = "vanilla-plus",
            DisplayName = "Vanilla Plus",
            Description = "A light-touch profile that nudges spawn pressure without changing entity identities.",
            ItemWeightMultiplier = 1f,
            SpawnRarityMultiplier = 1.1f,
            Notes = new List<string>
            {
                "Uses runtime multipliers only.",
                "Add optional JSON overrides under this preset's overrides folder."
            }
        });

        EnsurePreset(new PresetDefinition
        {
            Id = "hardcore",
            DisplayName = "Hardcore",
            Description = "A higher-pressure profile that increases item burden and enemy spawn rarity.",
            ItemWeightMultiplier = 1.1f,
            SpawnRarityMultiplier = 1.35f,
            Notes = new List<string>
            {
                "Uses runtime multipliers only for safe cross-modpack behavior.",
                "JSON overrides can be layered into the preset folder for advanced tuning."
            }
        });
    }

    private static void EnsurePreset(PresetDefinition preset)
    {
        var presetRoot = OPPaths.GetPresetRoot(preset.Id);
        var overridesRoot = OPPaths.GetPresetOverridesRoot(preset.Id);
        var manifestPath = OPPaths.GetPresetManifestPath(preset.Id);

        Directory.CreateDirectory(presetRoot);
        Directory.CreateDirectory(overridesRoot);

        if (File.Exists(manifestPath))
            return;

        JsonFileWriter.Write(manifestPath, preset);
        OPLog.Info("Presets", $"Seeded built-in preset '{preset.Id}' at {manifestPath}");
    }
}
