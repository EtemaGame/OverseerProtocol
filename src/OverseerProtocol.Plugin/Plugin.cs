using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using OverseerProtocol.Bootstrap;
using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Features;
using OverseerProtocol.Patches;

namespace OverseerProtocol
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.overseerprotocol.core";
        public const string ModName = "OverseerProtocol";
        public const string ModVersion = "0.1.0";

        internal static Plugin Instance = null!;
        internal static ManualLogSource Log = null!;
        private Harmony? _harmony;

        private const bool AutoExportGameData = true;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            
            Bootstrapper.Initialize(Logger);
            OPConfig.Bind(Config);
            PresetBootstrapFeature.EnsureBuiltInPresets();
            OPLog.Info("Bootstrap", $"{ModName} cargando core phase...");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();
            TerminalAdminCommandHook.TryPatch(_harmony);
            ExperimentalMultiplayerHook.TryPatch(_harmony);

            OPLog.Info("Bootstrap", "Harmony inicializado");

            if (AutoExportGameData)
            {
                // RunInitialExport se movió a ExportTriggerPatch para asegurar que los catálogos estén listos.
            }
        }
    }
}
