using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.Features;

public sealed class InteriorFeature
{
    private readonly ConfigFile _config;

    public InteriorFeature(ConfigFile config)
    {
        _config = config;
    }

    public void BindRuntimeConfigEntries()
    {
        var catalog = BuildCatalog();
        var levels = StartOfRound.Instance?.levels;
        foreach (var interior in catalog)
        {
            var section = "Interiors." + interior.Id;
            _config.Bind(section, "Enabled", false, "Enable this interior for explicit moon interior weighting.");
            _config.Bind(section, "DisplayName", interior.DisplayName, "Observed interior display name.");
            _config.Bind(section, "RuntimeIndex", interior.Index, "Observed RoundManager.dungeonFlowTypes index. Informational; edit Moons.<MoonId>.InteriorWeights to use interiors.");
            _config.Bind(section, "UsedByMoons", FormatInteriorMoonUsage(interior, levels), "Moons whose vanilla interior weights include this interior. Informational.");
            _config.Bind(section, "ObservedWeights", FormatInteriorMoonWeights(interior, levels), "Observed per-moon interior weights. Informational.");
        }

        if (levels != null)
        {
            foreach (var level in levels.Where(level => level != null && !string.IsNullOrWhiteSpace(level.name)))
            {
                var section = "Moons." + level.name;
                _config.Bind(section, "InteriorWeightsEnabled", false, "Enable replacement for this moon's interior weights.");
                _config.Bind(section, "InteriorWeights", FormatInteriorWeights(level, catalog), "Interior weights. Format: Factory:300, Mansion:100, Mineshaft:50.");
            }
        }

        OPLog.Info("Interiors", $"Bound interior config entries: interiors={catalog.Count}, moons={levels?.Length ?? 0}");
    }

    public void ApplyInteriorWeights()
    {
        var catalog = BuildCatalog();
        if (catalog.Count == 0)
        {
            OPLog.Warning("Interiors", "No runtime interior catalog was found. Interior weights skipped.");
            return;
        }

        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
            return;

        var applied = 0;
        foreach (var level in levels.Where(level => level != null && !string.IsNullOrWhiteSpace(level.name)))
        {
            var section = "Moons." + level.name;
            var enabled = _config.Bind(section, "InteriorWeightsEnabled", false, "Enable replacement for this moon's interior weights.");
            var raw = _config.Bind(section, "InteriorWeights", FormatInteriorWeights(level, catalog), "Interior weights. Format: Factory:300, Mansion:100, Mineshaft:50.");
            if (!enabled.Value)
                continue;

            var parsed = ParseInteriorWeights(raw.Value, catalog, level.name);
            if (parsed.Count == 0)
            {
                OPLog.Warning("Interiors", $"Moon '{level.name}' has InteriorWeightsEnabled=true but no valid entries. Existing interiors were kept.");
                continue;
            }

            level.dungeonFlowTypes = parsed
                .Select(entry => new IntWithRarity(entry.Index, entry.Rarity, null))
                .ToArray();
            applied++;
            OPLog.Info("Interiors", $"Applied interior weights: moon={level.name}, entries={parsed.Count}");
        }

        if (applied == 0)
            OPLog.Info("Interiors", "No active interior weight edits found in .cfg.");
    }

    private static List<InteriorCatalogEntry> BuildCatalog()
    {
        var maps = RoundManager.Instance?.dungeonFlowTypes;
        var result = new List<InteriorCatalogEntry>();
        if (maps == null)
            return result;

        for (var index = 0; index < maps.Length; index++)
        {
            var map = maps[index];
            if (map == null)
                continue;

            var flow = map.GetType().GetField("dungeonFlow")?.GetValue(map);
            var flowName = flow == null
                ? ""
                : flow.GetType().GetProperty("name")?.GetValue(flow, null) as string;
            var displayName = string.IsNullOrWhiteSpace(flowName)
                ? "Interior" + index.ToString(CultureInfo.InvariantCulture)
                : flowName;
            var id = SanitizeId(displayName, index);
            result.Add(new InteriorCatalogEntry(id, displayName, index));
        }

        return result
            .GroupBy(entry => entry.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static string FormatInteriorWeights(SelectableLevel level, List<InteriorCatalogEntry> catalog)
    {
        if (level.dungeonFlowTypes == null || level.dungeonFlowTypes.Length == 0)
            return "";

        var byIndex = catalog.ToDictionary(entry => entry.Index, entry => entry.Id);
        var parts = new List<string>();
        foreach (var entry in level.dungeonFlowTypes)
        {
            if (entry == null || !byIndex.TryGetValue(entry.id, out var id))
                continue;

            parts.Add(id + ":" + entry.rarity.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(", ", parts);
    }

    private static string FormatInteriorMoonUsage(InteriorCatalogEntry interior, SelectableLevel[]? levels)
    {
        if (levels == null)
            return "";

        var moons = levels
            .Where(level => level?.dungeonFlowTypes != null && level.dungeonFlowTypes.Any(entry => entry != null && entry.id == interior.Index))
            .Select(level => level.name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return string.Join(", ", moons);
    }

    private static string FormatInteriorMoonWeights(InteriorCatalogEntry interior, SelectableLevel[]? levels)
    {
        if (levels == null)
            return "";

        var parts = new List<string>();
        foreach (var level in levels.Where(level => level != null && !string.IsNullOrWhiteSpace(level.name)).OrderBy(level => level.name, StringComparer.Ordinal))
        {
            if (level.dungeonFlowTypes == null)
                continue;

            var weight = level.dungeonFlowTypes
                .Where(entry => entry != null && entry.id == interior.Index)
                .Sum(entry => entry.rarity);
            if (weight <= 0)
                continue;

            parts.Add(level.name + ":" + weight.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(", ", parts);
    }

    private static List<InteriorWeightEntry> ParseInteriorWeights(string raw, List<InteriorCatalogEntry> catalog, string moonId)
    {
        var byId = catalog.ToDictionary(entry => entry.Id, StringComparer.OrdinalIgnoreCase);
        var result = new List<InteriorWeightEntry>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        foreach (var part in raw.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.Length == 0)
                continue;

            var separator = trimmed.LastIndexOf(':');
            if (separator <= 0 || separator >= trimmed.Length - 1)
            {
                OPLog.Warning("Interiors", $"Invalid interior entry '{trimmed}' in {moonId}. Expected InteriorId:rarity.");
                continue;
            }

            var id = trimmed.Substring(0, separator).Trim();
            var rarityText = trimmed.Substring(separator + 1).Trim();
            if (!byId.TryGetValue(id, out var interior))
            {
                OPLog.Warning("Interiors", $"Unknown interior id '{id}' in {moonId}. Entry skipped.");
                continue;
            }

            if (!int.TryParse(rarityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rarity) || rarity < 0)
            {
                OPLog.Warning("Interiors", $"Invalid interior rarity '{rarityText}' for '{id}' in {moonId}. Entry skipped.");
                continue;
            }

            result.Add(new InteriorWeightEntry(interior.Index, rarity));
        }

        return result;
    }

    private static string SanitizeId(string displayName, int index)
    {
        var builder = new StringBuilder();
        foreach (var character in displayName)
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(character);
        }

        return builder.Length == 0 ? "Interior" + index.ToString(CultureInfo.InvariantCulture) : builder.ToString();
    }

    private sealed class InteriorCatalogEntry
    {
        public InteriorCatalogEntry(string id, string displayName, int index)
        {
            Id = id;
            DisplayName = displayName;
            Index = index;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Index { get; }
    }

    private sealed class InteriorWeightEntry
    {
        public InteriorWeightEntry(int index, int rarity)
        {
            Index = index;
            Rarity = rarity;
        }

        public int Index { get; }
        public int Rarity { get; }
    }
}
