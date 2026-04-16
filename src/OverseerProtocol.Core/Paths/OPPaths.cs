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

    public static string OverridesRoot =>
        Path.Combine(DataRoot, "overrides");

    public static string PresetsRoot =>
        Path.Combine(DataRoot, "presets");

    public static string PersistenceRoot =>
        Path.Combine(DataRoot, "saves");

    public static string RulesRoot =>
        Path.Combine(DataRoot, "rules");

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

    public static string LobbyRulesPath =>
        Path.Combine(RulesRoot, "lobby-rules.json");

    public static string ItemOverridePath =>
        Path.Combine(OverridesRoot, "items.override.json");

    public static string SpawnOverridePath =>
        Path.Combine(OverridesRoot, "spawns.override.json");

    public static string MoonOverridePath =>
        Path.Combine(OverridesRoot, "moons.override.json");

    public static string GetItemOverridePath(string presetName) =>
        GetOverridePath("items.override.json", presetName);

    public static string GetSpawnOverridePath(string presetName) =>
        GetOverridePath("spawns.override.json", presetName);

    public static string GetMoonOverridePath(string presetName) =>
        GetOverridePath("moons.override.json", presetName);

    public static string GetPresetRoot(string presetName) =>
        Path.Combine(PresetsRoot, SanitizePathSegment(presetName));

    public static string GetPresetOverridesRoot(string presetName) =>
        Path.Combine(GetPresetRoot(presetName), "overrides");

    public static string GetPresetManifestPath(string presetName) =>
        Path.Combine(GetPresetRoot(presetName), "preset.json");

    public static string GetPresetRulesRoot(string presetName) =>
        Path.Combine(GetPresetRoot(presetName), "rules");

    public static string GetPresetLobbyRulesPath(string presetName) =>
        string.Equals(SanitizePathSegment(presetName), "default", System.StringComparison.OrdinalIgnoreCase)
            ? LobbyRulesPath
            : Path.Combine(GetPresetRulesRoot(presetName), "lobby-rules.json");

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
        Directory.CreateDirectory(OverridesRoot);
        Directory.CreateDirectory(PresetsRoot);
        Directory.CreateDirectory(PersistenceRoot);
        Directory.CreateDirectory(RulesRoot);
    }

    private static string GetOverridePath(string fileName, string presetName)
    {
        if (string.IsNullOrWhiteSpace(presetName) ||
            string.Equals(presetName, "default", System.StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(OverridesRoot, fileName);
        }

        return Path.Combine(GetPresetOverridesRoot(presetName), fileName);
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var chars = value.Trim().ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (System.Array.IndexOf(invalidChars, chars[i]) >= 0 || chars[i] == '/' || chars[i] == '\\')
                chars[i] = '_';
        }

        return new string(chars);
    }
}
