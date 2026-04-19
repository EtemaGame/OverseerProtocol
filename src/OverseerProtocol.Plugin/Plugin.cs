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
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.overseerprotocol.core";
        public const string ModName = "OverseerProtocol";
        public const string ModVersion = "0.1.0";

        internal static Plugin Instance = null!;
        internal static ManualLogSource Log = null!;
        internal static OverseerInputActions InputActions = null!;
        private Harmony? _harmony;
        private OverseerInfoPanel? _overseerInfoPanel;

        private const bool AutoExportGameData = true;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            
            Bootstrapper.Initialize(Logger);
            OPConfig.Bind(Config);
            try
            {
                InputActions = new OverseerInputActions();
            }
            catch (System.Exception ex)
            {
                OPLog.Warning("Input", $"InputUtils setup failed. Overseer panel keybind fallback will be unavailable this session: {ex.Message}");
            }
            RuntimeDiagnostics.LogBootSummary();
            OPLog.Info("Bootstrap", $"{ModName} cargando core phase...");

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll();
            TerminalAdminCommandHook.TryPatch(_harmony);
            MultiplayerHook.TryPatch(_harmony);
            OverseerHostEntryPatch.TryPatch(_harmony);
            gameObject.AddComponent<MultiplayerStatusHud>();
            _overseerInfoPanel = gameObject.AddComponent<OverseerInfoPanel>();
            _overseerInfoPanel.Initialize();

            OPLog.Info("Bootstrap", "Harmony inicializado");

            if (AutoExportGameData)
            {
                // RunInitialExport se movio a ExportTriggerPatch para asegurar que los catalogos esten listos.
            }
        }

        private void Update()
        {
            InputActions?.TickFallback();
            _overseerInfoPanel?.Tick();
        }
    }
}
