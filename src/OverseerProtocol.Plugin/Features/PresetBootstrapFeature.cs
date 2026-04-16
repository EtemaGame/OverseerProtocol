using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Lobby;
using OverseerProtocol.Data.Models.Presets;
using OverseerProtocol.Data.Models.Rules;

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
            RoutePriceMultiplier = 1f,
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
            RoutePriceMultiplier = 1.1f,
            Notes = new List<string>
            {
                "Uses runtime multipliers only for safe cross-modpack behavior.",
                "JSON overrides can be layered into the preset folder for advanced tuning."
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
                "Intentionally ships without moon IDs; add moons.override.json entries after exporting catalogs.",
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
                "Add spawns.override.json entries after exporting moon and enemy catalogs."
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
                "Add items.override.json entries after exporting item catalogs."
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
        {
            EnsureOverrideTemplates(preset.Id);
            return;
        }

        JsonFileWriter.Write(manifestPath, preset);
        EnsureOverrideTemplates(preset.Id);
        OPLog.Info("Presets", $"Seeded built-in preset '{preset.Id}' at {manifestPath}");
    }

    private static void EnsureOverrideTemplates(string presetId)
    {
        EnsureTemplate(OPPaths.GetItemOverridePath(presetId), new ItemOverrideCollection());
        EnsureTemplate(OPPaths.GetSpawnOverridePath(presetId), new SpawnOverrideCollection());
        EnsureTemplate(OPPaths.GetMoonOverridePath(presetId), new MoonOverrideCollection());
        EnsureTemplate(OPPaths.GetPresetLobbyRulesPath(presetId), new LobbyRulesDefinition());
        EnsureTemplate(OPPaths.GetPresetRuntimeRulesPath(presetId), new RuntimeRulesDefinition());
    }

    private static void EnsureTemplate<T>(string path, T template)
    {
        if (File.Exists(path))
            return;

        JsonFileWriter.Write(path, template);
    }
}
