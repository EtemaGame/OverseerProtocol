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
            var section = "Items." + id;
            var inStore = IsStoreItem(item);

            var enabled = BindBool(section, "Enabled", false, "Enable edits for this item. When false, the values below are only a runtime catalog.");
            BindString(section, "DisplayName", item.itemName ?? id, "Observed display name. Informational.");
            var value = BindInt(section, "Value", item.creditsWorth, "Item creditsWorth / store price base value.", 0, 99999);
            var weight = BindFloat(section, "Weight", item.weight, "Item weight multiplier value used by Lethal Company.", 0f, 100f);
            var isScrap = BindBool(section, "IsScrap", item.isScrap, "Whether the item is considered scrap.");
            var addToStore = BindBool(section, "InStore", inStore, "Whether the item should be present in the Terminal store.");
            var storePrice = BindInt(section, "StorePrice", item.creditsWorth, "Price used when the item is in the Terminal store.", 0, 99999);
            var requiresBattery = BindBool(section, "RequiresBattery", item.requiresBattery, "Whether this item uses battery charge.");
            var isConductive = BindBool(section, "Conductive", item.isConductiveMetal, "Whether this item conducts lightning.");
            var twoHanded = BindBool(section, "TwoHanded", item.twoHanded, "Whether this item uses both hands.");
            var minValue = BindInt(section, "MinScrapValue", item.minValue, "Base minimum randomized scrap value. The final in-game value can be scaled by the moon total scrap budget.", 0, 99999);
            var maxValue = BindInt(section, "MaxScrapValue", item.maxValue, "Base maximum randomized scrap value. The final in-game value can be scaled by the moon total scrap budget.", 0, 99999);

            if (!enabled.Value)
                continue;

            collection.Overrides.Add(new ItemOverrideDefinition
            {
                Id = id,
                CreditsWorth = value.Value,
                Weight = weight.Value,
                IsScrap = isScrap.Value,
                AddToStore = addToStore.Value,
                StorePrice = storePrice.Value,
                RequiresBattery = requiresBattery.Value,
                IsConductiveMetal = isConductive.Value,
                TwoHanded = twoHanded.Value,
                MinValue = minValue.Value,
                MaxValue = maxValue.Value
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
            var section = "Moons." + id;
            var enabled = BindBool(section, "Enabled", false, "Enable edits for this moon. When false, the values below are only a runtime catalog.");
            BindString(section, "DisplayName", level.PlanetName ?? id, "Observed moon display name. Informational.");
            var price = BindInt(section, "RoutePrice", ResolveRoutePrice(levelIndex), "Terminal route price.", 0, 99999);
            var tier = BindString(section, "Tier", level.riskLevel ?? "None", "Risk label shown by the game. Examples: D, C, B, A, S, S+.");
            var riskLevel = BindInt(section, "RiskLevel", ParseRiskLevel(level.riskLevel), "Numeric helper for vanilla labels: 0=None, 1=D, 2=C, 3=B, 4=A, 5=S.", 0, 5);
            var description = BindString(section, "Description", TryReadStringMember(level, "LevelDescription", "levelDescription", "sceneName") ?? "", "Moon description text.");
            var minScrap = BindInt(section, "MinScrap", ReadIntMember(level, "minScrap"), "Minimum scrap items for this moon.", 0, 999);
            var maxScrap = BindInt(section, "MaxScrap", ReadIntMember(level, "maxScrap"), "Maximum scrap items for this moon.", 0, 999);
            var minTotalScrapValue = BindInt(section, "MinTotalScrapValue", level.minTotalScrapValue, "Minimum total value target for all scrap spawned on this moon.", 0, 99999);
            var maxTotalScrapValue = BindInt(section, "MaxTotalScrapValue", level.maxTotalScrapValue, "Maximum total value target for all scrap spawned on this moon.", 0, 99999);
            BindEnemyPool(section, "InsideEnemies", level.Enemies);
            BindEnemyPool(section, "OutsideEnemies", level.OutsideEnemies);
            BindEnemyPool(section, "DaytimeEnemies", level.DaytimeEnemies);
            BindBool(section, "RouteMultiplierEnabled", false, "Enable the per-moon route multiplier below.");
            BindFloat(section, "RouteMultiplier", 1f, "Additional route price multiplier for this moon.", 0f, 10f);

            if (!enabled.Value)
                continue;

            collection.Overrides.Add(new MoonOverrideDefinition
            {
                MoonId = id,
                RoutePrice = price.Value,
                RiskLevel = riskLevel.Value,
                RiskLabel = tier.Value,
                Description = description.Value,
                MinScrap = minScrap.Value,
                MaxScrap = maxScrap.Value,
                MinTotalScrapValue = minTotalScrapValue.Value,
                MaxTotalScrapValue = maxTotalScrapValue.Value
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
            var section = "Moons." + id;
            var moonOverride = new MoonSpawnOverride
            {
                MoonId = id,
                InsideEnemies = ResolveEnemyPool(section, id, "InsideEnemies"),
                OutsideEnemies = ResolveEnemyPool(section, id, "OutsideEnemies"),
                DaytimeEnemies = ResolveEnemyPool(section, id, "DaytimeEnemies")
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

    public GameplayRouteRulesDefinition BuildGameplayRouteRules()
    {
        var rules = new GameplayRouteRulesDefinition();
        rules.Economy.TravelDiscountMultiplier = OPConfig.TravelDiscountMultiplier.Value;

        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
            return rules;

        foreach (var level in levels.OrderBy(level => level?.name, StringComparer.Ordinal))
        {
            if (level == null || string.IsNullOrWhiteSpace(level.name))
                continue;

            var section = "Moons." + level.name;
            var enabled = BindBool(section, "RouteMultiplierEnabled", false, "Enable the per-moon route multiplier below.");
            var multiplier = BindFloat(section, "RouteMultiplier", 1f, "Additional route price multiplier for this moon.", 0f, 10f);
            if (!enabled.Value || Math.Abs(multiplier.Value - 1f) < 0.0001f)
                continue;

            rules.MoonRules[level.name] = new MoonGameplayRouteRuleDefinition
            {
                RoutePriceMultiplier = multiplier.Value
            };
        }

        OPLog.Info("Config", $"Resolved gameplay route config: travelDiscount={rules.Economy.TravelDiscountMultiplier:0.###}, moonRouteMultipliers={rules.MoonRules.Count}");
        return rules;
    }

    private List<SpawnEntry>? ResolveEnemyPool(string section, string moonId, string keyPrefix)
    {
        var enabled = BindBool(section, keyPrefix + "Enabled", false, $"Enable replacement for {keyPrefix}.");
        if (!enabled.Value)
            return null;

        var entries = BindString(section, keyPrefix, "", "Enemy pool entries. Format: EnemyId:rarity, EnemyId:rarity.");
        if (string.IsNullOrWhiteSpace(entries.Value))
        {
            OPLog.Info("Config", $"Moon '{moonId}' {keyPrefix} enabled with empty entries. Runtime pool will be cleared.");
            return new List<SpawnEntry>();
        }

        var parsed = ParseSpawnEntries(entries.Value, moonId, keyPrefix);
        OPLog.Info("Config", $"Moon '{moonId}' {keyPrefix} replacement requested: entries={parsed.Count}");
        return parsed;
    }

    private void BindEnemyPool(string section, string keyPrefix, List<SpawnableEnemyWithRarity>? pool)
    {
        BindBool(section, keyPrefix + "Enabled", false, $"Enable replacement for {keyPrefix}.");
        BindString(section, keyPrefix, FormatEnemyPool(pool), "Enemy pool entries. Format: EnemyId:rarity, EnemyId:rarity.");
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

    private static bool IsStoreItem(Item item)
    {
        var terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
        if (terminal?.buyableItemsList == null)
            return false;

        return terminal.buyableItemsList.Any(existing => existing != null && existing.name == item.name);
    }

    private ConfigEntry<bool> BindBool(string section, string key, bool value, string description) =>
        _config.Bind(section, key, value, description);

    private ConfigEntry<string> BindString(string section, string key, string value, string description) =>
        _config.Bind(section, key, value ?? "", description);

    private ConfigEntry<int> BindInt(string section, string key, int value, string description, int min, int max) =>
        _config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<int>(min, max)));

    private ConfigEntry<float> BindFloat(string section, string key, float value, string description, float min, float max) =>
        _config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));

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

    private static int ParseRiskLevel(string? risk)
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
}
