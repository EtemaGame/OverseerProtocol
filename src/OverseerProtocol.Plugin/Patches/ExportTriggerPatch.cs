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
                RuntimeServices.Orchestrator.RunStartupPipeline();

                _initialExportCompleted = true;
            }
            else
            {
                OPLog.Info("Export", "Export trigger: processing already completed for this session, skipping.");
            }
        }
    }
}
