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
        RuntimeDiagnostics.LogTestSignal("Runtime", "=== TEST SIGNAL: STARTUP PIPELINE BEGIN ===");
        OPLog.Info("Runtime", "Running startup pipeline.");
        OPLog.Info("Config", $"Advanced preset: {OPConfig.ActivePresetName}");
        RuntimeDiagnostics.LogConfigSummary();
        RuntimeDiagnostics.LogRuntimePreconditions("startup-pipeline/begin");

        OPLog.Info("Runtime", "Startup step 1/6: load data-only contracts.");
        LoadDataContracts();

        OPLog.Info("Runtime", "Startup step 2/6: capture vanilla runtime snapshot if needed.");
        if (!_snapshot.IsCaptured)
            _snapshot.Capture();
        else
            OPLog.Info("Snapshot", "Runtime snapshot was already captured. Startup capture skipped.");

        RuntimeDiagnostics.LogRuntimePreconditions("startup-pipeline/after-snapshot");
        OPLog.Info("Runtime", "Startup step 3/6: export vanilla catalogs.");
        ExportVanillaCatalogs();
        OPLog.Info("Runtime", "Startup step 4/6: bind runtime .cfg entries for discovered IDs.");
        BindRuntimeConfigEntries();
        LogFingerprints();
        OPLog.Info("Runtime", "Startup step 5/6: reset runtime state to snapshot before applying config.");
        ResetToSnapshot();
        OPLog.Info("Runtime", "Startup step 6/6: apply preset, .cfg overrides, multipliers, and gameplay rules.");
        ApplyRuntimeConfiguration();
        RuntimeDiagnostics.LogRuntimePreconditions("startup-pipeline/end");
        RuntimeDiagnostics.LogTestSignal("Runtime", "=== TEST SIGNAL: STARTUP PIPELINE END ===");
        metrics.Complete();
    }

    public void ReloadRuntimeConfiguration()
    {
        var metrics = PhaseMetrics.Start("Runtime", "reload-runtime-configuration");
        RuntimeDiagnostics.LogTestSignal("Runtime", "=== TEST SIGNAL: RUNTIME RELOAD BEGIN ===");
        OPLog.Info("Runtime", "Reloading runtime configuration from snapshot.");
        RuntimeDiagnostics.LogRuntimePreconditions("reload/begin");

        if (!_snapshot.IsCaptured)
        {
            OPLog.Warning("Runtime", "No runtime snapshot is available. Reload aborted.");
            metrics.Warning();
            metrics.Complete();
            RuntimeDiagnostics.LogTestSignal("Runtime", "=== TEST SIGNAL: RUNTIME RELOAD ABORTED ===");
            return;
        }

        OPConfig.Reload();
        LoadDataContracts();
        BindRuntimeConfigEntries();
        LogFingerprints();
        ResetToSnapshot();
        ApplyRuntimeConfiguration();
        RuntimeDiagnostics.LogRuntimePreconditions("reload/end");
        RuntimeDiagnostics.LogTestSignal("Runtime", "=== TEST SIGNAL: RUNTIME RELOAD END ===");
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
            if (OPConfig.DisableDiagnosticExportsAfterFirstRun.Value)
            {
                OPConfig.EnableDataExport.Value = false;
                OPConfig.ConfigFile.Save();
                OPLog.Info("Export", "Diagnostic exports completed once and were disabled by Utility.DisableDiagnosticExportsAfterFirstRun.");
            }
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

    public void BindRuntimeConfigEntries()
    {
        var metrics = PhaseMetrics.Start("Runtime", "bind-runtime-cfg-entries");
        var config = new RuntimeTuningConfig(OPConfig.ConfigFile);
        config.BuildItemOverrides();
        config.BuildMoonOverrides();
        config.BuildSpawnOverrides();
        config.BuildGameplayRouteRules();
        new InteriorFeature(OPConfig.ConfigFile).BindRuntimeConfigEntries();
        OPConfig.ConfigFile.Save();
        new LethalConfigBridgeFeature().RegisterDynamicConfig(OPConfig.ConfigFile);
        OPLog.Info("Config", "Runtime .cfg entries were bound and saved for discovered item, moon, and spawn IDs.");
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

        RuntimeDiagnostics.LogFeatureDecision("Multiplayer", OPConfig.EnableMultiplayer.Value, "resolve multiplayer .cfg rules");
        new LobbyRulesFeature().LoadOrCreate();

        RuntimeDiagnostics.LogFeatureDecision("Gameplay route rules", true, "resolve route multipliers from .cfg");
        new GameplayRulesFeature().LoadOrCreate();

        if (OPConfig.EnableMultiplayer.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Multiplayer", true, "apply max players and lobby metadata");
            var multiplayerFeature = new MultiplayerFeature();
            multiplayerFeature.Apply();
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Multiplayer", false, "skip multiplayer patches");
            OPLog.Info("Multiplayer", "Multiplayer disabled by config.");
        }
    }

    private static void ApplyRuntimeConfiguration()
    {
        OPLog.Info("Runtime", "Applying runtime configuration in precedence order: snapshot -> built-in preset -> .cfg explicit overrides -> .cfg multipliers/toggles -> .cfg gameplay rules.");
        RuntimeDiagnostics.LogRuntimePreconditions("apply-runtime-configuration/before");
        var presetFeature = new PresetFeature();
        var presetRuntimeSettings = presetFeature.ResolveRuntimeSettings();
        OPLog.Info(
            "Runtime",
            $"Resolved effective runtime settings: itemWeight={presetRuntimeSettings.ItemWeightMultiplier:0.###}, spawnRarity={presetRuntimeSettings.SpawnRarityMultiplier:0.###}, routePrice={presetRuntimeSettings.RoutePriceMultiplier:0.###}");

        if (OPConfig.EnableItemOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Item tuning", true, "load, validate, and apply .cfg item overrides");
            var itemOverrideFeature = new ItemOverrideFeature();
            itemOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Item tuning", false, "skip .cfg item overrides");
            OPLog.Info("Overrides", "Item tuning disabled by config.");
        }

        if (OPConfig.EnableSpawnOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Spawn tuning", true, "load, validate, and apply .cfg spawn pool overrides");
            var spawnOverrideFeature = new SpawnOverrideFeature();
            spawnOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Spawn tuning", false, "skip .cfg spawn pool overrides");
            OPLog.Info("Overrides", "Spawn tuning disabled by config.");
        }

        if (OPConfig.EnableMoonOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Moon tuning", true, "load, validate, and apply .cfg moon overrides");
            var moonOverrideFeature = new MoonOverrideFeature();
            moonOverrideFeature.ApplyOverrides(OPConfig.ActivePresetName);
        }
        else
        {
            RuntimeDiagnostics.LogFeatureDecision("Moon tuning", false, "skip .cfg moon overrides");
            OPLog.Info("Overrides", "Moon tuning disabled by config.");
        }

        if (OPConfig.EnableRuntimeMultipliers.Value && !OPConfig.DryRunOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Runtime multipliers", true, "apply effective cfg/preset multipliers");
            var runtimeMultiplierFeature = new RuntimeMultiplierFeature();
            runtimeMultiplierFeature.ApplyMultipliers(
                presetRuntimeSettings.ItemWeightMultiplier,
                presetRuntimeSettings.SpawnRarityMultiplier,
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

        if (!OPConfig.DryRunOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Gameplay route rules", true, "apply active .cfg route multiplier fields");
            var gameplayRulesFeature = new GameplayRulesFeature();
            var gameplayRules = gameplayRulesFeature.LoadOrCreate();
            gameplayRulesFeature.Apply(gameplayRules);

            RuntimeDiagnostics.LogFeatureDecision("Interior weights", true, "apply active .cfg interior weight fields");
            new InteriorFeature(OPConfig.ConfigFile).ApplyInteriorWeights();
        }
        else if (OPConfig.DryRunOverrides.Value)
        {
            RuntimeDiagnostics.LogFeatureDecision("Gameplay route rules", false, "dry-run: load but do not apply");
            OPLog.Info("Gameplay", "Dry-run enabled. Gameplay route rules were loaded but not applied.");
        }

        RuntimeDiagnostics.LogRuntimePreconditions("apply-runtime-configuration/after");
    }
}
