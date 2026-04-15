namespace OverseerProtocol.Core.Logging;

public static class OPLog
{
    private static BepInEx.Logging.ManualLogSource? _log;

    public static void Initialize(BepInEx.Logging.ManualLogSource log)
    {
        _log = log;
    }

    public static void Info(string message) => _log?.LogInfo(message);
    public static void Warning(string message) => _log?.LogWarning(message);
    public static void Error(string message) => _log?.LogError(message);
    public static void Debug(string message) => _log?.LogDebug(message);
}
