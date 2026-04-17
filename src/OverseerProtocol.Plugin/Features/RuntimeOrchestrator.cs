using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Diagnostics;
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
        var metrics = PhaseMetrics.Start("Runtime", "startup-pipeline");
        OPLog.Info("Runtime", "=== TEST SIGNAL: STARTUP PIPELINE BEGIN ===");
        OPLog.Info("Runtime", "Running startup pipeline.");
        OPLog.Info("Config", $"Active preset: {OPConfig.ActivePresetName}");
        RuntimeDiagnostics.LogConfigSummary();
        RuntimeDiagnostics.LogRuntimePreconditions("startup-pipeline/begin");

        OPLog.Info("Runtime", "Startup step 1/6: load data-only contracts.");
        LoadDataContracts();

        OPLog.Info("Runtime", "Startup step 2/5: capture vanilla runtime snapshot if needed.");
        if (!_snapshot.IsCaptured)
            _snapshot.Capture();
        else
            OPLog.Info("Snapshot", "Runtime snapshot was already captured. Startup capture skipped.");

        RuntimeDiagnostics.LogRuntimePreconditions("startup-pipeline/after-snapshot");
        OPLog.Info("Runtime", "Startup step 3/6: export vanilla catalogs.");
        ExportVanillaCatalogs();
        OPLog.Info("Runtime", "Startup step 4/6: ensure human-editable config files.");
        EnsureUserConfigFiles();
        LogFingerprints();
        OPLog.Info("Runtime", "Startup step 5/6: reset runtime state to snapshot before applying config.");
        ResetToSnapshot();
        OPLog.Info("Runtime", "Startup step 6/6: apply preset, user config, multipliers, and runtime rules.");
        ApplyRuntimeConfiguration();
        RuntimeDiagnostics.LogRuntimePreconditions("startup-pipeline/end");
        OPLog.Info("Runtime", "=== TEST SIGNAL: STARTUP PIPELINE END ===");
        metrics.Complete();
    }

    public void ReloadRuntimeConfiguration()
    {
        var metrics = PhaseMetrics.Start("Runtime", "reload-runtime-configuration");
        OPLog.Info("Runtime", "=== TEST SIGNAL: RUNTIME RELOAD BEGIN ===");
        OPLog.Info("Runtime", "Reloading runtime configuration from snapshot.");
        RuntimeDiagnostics.LogRuntimePreconditions("reload/begin");

        if (!_snapshot.IsCaptured)
        {
            OPLog.Warning("Runtime", "No runtime snapshot is available. Reload aborted.");
            metrics.Warning();
            metrics.Complete();
            OPLog.Info("Runtime", "=== TEST SIGNAL: RUNTIME RELOAD ABORTED ===");
            return;
        }

        LoadDataContracts();
        EnsureUserConfigFiles();
        LogFingerprints();
        ResetToSnapshot();
        ApplyRuntimeConfiguration();
        RuntimeDiagnostics.LogRuntimePreconditions("reload/end");
        OPLog.Info("Runtime", "=== TEST SIGNAL: RUNTIME RELOAD END ===");
        metrics.Complete();
    }

    public void ResetToSnapshot()
    {
        var metrics = PhaseMetrics.Start("Runtime", "reset-to-snapshot");
        RuntimeDiagnostics.LogRuntimePreconditions("reset-to-snapshot/before");
        _snapshot.Restore();
        RuntimeDiagnostics.LogRuntimePreconditions("reset-to-snapshot/after");
        metrics.Complete();
    }

    public void ExportVanillaCatalogs()
    {
        var metrics = PhaseMetrics.Start("Runtime", "export-vanilla-catalogs");
        RuntimeDiagnostics.LogRuntimePreconditions("export-vanilla-catalogs/before");
        if (OPConfig.EnableDataExport.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Data export", true, "running catalog export");
            DataExportFeature.RunInitialExport();
            metrics.Applied();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Data export", false, "skipping catalog export");
            OPLog.Info("Export", "Data export disabled by config.");
            metrics.Skipped();
        }

        RuntimeDiagnostics.LogRuntimePreconditions("export-vanilla-catalogs/after");
        metrics.Complete();
    }

    public void EnsureUserConfigFiles()
    {
        var metrics = PhaseMetrics.Start("Runtime", "ensure-user-config-files");
        var userConfigFeature = new UserConfigFeature();
        userConfigFeature.EnsureUserFiles();
        metrics.Applied();
        metrics.Complete();
    }

    private static void LogFingerprints()
    {
        var fingerprints = new FingerprintFeature().ComputeCurrent();
        OPLog.Info("Fingerprint", $"Active preset: {fingerprints.ActivePreset}");
        OPLog.Info("Fingerprint", $"Preset fingerprint: {fingerprints.PresetFingerprint}");
        OPLog.Info("Fingerprint", $"Config fingerprint: {fingerprints.ConfigFingerprint}");
    }

    private static void LoadDataContracts()
    {
        OPLog.Info("Runtime", "Loading data-only/runtime contract files.");
        if (OPConfig.EnableProgressionStorage.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Progression storage", true, "load or create progression.json");
            var progressionStore = new ProgressionStore();
            progressionStore.LoadOrCreate();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Progression storage", false, "skip progression.json");
            OPLog.Info("Progression", "Progression storage disabled by config.");
        }

        if (OPConfig.EnablePerkCatalog.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Perk catalog", true, "load or create perks.json");
            var perkCatalogFeature = new PerkCatalogFeature();
            perkCatalogFeature.LoadOrCreate();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Perk catalog", false, "skip perks.json");
            OPLog.Info("Perks", "Perk catalog loading disabled by config.");
        }

        if (OPConfig.EnableLobbyRulesLoading.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Lobby rules", true, "load or create lobby-rules.json");
            var lobbyRulesFeature = new LobbyRulesFeature();
            lobbyRulesFeature.LoadOrCreate();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Lobby rules", false, "skip lobby-rules.json");
            OPLog.Info("LobbyRules", "Lobby rules loading disabled by config.");
        }

        if (OPConfig.EnableRuntimeRulesLoading.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime rules", true, "load or create runtime-rules.json");
            var runtimeRulesFeature = new RuntimeRulesFeature();
            runtimeRulesFeature.LoadOrCreate();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime rules", false, "skip runtime-rules.json");
            OPLog.Info("RuntimeRules", "Runtime rules loading disabled by config.");
        }

        if (OPConfig.EnableExperimentalMultiplayer.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Experimental multiplayer", true, "apply diagnostics/scaffold");
            var multiplayerFeature = new ExperimentalMultiplayerFeature();
            multiplayerFeature.Apply();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Experimental multiplayer", false, "skip experimental multiplayer scaffold");
            OPLog.Info("Multiplayer", "Experimental multiplayer loading disabled by config.");
        }
    }

    private static void ApplyRuntimeConfiguration()
    {
        OPLog.Info("Runtime", "Applying runtime configuration in precedence order: snapshot -> preset -> user JSON tuning -> cfg multipliers/toggles -> runtime rules.");
        RuntimeDiagnostics.LogRuntimePreconditions("apply-runtime-configuration/before");
        var presetFeature = new PresetFeature();
        var presetRuntimeSettings = presetFeature.ResolveRuntimeSettings();
        OPLog.Info(
            "Runtime",
            $"Resolved effective runtime settings: itemWeight={presetRuntimeSettings.ItemWeightMultiplier:0.###}, spawnRarity={presetRuntimeSettings.SpawnRarityMultiplier:0.###}, routePrice={presetRuntimeSettings.RoutePriceMultiplier:0.###}");

        if (OPConfig.EnableItemOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Item tuning", true, "load, validate, and apply overseer-data/items.json");
            var itemOverrideFeature = new ItemOverrideFeature();
            itemOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Item tuning", false, "skip overseer-data/items.json");
            OPLog.Info("Overrides", "Item tuning disabled by config.");
        }

        if (OPConfig.EnableSpawnOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Spawn tuning", true, "load, validate, and apply overseer-data/moons/*.json spawn pools");
            var spawnOverrideFeature = new SpawnOverrideFeature();
            spawnOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Spawn tuning", false, "skip overseer-data/moons/*.json spawn pools");
            OPLog.Info("Overrides", "Spawn tuning disabled by config.");
        }

        if (OPConfig.EnableMoonOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Moon tuning", true, "load, validate, and apply overseer-data/moons/*.json moon fields");
            var moonOverrideFeature = new MoonOverrideFeature();
            moonOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Moon tuning", false, "skip overseer-data/moons/*.json moon fields");
            OPLog.Info("Overrides", "Moon tuning disabled by config.");
        }

        if (OPConfig.EnableRuntimeMultipliers.Value && !OPConfig.DryRunOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime multipliers", true, "apply effective cfg/preset multipliers");
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
            RuntimeDiagnostics.LogFeatureDecision("Runtime multipliers", false, "dry-run: resolved but not applied");
            OPLog.Info("Overrides", "Dry-run enabled. Runtime multipliers were resolved but not applied.");
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime multipliers", false, "disabled by config");
            OPLog.Info("Overrides", "Runtime multipliers disabled by config.");
        }

        if (OPConfig.EnableRuntimeRulesLoading.Value && !OPConfig.DryRunOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime rules application", true, "apply active runtime-rules.json fields");
            var runtimeRulesFeature = new RuntimeRulesFeature();
            var runtimeRules = runtimeRulesFeature.LoadOrCreate();
            runtimeRulesFeature.Apply(runtimeRules);
        }
        else if (OPConfig.DryRunOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime rules application", false, "dry-run: load but do not apply");
            OPLog.Info("RuntimeRules", "Dry-run enabled. Runtime rules were loaded but not applied.");
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime rules application", false, "disabled by config");
        }

        RuntimeDiagnostics.LogRuntimePreconditions("apply-runtime-configuration/after");
    }
}
