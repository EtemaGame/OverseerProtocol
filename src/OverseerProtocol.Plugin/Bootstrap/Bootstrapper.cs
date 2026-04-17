using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;

namespace OverseerProtocol.Bootstrap;

public static class Bootstrapper
{
    public static void Initialize(BepInEx.Logging.ManualLogSource logger)
    {
        OPLog.Initialize(logger);
        OPPaths.EnsureDirectories();
        OPLog.Info("Bootstrap", "Directorios y core de OverseerProtocol preparados.");
        OPLog.Info("Bootstrap", $"Data root asegurado en {OPPaths.DataRoot}");
    }
}
