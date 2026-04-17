using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
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

        OPLog.Info("Overrides", "Loading spawn tuning from BepInEx .cfg.");
        var collection = new RuntimeTuningConfig(OPConfig.ConfigFile).BuildSpawnOverrides();

        if (collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No active spawn tuning entries found in .cfg.");
            return;
        }

        OPLog.Info("Overrides", $"Spawn .cfg tuning loaded: moonTuningCount={collection.Overrides.Count}");

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

    private SpawnOverrideReferenceCatalog LoadReferenceCatalog()
    {
        var catalog = new SpawnOverrideReferenceCatalog();

        foreach (var enemyId in _registry.EnemyIds)
        {
            if (!string.IsNullOrWhiteSpace(enemyId))
                catalog.EnemyIds.Add(enemyId);
        }

        if (StartOfRound.Instance?.levels != null)
        {
            foreach (var level in StartOfRound.Instance.levels)
            {
                if (level != null && !string.IsNullOrWhiteSpace(level.name))
                    catalog.MoonIds.Add(level.name);
            }
        }

        OPLog.Info("Validation", $"Loaded .cfg spawn validation IDs from runtime: moons={catalog.MoonIds.Count}, enemies={catalog.EnemyIds.Count}");
        return catalog;
    }
}
