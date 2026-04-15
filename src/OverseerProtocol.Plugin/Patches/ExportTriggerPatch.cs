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
                OPLog.Info("Export trigger: running initial item export.");
                DataExportFeature.RunInitialExport();
                _initialExportCompleted = true;
            }
            else
            {
                OPLog.Info("Export trigger: initial export already completed, skipping.");
            }
        }
    }
}
