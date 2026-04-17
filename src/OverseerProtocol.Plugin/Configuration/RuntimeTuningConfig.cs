using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Rules;
using OverseerProtocol.Data.Models.Spawns;
using UnityEngine;

namespace OverseerProtocol.Configuration;

public sealed class RuntimeTuningConfig
{
    private readonly ConfigFile _config;

    public RuntimeTuningConfig(ConfigFile config)
    {
        _config = config;
    }

    public ItemOverrideCollection BuildItemOverrides()
    {
        var collection = new ItemOverrideCollection();
        var items = StartOfRound.Instance?.allItemsList?.itemsList;
        if (items == null)
        {
            OPLog.Warning("Config", "Runtime item catalog is unavailable. Item .cfg entries cannot be resolved.");
            return collection;
        }

        foreach (var item in items.OrderBy(item => item?.name, StringComparer.Ordinal))
        {
            if (item == null || string.IsNullOrWhiteSpace(item.name))
                continue;

            var id = item.name;
            var entry = _config.Bind(
                "Items",
                id,
                BuildObservedItemLine(item),
                "Entity item tuning. Set enabled=true to apply active fields. Active: value, weight, store, storePrice, minValue, maxValue. Reserved: battery.");

            var fields = EntityLine.Parse(entry.Value);
            if (!fields.Enabled)
                continue;

            WarnReservedItemFields(id, fields);

            var creditsWorth = fields.GetInt("value");
            var storePrice = fields.GetInt("storePrice");
            var weight = fields.GetFloat("weight");
            var addToStore = fields.GetBool("store");
            var minValue = fields.GetInt("minValue");
            var maxValue = fields.GetInt("maxValue");
            if (!creditsWorth.HasValue && storePrice.HasValue)
                creditsWorth = storePrice;

            if (!creditsWorth.HasValue && !weight.HasValue && addToStore != true && !minValue.HasValue && !maxValue.HasValue)
            {
                OPLog.Info("Config", $"Item '{id}' is enabled but has no active value/weight/store/range fields.");
                continue;
            }

            collection.Overrides.Add(new ItemOverrideDefinition
            {
                Id = id,
                CreditsWorth = creditsWorth,
                Weight = weight,
                AddToStore = addToStore,
                StorePrice = storePrice,
                MinValue = minValue,
                MaxValue = maxValue
            });
        }

        OPLog.Info("Config", $"Resolved item entity config: active={collection.Overrides.Count}");
        return collection;
    }

    public MoonOverrideCollection BuildMoonOverrides()
    {
        var collection = new MoonOverrideCollection();
        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
        {
            OPLog.Warning("Config", "Runtime moon catalog is unavailable. Moon .cfg entries cannot be resolved.");
            return collection;
        }

        for (var levelIndex = 0; levelIndex < levels.Length; levelIndex++)
        {
            var level = levels[levelIndex];
            if (level == null || string.IsNullOrWhiteSpace(level.name))
                continue;

            var id = level.name;
            var entry = _config.Bind(
                "Moons",
                id,
                BuildObservedMoonLine(level, levelIndex),
                "Entity moon tuning. Set enabled=true to apply active fields. Active: price, tier/riskLabel, riskLevel, description, minScrap, maxScrap. Reserved: interior.");

            var fields = EntityLine.Parse(entry.Value);
            if (!fields.Enabled)
                continue;

            WarnReservedMoonFields(id, fields);

            var routePrice = fields.GetInt("price");
            var riskLevel = fields.GetInt("riskLevel");
            var riskLabel = fields.GetString("tier");
            if (string.IsNullOrWhiteSpace(riskLabel))
                riskLabel = fields.GetString("riskLabel");

            var description = fields.GetString("description");
            var minScrap = fields.GetInt("minScrap");
            var maxScrap = fields.GetInt("maxScrap");

            if (!routePrice.HasValue &&
                !riskLevel.HasValue &&
                string.IsNullOrWhiteSpace(riskLabel) &&
                string.IsNullOrWhiteSpace(description) &&
                !minScrap.HasValue &&
                !maxScrap.HasValue)
            {
                OPLog.Info("Config", $"Moon '{id}' is enabled but has no active price/tier/description/scrap fields.");
                continue;
            }

            collection.Overrides.Add(new MoonOverrideDefinition
            {
                MoonId = id,
                RoutePrice = routePrice,
                RiskLevel = riskLevel,
                RiskLabel = riskLabel,
                Description = description,
                MinScrap = minScrap,
                MaxScrap = maxScrap
            });
        }

        OPLog.Info("Config", $"Resolved moon entity config: active={collection.Overrides.Count}");
        return collection;
    }

    public SpawnOverrideCollection BuildSpawnOverrides()
    {
        var collection = new SpawnOverrideCollection();
        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
        {
            OPLog.Warning("Config", "Runtime moon catalog is unavailable. Spawn .cfg entries cannot be resolved.");
            return collection;
        }

        foreach (var level in levels.OrderBy(level => level?.name, StringComparer.Ordinal))
        {
            if (level == null || string.IsNullOrWhiteSpace(level.name))
                continue;

            var id = level.name;
            var moonOverride = new MoonSpawnOverride
            {
                MoonId = id,
                InsideEnemies = ResolveEnemyPool(id, "Inside", level.Enemies),
                OutsideEnemies = ResolveEnemyPool(id, "Outside", level.OutsideEnemies),
                DaytimeEnemies = ResolveEnemyPool(id, "Daytime", level.DaytimeEnemies)
            };

            if (moonOverride.InsideEnemies == null &&
                moonOverride.OutsideEnemies == null &&
                moonOverride.DaytimeEnemies == null)
            {
                continue;
            }

            collection.Overrides.Add(moonOverride);
        }

        OPLog.Info("Config", $"Resolved moon enemy pool config: active={collection.Overrides.Count}");
        return collection;
    }

    public RuntimeRulesDefinition BuildRuntimeRules()
    {
        var rules = new RuntimeRulesDefinition();
        rules.Economy.TravelDiscountMultiplier = OPConfig.TravelDiscountMultiplier.Value;

        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
            return rules;

        foreach (var level in levels.OrderBy(level => level?.name, StringComparer.Ordinal))
        {
            if (level == null || string.IsNullOrWhiteSpace(level.name))
                continue;

            var entry = _config.Bind(
                "Moons.RouteMultiplier",
                level.name,
                "enabled=false; multiplier=1",
                "Optional per-moon route price multiplier. Set enabled=true to apply multiplier.");
            var fields = EntityLine.Parse(entry.Value);
            if (!fields.Enabled)
                continue;

            var multiplier = fields.GetFloat("multiplier") ?? 1f;
            if (Math.Abs(multiplier - 1f) < 0.0001f)
                continue;

            rules.MoonRules[level.name] = new MoonRuntimeRuleDefinition
            {
                RoutePriceMultiplier = multiplier
            };
        }

        OPLog.Info("Config", $"Resolved runtime rules config: travelDiscount={rules.Economy.TravelDiscountMultiplier:0.###}, moonRules={rules.MoonRules.Count}");
        return rules;
    }

    public void BindReservedScaffolds()
    {
        _config.Bind(
            "Interiors",
            "Reserved",
            "enabled=false; note=Interior selection, loot tables, and interior enemy tables need verified dungeon-flow hooks before runtime application.",
            "Reserved. Documents planned interior tuning without pretending it is active.");
        _config.Bind(
            "Utility",
            "Reserved",
            "enabled=false; note=FPS, latency, and connection tuning need separate verified performance/network patches.",
            "Reserved. Documents planned utility tuning without pretending it is active.");
        _config.Bind(
            "Perks",
            "Reserved",
            "enabled=false; note=Perk costs, max levels, and point sources need gameplay hooks before runtime application.",
            "Reserved. Documents planned perk tuning without pretending it is active.");
    }

    private List<SpawnEntry>? ResolveEnemyPool(string moonId, string poolName, List<SpawnableEnemyWithRarity>? observedPool)
    {
        var entry = _config.Bind(
            $"Moons.{poolName}Enemies",
            moonId,
            BuildObservedEnemyPoolLine(observedPool),
            "Moon enemy pool tuning. Set enabled=true to replace this pool. entries format: EnemyId:rarity, EnemyId:rarity.");

        var fields = EntityLine.Parse(entry.Value);
        if (!fields.Enabled)
            return null;

        var entries = fields.GetString("entries");
        if (string.IsNullOrWhiteSpace(entries))
        {
            OPLog.Info("Config", $"Moon '{moonId}' {poolName}Enemies enabled with empty entries. Runtime pool will be cleared.");
            return new List<SpawnEntry>();
        }

        var parsed = ParseSpawnEntries(entries, moonId, poolName);
        OPLog.Info("Config", $"Moon '{moonId}' {poolName}Enemies replacement requested: entries={parsed.Count}");
        return parsed;
    }

    private static string BuildObservedItemLine(Item item)
    {
        return "enabled=false; " +
               $"displayName={Escape(item.itemName)}; " +
               $"value={item.creditsWorth.ToString(CultureInfo.InvariantCulture)}; " +
               $"weight={item.weight.ToString("0.###", CultureInfo.InvariantCulture)}; " +
               $"scrap={Bool(item.isScrap)}; " +
               "store=false; storePrice=-1; " +
               $"battery={Bool(item.requiresBattery)}; " +
               $"minValue={item.minValue.ToString(CultureInfo.InvariantCulture)}; " +
               $"maxValue={item.maxValue.ToString(CultureInfo.InvariantCulture)}";
    }

    private static string BuildObservedMoonLine(SelectableLevel level, int levelIndex)
    {
        var routePrice = ResolveRoutePrice(levelIndex);
        var description = TryReadStringMember(level, "LevelDescription", "levelDescription", "sceneName") ?? "";
        var minScrap = ReadIntMember(level, "minScrap");
        var maxScrap = ReadIntMember(level, "maxScrap");
        return "enabled=false; " +
               $"displayName={Escape(level.PlanetName)}; " +
               $"price={routePrice.ToString(CultureInfo.InvariantCulture)}; " +
               $"tier={Escape(level.riskLevel)}; " +
               $"riskLevel={ParseRiskLevel(level.riskLevel).ToString(CultureInfo.InvariantCulture)}; " +
               $"description={Escape(description)}; " +
               $"minScrap={minScrap.ToString(CultureInfo.InvariantCulture)}; " +
               $"maxScrap={maxScrap.ToString(CultureInfo.InvariantCulture)}; " +
               "interior=reserved";
    }

    private static string BuildObservedEnemyPoolLine(List<SpawnableEnemyWithRarity>? pool)
    {
        return "enabled=false; entries=" + FormatEnemyPool(pool);
    }

    private static string FormatEnemyPool(List<SpawnableEnemyWithRarity>? pool)
    {
        if (pool == null || pool.Count == 0)
            return "";

        return string.Join(
            ", ",
            pool
                .Where(entry => entry?.enemyType != null && !string.IsNullOrWhiteSpace(entry.enemyType.name))
                .Select(entry => entry.enemyType.name + ":" + entry.rarity.ToString(CultureInfo.InvariantCulture)));
    }

    private static int ResolveRoutePrice(int levelIndex)
    {
        var nodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        var best = 0;
        if (nodes == null)
            return best;

        foreach (var node in nodes)
        {
            if (node == null || node.buyRerouteToMoon != levelIndex)
                continue;

            if (node.itemCost > 0 || best == 0)
                best = node.itemCost;
        }

        return best;
    }

    private static void WarnReservedItemFields(string itemId, EntityLine fields)
    {
        if (fields.GetBool("battery") == true)
            OPLog.Warning("Config", $"Item '{itemId}' battery is visible but reserved until battery runtime hooks are verified.");

    }

    private static void WarnReservedMoonFields(string moonId, EntityLine fields)
    {
        if (fields.GetString("interior") is { Length: > 0 } interior && !string.Equals(interior, "reserved", StringComparison.OrdinalIgnoreCase))
            OPLog.Warning("Config", $"Moon '{moonId}' interior is reserved until dungeon flow hooks are verified.");
    }

    private static List<SpawnEntry> ParseSpawnEntries(string raw, string moonId, string poolName)
    {
        var result = new List<SpawnEntry>();
        if (string.IsNullOrWhiteSpace(raw))
            return result;

        var parts = raw.Split(',');
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (part.Length == 0)
                continue;

            var separator = part.LastIndexOf(':');
            if (separator <= 0 || separator >= part.Length - 1)
            {
                OPLog.Warning("Config", $"Invalid spawn entry '{part}' in {moonId}/{poolName}. Expected EnemyId:rarity.");
                continue;
            }

            var enemyId = part.Substring(0, separator).Trim();
            var rarityText = part.Substring(separator + 1).Trim();
            if (!int.TryParse(rarityText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rarity))
            {
                OPLog.Warning("Config", $"Invalid rarity '{rarityText}' for enemy '{enemyId}' in {moonId}/{poolName}. Entry skipped.");
                continue;
            }

            result.Add(new SpawnEntry
            {
                EnemyId = enemyId,
                Rarity = rarity
            });
        }

        return result;
    }

    private static int ParseRiskLevel(string risk)
    {
        if (string.IsNullOrEmpty(risk)) return 0;
        if (risk.Contains("S")) return 5;
        if (risk.Contains("A")) return 4;
        if (risk.Contains("B")) return 3;
        if (risk.Contains("C")) return 2;
        if (risk.Contains("D")) return 1;
        return 0;
    }

    private static string? TryReadStringMember(object instance, params string[] names)
    {
        var type = instance.GetType();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var name in names)
        {
            var field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(string))
                return field.GetValue(instance) as string;

            var property = type.GetProperty(name, flags);
            if (property != null && property.PropertyType == typeof(string))
                return property.GetValue(instance, null) as string;
        }

        return null;
    }

    private static int ReadIntMember(object instance, params string[] names)
    {
        var type = instance.GetType();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var name in names)
        {
            var field = type.GetField(name, flags);
            if (field != null && field.FieldType == typeof(int))
                return (int)field.GetValue(instance);

            var property = type.GetProperty(name, flags);
            if (property != null && property.PropertyType == typeof(int))
                return (int)property.GetValue(instance, null);
        }

        return 0;
    }

    private static string Escape(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace(";", ",").Trim();

    private static string Bool(bool value) => value ? "true" : "false";

    private sealed class EntityLine
    {
        private readonly Dictionary<string, string> _values;

        private EntityLine(Dictionary<string, string> values)
        {
            _values = values;
        }

        public bool Enabled => GetBool("enabled") == true;

        public static EntityLine Parse(string? raw)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(raw))
                return new EntityLine(values);

            foreach (var part in raw.Split(';'))
            {
                var trimmed = part.Trim();
                if (trimmed.Length == 0)
                    continue;

                var separator = trimmed.IndexOf('=');
                if (separator <= 0)
                    continue;

                var key = trimmed.Substring(0, separator).Trim();
                var value = trimmed.Substring(separator + 1).Trim();
                values[key] = value;
            }

            return new EntityLine(values);
        }

        public string? GetString(string key) =>
            _values.TryGetValue(key, out var value) ? value : null;

        public int? GetInt(string key)
        {
            if (!_values.TryGetValue(key, out var value))
                return null;

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        public float? GetFloat(string key)
        {
            if (!_values.TryGetValue(key, out var value))
                return null;

            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        public bool? GetBool(string key)
        {
            if (!_values.TryGetValue(key, out var value))
                return null;

            if (bool.TryParse(value, out var parsed))
                return parsed;

            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return null;
        }
    }
}
