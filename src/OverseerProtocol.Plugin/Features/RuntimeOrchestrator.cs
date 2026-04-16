using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.GameAbstractions.State;

namespace OverseerProtocol.Features;

public sealed class RuntimeOrchestrator
{
    private readonly RuntimeStateSnapshot _snapshot;

    public RuntimeOrchestrator(RuntimeStateSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public void RunStartupPipeline()
    {
        OPLog.Info("Runtime", "Running startup pipeline.");
        OPLog.Info("Config", $"Active preset: {OPConfig.ActivePresetName}");

        LoadDataContracts();

        if (!_snapshot.IsCaptured)
            _snapshot.Capture();

        ExportVanillaCatalogs();
        ResetToSnapshot();
        ApplyRuntimeConfiguration();
    }

    public void ReloadRuntimeConfiguration()
    {
        OPLog.Info("Runtime", "Reloading runtime configuration from snapshot.");

        if (!_snapshot.IsCaptured)
        {
            OPLog.Warning("Runtime", "No runtime snapshot is available. Reload aborted.");
            return;
        }

        LoadDataContracts();
        ResetToSnapshot();
        ApplyRuntimeConfiguration();
    }

    public void ResetToSnapshot()
    {
        _snapshot.Restore();
    }

    public void ExportVanillaCatalogs()
    {
        if (OPConfig.EnableDataExport.Value)
        {
            DataExportFeature.RunInitialExport();
        }
        else
        {
            OPLog.Info("Export", "Data export disabled by config.");
        }
    }

    private static void LoadDataContracts()
    {
        if (OPConfig.EnableProgressionStorage.Value)
        {
            var progressionStore = new ProgressionStore();
            progressionStore.LoadOrCreate();
        }
        else
        {
            OPLog.Info("Progression", "Progression storage disabled by config.");
        }

        if (OPConfig.EnablePerkCatalog.Value)
        {
            var perkCatalogFeature = new PerkCatalogFeature();
            perkCatalogFeature.LoadOrCreate();
        }
        else
        {
            OPLog.Info("Perks", "Perk catalog loading disabled by config.");
        }

        if (OPConfig.EnableLobbyRulesLoading.Value)
        {
            var lobbyRulesFeature = new LobbyRulesFeature();
            lobbyRulesFeature.LoadOrCreate();
        }
        else
        {
            OPLog.Info("LobbyRules", "Lobby rules loading disabled by config.");
        }

        if (OPConfig.EnableRuntimeRulesLoading.Value)
        {
            var runtimeRulesFeature = new RuntimeRulesFeature();
            runtimeRulesFeature.LoadOrCreate();
        }
        else
        {
            OPLog.Info("RuntimeRules", "Runtime rules loading disabled by config.");
        }
    }

    private static void ApplyRuntimeConfiguration()
    {
        var presetFeature = new PresetFeature();
        var presetRuntimeSettings = presetFeature.ResolveRuntimeSettings();

        if (OPConfig.EnableItemOverrides.Value)
        {
            var itemOverrideFeature = new ItemOverrideFeature();
            itemOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            OPLog.Info("Overrides", "Item overrides disabled by config.");
        }

        if (OPConfig.EnableSpawnOverrides.Value)
        {
            var spawnOverrideFeature = new SpawnOverrideFeature();
            spawnOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            OPLog.Info("Overrides", "Spawn overrides disabled by config.");
        }

        if (OPConfig.EnableMoonOverrides.Value)
        {
            var moonOverrideFeature = new MoonOverrideFeature();
            moonOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            OPLog.Info("Overrides", "Moon overrides disabled by config.");
        }

        if (OPConfig.EnableRuntimeMultipliers.Value && !OPConfig.DryRunOverrides.Value)
        {
            var semanticDifficultyFeature = new SemanticDifficultyFeature();
            var spawnRarityMultiplier = semanticDifficultyFeature.ApplyAggressionProfile(presetRuntimeSettings.SpawnRarityMultiplier);

            var runtimeMultiplierFeature = new RuntimeMultiplierFeature();
            runtimeMultiplierFeature.ApplyMultipliers(
                presetRuntimeSettings.ItemWeightMultiplier,
                spawnRarityMultiplier,
                presetRuntimeSettings.RoutePriceMultiplier);
        }
        else if (OPConfig.DryRunOverrides.Value)
        {
            OPLog.Info("Overrides", "Dry-run enabled. Runtime multipliers were resolved but not applied.");
        }
        else
        {
            OPLog.Info("Overrides", "Runtime multipliers disabled by config.");
        }
    }
}
