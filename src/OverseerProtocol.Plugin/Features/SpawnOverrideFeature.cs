using System.Collections.Generic;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Enemies;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.GameAbstractions.Overrides;

namespace OverseerProtocol.Features;

public sealed class SpawnOverrideFeature
{
    private readonly EnemyTypeRegistry _registry;
    private readonly SpawnOverrideValidator _validator;
    private readonly SpawnOverrideApplier _applier;

    public SpawnOverrideFeature()
    {
        _registry = new EnemyTypeRegistry();
        _validator = new SpawnOverrideValidator(_registry);
        _applier = new SpawnOverrideApplier(_registry);
    }

    public void ApplyOverrides(string presetName)
    {
        // 1. Prepare Registry
        _registry.BuildRegistry();

        // 2. Load Configuration
        var path = OPPaths.GetSpawnOverridePath(presetName);
        OPLog.Info("Overrides", $"Loading spawn overrides from: {path}");
        
        var collection = JsonFileReader.Read<SpawnOverrideCollection>(path);
        
        if (collection == null)
        {
            OPLog.Info("Overrides", "No valid spawns.override.json found or file is empty.");
            return;
        }

        // 3. Pre-validation and reference resolution
        var references = LoadReferenceCatalog();
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        if (!validationResult.CanApplyWithStrictMode(OPConfig.StrictValidation.Value))
        {
            var reason = OPConfig.StrictValidation.Value && validationResult.Report.WarningCount > 0
                ? "strict validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Spawn override collection was not applied because {reason}.");
            return;
        }

        // 4. Application
        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} moon spawn overrides validated; no runtime spawn mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private SpawnOverrideReferenceCatalog LoadReferenceCatalog()
    {
        var catalog = new SpawnOverrideReferenceCatalog();

        var enemies = JsonFileReader.Read<List<EnemyDefinition>>(OPPaths.EnemyExportPath);
        if (enemies == null || enemies.Count == 0)
        {
            OPLog.Warning("Validation", $"No exported enemy catalog found at {OPPaths.EnemyExportPath}.");
        }
        else
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && !string.IsNullOrWhiteSpace(enemy.Id))
                    catalog.EnemyIds.Add(enemy.Id);
            }

            OPLog.Info("Validation", $"Loaded {catalog.EnemyIds.Count} exported enemy IDs for spawn override validation.");
        }

        var moons = JsonFileReader.Read<List<MoonDefinition>>(OPPaths.MoonExportPath);
        if (moons == null || moons.Count == 0)
        {
            OPLog.Warning("Validation", $"No exported moon catalog found at {OPPaths.MoonExportPath}.");
        }
        else
        {
            foreach (var moon in moons)
            {
                if (moon != null && !string.IsNullOrWhiteSpace(moon.Id))
                    catalog.MoonIds.Add(moon.Id);
            }

            OPLog.Info("Validation", $"Loaded {catalog.MoonIds.Count} exported moon IDs for spawn override validation.");
        }

        return catalog;
    }
}
