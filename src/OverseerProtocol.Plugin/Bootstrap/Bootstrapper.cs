using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;

namespace OverseerProtocol.Bootstrap;

public static class Bootstrapper
{
    public static void Initialize(BepInEx.Logging.ManualLogSource logger)
    {
        OPLog.Initialize(logger);
        OPPaths.EnsureDirectories();
        OPLog.Info("Directorios y Core de OverseerProtocol preparados.");
    }
}
