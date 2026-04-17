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
        OPLog.Info("Presets", "Ensuring built-in preset manifests exist.");

        EnsurePreset(new PresetDefinition
        {
            Id = "vanilla-plus",
            DisplayName = "Vanilla Plus",
            Description = "A light-touch profile that nudges spawn pressure without changing entity identities.",
            ItemWeightMultiplier = 1f,
            SpawnRarityMultiplier = 1.1f,
            RoutePriceMultiplier = 1f,
            Notes = new List<string>
            {
                "Uses runtime multipliers only.",
                "Detailed tuning now lives in overseer-data/items.json and overseer-data/moons/<MoonId>.json."
            }
        });

        EnsurePreset(new PresetDefinition
        {
            Id = "hardcore",
            DisplayName = "Hardcore",
            Description = "A higher-pressure profile that increases item burden and enemy spawn rarity.",
            ItemWeightMultiplier = 1.1f,
            SpawnRarityMultiplier = 1.35f,
            RoutePriceMultiplier = 1.1f,
            Notes = new List<string>
            {
                "Uses runtime multipliers only for safe cross-modpack behavior.",
                "Detailed tuning now lives in overseer-data/items.json and overseer-data/moons/<MoonId>.json."
            }
        });

        EnsurePreset(new PresetDefinition
        {
            Id = "economy-chaos",
            DisplayName = "Economy Chaos",
            Description = "A preset shell for aggressive moon economy and route price experiments.",
            ItemWeightMultiplier = 1f,
            SpawnRarityMultiplier = 1f,
            RoutePriceMultiplier = 1.5f,
            Notes = new List<string>
            {
                "Tune moon route prices in overseer-data/moons/<MoonId>.json after exports are generated.",
                "Designed as a safe starting point for host-side economy tuning."
            }
        });

        EnsurePreset(new PresetDefinition
        {
            Id = "outside-nightmare",
            DisplayName = "Outside Nightmare",
            Description = "A preset shell for outside and daytime spawn pressure experiments.",
            ItemWeightMultiplier = 1f,
            SpawnRarityMultiplier = 1.25f,
            RoutePriceMultiplier = 1f,
            Notes = new List<string>
            {
                "Uses a modest global spawn rarity multiplier until explicit spawn pools are added.",
                "Set a moon spawn pool mode to replace in overseer-data/moons/<MoonId>.json to tune exact enemies."
            }
        });

        EnsurePreset(new PresetDefinition
        {
            Id = "scrap-heaven",
            DisplayName = "Scrap Heaven",
            Description = "A preset shell for higher-value scrap and lighter carry experiments.",
            ItemWeightMultiplier = 0.9f,
            SpawnRarityMultiplier = 1f,
            RoutePriceMultiplier = 0.9f,
            Notes = new List<string>
            {
                "Uses a light item weight multiplier only.",
                "Tune item values in overseer-data/items.json after exports are generated."
            }
        });
    }

    private static void EnsurePreset(PresetDefinition preset)
    {
        var presetRoot = OPPaths.GetPresetRoot(preset.Id);
        var manifestPath = OPPaths.GetPresetManifestPath(preset.Id);

        Directory.CreateDirectory(presetRoot);

        if (File.Exists(manifestPath))
        {
            OPLog.Info("Presets", $"Built-in preset '{preset.Id}' already exists at {manifestPath}. User file will not be overwritten.");
            return;
        }

        JsonFileWriter.Write(manifestPath, preset);
        OPLog.Info("Presets", $"Seeded built-in preset '{preset.Id}' at {manifestPath}");
    }
}
