using HarmonyLib;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;

namespace OverseerProtocol.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class ExportTriggerPatch
    {
        private static bool _initialExportCompleted = false;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void TriggerInitialExport()
        {
            if (!_initialExportCompleted)
            {
                OPLog.Info("Export", "Export trigger: running initial data processing.");
                
                // 1. Export vanilla state first (User Adjustment 4: ensure the "vanilla photo" is captured)
                DataExportFeature.RunInitialExport();
                
                // 2. Apply overrides after export to modify runtime without polluting vanilla exports
                var overrideFeature = new ItemOverrideFeature();
                overrideFeature.ApplyOverrides();

                _initialExportCompleted = true;
            }
            else
            {
                OPLog.Info("Export", "Export trigger: processing already completed for this session, skipping.");
            }
        }
    }
}
