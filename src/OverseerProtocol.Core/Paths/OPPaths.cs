using System.IO;
using BepInEx;

namespace OverseerProtocol.Core.Paths;

public static class OPPaths
{
    public static string PluginRoot =>
        Path.Combine(BepInEx.Paths.PluginPath, "OverseerProtocol");

    public static string DataRoot =>
        Path.Combine(PluginRoot, "overseer-data");

    public static string ExportRoot =>
        Path.Combine(DataRoot, "exports");

    public static string ItemExportRoot =>
        Path.Combine(ExportRoot, "items");

    public static string MoonExportRoot =>
        Path.Combine(ExportRoot, "moons");

    public static string EnemyExportRoot =>
        Path.Combine(ExportRoot, "enemies");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(PluginRoot);
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(ExportRoot);
        Directory.CreateDirectory(ItemExportRoot);
        Directory.CreateDirectory(MoonExportRoot);
        Directory.CreateDirectory(EnemyExportRoot);
    }
}
