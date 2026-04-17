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

    public static string SpawnExportRoot =>
        Path.Combine(ExportRoot, "spawns");

    public static string EconomyExportRoot =>
        Path.Combine(ExportRoot, "economy");

    public static string PersistenceRoot =>
        Path.Combine(DataRoot, "saves");

    public static string DefinitionsRoot =>
        Path.Combine(DataRoot, "definitions");

    public static string ItemExportPath =>
        Path.Combine(ItemExportRoot, "items.json");

    public static string MoonExportPath =>
        Path.Combine(MoonExportRoot, "moons.json");

    public static string EnemyExportPath =>
        Path.Combine(EnemyExportRoot, "enemies.json");

    public static string SpawnProfileExportPath =>
        Path.Combine(SpawnExportRoot, "moon-spawn-profiles.json");

    public static string MoonEconomyExportPath =>
        Path.Combine(EconomyExportRoot, "moon-economy.json");

    public static string ProgressionSavePath =>
        Path.Combine(PersistenceRoot, "progression.json");

    public static string PerkCatalogPath =>
        Path.Combine(DefinitionsRoot, "perks.json");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(PluginRoot);
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(ExportRoot);
        Directory.CreateDirectory(ItemExportRoot);
        Directory.CreateDirectory(MoonExportRoot);
        Directory.CreateDirectory(EnemyExportRoot);
        Directory.CreateDirectory(SpawnExportRoot);
        Directory.CreateDirectory(EconomyExportRoot);
        Directory.CreateDirectory(PersistenceRoot);
        Directory.CreateDirectory(DefinitionsRoot);
    }
}
