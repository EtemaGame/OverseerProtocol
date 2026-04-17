using System.Collections.Generic;
using System.IO;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Enemies;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.Data.Models.Spawns;
using OverseerProtocol.Data.Models.UserConfig;
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
        _registry.BuildRegistry();

        OPLog.Info("Overrides", $"Loading moon spawn tuning files from: {OPPaths.MoonConfigRoot}");
        var collection = BuildOverrideCollection();

        if (collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No active spawn tuning entries found in moons/*.json.");
            return;
        }

        OPLog.Info("Overrides", $"Spawn tuning loaded: schemaVersion={collection.SchemaVersion}, moonTuningCount={collection.Overrides.Count}");

        var references = LoadReferenceCatalog();
        OPLog.Info("Validation", $"Spawn validation references: exportedMoons={references.MoonIds.Count}, exportedEnemies={references.EnemyIds.Count}");
        var validationResult = _validator.Validate(collection, references);
        validationResult.Report.WriteToLog("Validation");

        var abortOnWarnings = OPConfig.StrictValidation.Value || OPConfig.AbortOnInvalidOverrideBlock.Value;
        OPLog.Info("Validation", $"Spawn validation policy: strict={OPConfig.StrictValidation.Value}, abortOnInvalidBlock={OPConfig.AbortOnInvalidOverrideBlock.Value}, abortOnWarnings={abortOnWarnings}, canApply={validationResult.CanApplyWithStrictMode(abortOnWarnings)}");
        if (!validationResult.CanApplyWithStrictMode(abortOnWarnings))
        {
            var reason = abortOnWarnings && validationResult.Report.WarningCount > 0
                ? "strict tuning validation is enabled and warnings were reported"
                : "critical validation errors were reported";

            OPLog.Warning("Validation", $"Spawn tuning was not applied because {reason}.");
            return;
        }

        if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", $"Dry-run enabled. {validationResult.Collection.Overrides.Count} moon spawn tuning entries validated; no runtime spawn mutations applied.");
            return;
        }

        _applier.Apply(validationResult.Collection);
    }

    private static SpawnOverrideCollection BuildOverrideCollection()
    {
        var collection = new SpawnOverrideCollection();
        if (!Directory.Exists(OPPaths.MoonConfigRoot))
            return collection;

        foreach (var path in Directory.GetFiles(OPPaths.MoonConfigRoot, "*.json"))
        {
            var config = JsonFileReader.Read<UserMoonConfigFile>(path);
            if (config == null || string.IsNullOrWhiteSpace(config.MoonId))
                continue;

            if (!config.Enabled)
            {
                OPLog.Info("Overrides", $"Moon tuning disabled for '{config.MoonId}'. Spawn pools skipped.");
                continue;
            }

            if (config.MissingFromRuntime)
            {
                OPLog.Warning("Overrides", $"Moon tuning '{config.MoonId}' is marked missingFromRuntime. Spawn pools skipped.");
                continue;
            }

            var moonOverride = new MoonSpawnOverride
            {
                MoonId = config.MoonId,
                InsideEnemies = ResolvePool(config.MoonId, "insideEnemies", config.Spawns?.InsideEnemies),
                OutsideEnemies = ResolvePool(config.MoonId, "outsideEnemies", config.Spawns?.OutsideEnemies),
                DaytimeEnemies = ResolvePool(config.MoonId, "daytimeEnemies", config.Spawns?.DaytimeEnemies)
            };

            if (moonOverride.InsideEnemies == null &&
                moonOverride.OutsideEnemies == null &&
                moonOverride.DaytimeEnemies == null)
            {
                OPLog.Info("Overrides", $"Moon '{config.MoonId}' spawn pools are all mode=keep.");
                continue;
            }

            collection.Overrides.Add(moonOverride);
        }

        return collection;
    }

    private static List<SpawnEntry>? ResolvePool(string moonId, string poolName, SpawnPoolConfig? pool)
    {
        var mode = string.IsNullOrWhiteSpace(pool?.Mode) ? "keep" : pool!.Mode.Trim();
        if (string.Equals(mode, "keep", System.StringComparison.OrdinalIgnoreCase))
            return null;

        if (string.Equals(mode, "clear", System.StringComparison.OrdinalIgnoreCase))
        {
            OPLog.Info("Overrides", $"Moon '{moonId}' {poolName} mode=clear. Runtime pool will be empty.");
            return new List<SpawnEntry>();
        }

        if (string.Equals(mode, "replace", System.StringComparison.OrdinalIgnoreCase))
        {
            OPLog.Info("Overrides", $"Moon '{moonId}' {poolName} mode=replace. Runtime pool entries={pool?.Entries?.Count ?? 0}.");
            return pool?.Entries ?? new List<SpawnEntry>();
        }

        OPLog.Warning("Overrides", $"Moon '{moonId}' {poolName} has unknown mode '{mode}'. Keeping vanilla/current pool.");
        return null;
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

            OPLog.Info("Validation", $"Loaded {catalog.EnemyIds.Count} exported enemy IDs for spawn tuning validation.");
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

            OPLog.Info("Validation", $"Loaded {catalog.MoonIds.Count} exported moon IDs for spawn tuning validation.");
        }

        return catalog;
    }
}
