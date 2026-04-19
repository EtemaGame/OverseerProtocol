using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx;
using OverseerProtocol.Configuration;

namespace OverseerProtocol.Features.HostFlow.OverseerHostScreen;

internal sealed class OverseerHostPresetStore
{
    private const string CustomPrefix = "custom:";

    public IReadOnlyList<PresetSummary> LoadSummaries()
    {
        var result = new List<PresetSummary>();
        foreach (var file in EnumeratePresetFiles())
        {
            var id = CustomPrefix + Path.GetFileNameWithoutExtension(file);
            var values = ReadValues(file);
            result.Add(new PresetSummary(id, Get(values, "DisplayName", Path.GetFileNameWithoutExtension(file)), false));
        }

        return result;
    }

    public bool IsCustom(string id) => id.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase);

    public bool TryApply(string id, TuningDraftStore draft, out string error)
    {
        error = "";
        if (!IsCustom(id))
        {
            draft.ApplyPreset(LobbyPresetDefinition.Resolve(id));
            return true;
        }

        var file = PathForId(id);
        if (!File.Exists(file))
        {
            error = "Custom preset '" + id + "' was not found.";
            return false;
        }

        var values = ReadValues(file);
        draft.Lobby.PresetId = id;
        draft.Lobby.EnableMultiplayer = GetBool(values, "EnableMultiplayer", draft.Lobby.EnableMultiplayer);
        draft.Lobby.MaxPlayers = GetInt(values, "MaxPlayers", draft.Lobby.MaxPlayers);
        draft.Lobby.EnableLateJoin = GetBool(values, "EnableLateJoin", draft.Lobby.EnableLateJoin);
        draft.Lobby.LateJoinInOrbit = GetBool(values, "LateJoinInOrbit", draft.Lobby.LateJoinInOrbit);
        draft.Lobby.LateJoinOnMoonAsSpectator = GetBool(values, "LateJoinOnMoonAsSpectator", draft.Lobby.LateJoinOnMoonAsSpectator);
        draft.Lobby.RequireSameModVersion = GetBool(values, "RequireSameModVersion", draft.Lobby.RequireSameModVersion);
        draft.Lobby.RequireSameConfigHash = GetBool(values, "RequireSameConfigHash", draft.Lobby.RequireSameConfigHash);
        return true;
    }

    public PresetSummary SaveAs(string displayName, TuningDraftStore draft)
    {
        var safeName = SafeFileName(string.IsNullOrWhiteSpace(displayName) ? "New preset" : displayName.Trim());
        var finalName = NextAvailableName(safeName);
        var id = CustomPrefix + finalName;
        WritePreset(PathForId(id), displayName.Trim().Length == 0 ? finalName : displayName.Trim(), draft);
        draft.Lobby.PresetId = id;
        return new PresetSummary(id, displayName.Trim().Length == 0 ? finalName : displayName.Trim(), false);
    }

    public void Save(string id, string displayName, TuningDraftStore draft)
    {
        if (!IsCustom(id))
            throw new InvalidOperationException("Built-in presets cannot be overwritten.");

        WritePreset(PathForId(id), displayName, draft);
    }

    public PresetSummary Rename(string id, string displayName, TuningDraftStore draft)
    {
        if (!IsCustom(id))
            throw new InvalidOperationException("Built-in presets cannot be renamed.");

        var oldPath = PathForId(id);
        var finalName = NextAvailableName(SafeFileName(displayName));
        var newId = CustomPrefix + finalName;
        var newPath = PathForId(newId);
        if (File.Exists(oldPath))
            File.Delete(oldPath);

        WritePreset(newPath, displayName, draft);
        draft.Lobby.PresetId = newId;
        return new PresetSummary(newId, displayName, false);
    }

    public void Delete(string id)
    {
        if (!IsCustom(id))
            throw new InvalidOperationException("Built-in presets cannot be deleted.");

        var path = PathForId(id);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static IEnumerable<string> EnumeratePresetFiles()
    {
        var dir = PresetDirectory;
        if (!Directory.Exists(dir))
            yield break;

        foreach (var file in Directory.GetFiles(dir, "*.preset"))
            yield return file;
    }

    private static string PresetDirectory =>
        Path.Combine(Paths.ConfigPath, "OverseerProtocol", "presets", "host");

    private static string PathForId(string id)
    {
        var name = id.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase)
            ? id.Substring(CustomPrefix.Length)
            : id;
        return Path.Combine(PresetDirectory, SafeFileName(name) + ".preset");
    }

    private static string NextAvailableName(string name)
    {
        Directory.CreateDirectory(PresetDirectory);
        var candidate = name;
        var index = 2;
        while (File.Exists(Path.Combine(PresetDirectory, candidate + ".preset")))
        {
            candidate = name + " " + index.ToString(CultureInfo.InvariantCulture);
            index++;
        }

        return candidate;
    }

    private static void WritePreset(string path, string displayName, TuningDraftStore draft)
    {
        Directory.CreateDirectory(PresetDirectory);
        var lines = new[]
        {
            "DisplayName=" + Escape(displayName),
            "EnableMultiplayer=" + draft.Lobby.EnableMultiplayer,
            "MaxPlayers=" + Math.Max(1, Math.Min(64, draft.Lobby.MaxPlayers)).ToString(CultureInfo.InvariantCulture),
            "EnableLateJoin=" + draft.Lobby.EnableLateJoin,
            "LateJoinInOrbit=" + draft.Lobby.LateJoinInOrbit,
            "LateJoinOnMoonAsSpectator=" + draft.Lobby.LateJoinOnMoonAsSpectator,
            "RequireSameModVersion=" + draft.Lobby.RequireSameModVersion,
            "RequireSameConfigHash=" + draft.Lobby.RequireSameConfigHash
        };
        File.WriteAllLines(path, lines);
    }

    private static Dictionary<string, string> ReadValues(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var split = line.IndexOf('=');
            if (split <= 0)
                continue;

            result[line.Substring(0, split).Trim()] = Unescape(line.Substring(split + 1).Trim());
        }

        return result;
    }

    private static string Get(Dictionary<string, string> values, string key, string fallback) =>
        values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static bool GetBool(Dictionary<string, string> values, string key, bool fallback) =>
        values.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) ? parsed : fallback;

    private static int GetInt(Dictionary<string, string> values, string key, int fallback) =>
        values.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;

    private static string SafeFileName(string value)
    {
        var name = string.IsNullOrWhiteSpace(value) ? "New preset" : value.Trim();
        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name;
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "");

    private static string Unescape(string value) => value.Replace("\\n", "\n").Replace("\\\\", "\\");
}
