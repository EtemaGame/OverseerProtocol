using HarmonyLib;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;
using OverseerProtocol.GameAbstractions.State;

namespace OverseerProtocol.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class ExportTriggerPatch
    {
        private static bool _initialExportCompleted = false;
        private static readonly RuntimeStateSnapshot _runtimeStateSnapshot = new();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void TriggerInitialExport()
        {
            if (!_initialExportCompleted)
            {
                OPLog.Info("Export", "Export trigger: running initial data processing.");
                OPLog.Info("Config", $"Active preset: {OPConfig.ActivePresetName}");
                var presetFeature = new PresetFeature();
                var presetRuntimeSettings = presetFeature.ResolveRuntimeSettings();

                if (OPConfig.EnableProgressionStorage.Value)
                {
                    var progressionStore = new ProgressionStore();
                    progressionStore.LoadOrCreate();
                }
                else
                {
                    OPLog.Info("Progression", "Progression storage disabled by config.");
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

                _runtimeStateSnapshot.Capture();
                
                // 1. Export vanilla state first (User Adjustment 4: ensure the "vanilla photo" is captured)
                if (OPConfig.EnableDataExport.Value)
                {
                    DataExportFeature.RunInitialExport();
                }
                else
                {
                    OPLog.Info("Export", "Data export disabled by config.");
                }

                _runtimeStateSnapshot.Restore();
                
                // 2. Apply overrides after export to modify runtime without polluting vanilla exports
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

                _initialExportCompleted = true;
            }
            else
            {
                OPLog.Info("Export", "Export trigger: processing already completed for this session, skipping.");
            }
        }
    }
}
