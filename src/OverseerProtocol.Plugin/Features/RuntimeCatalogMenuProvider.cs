using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Enemies;
using OverseerProtocol.Data.Models.Items;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.Data.Models.Spawns;
using UnityEngine;

namespace OverseerProtocol.Features;

internal sealed class RuntimeCatalogMenuProvider
{
    public List<MenuMoonRecord> LoadMoons()
    {
        var levels = StartOfRound.Instance?.levels;
        if (levels != null)
        {
            var moons = new List<MenuMoonRecord>();
            for (var i = 0; i < levels.Length; i++)
            {
                var level = levels[i];
                if (level == null || string.IsNullOrWhiteSpace(level.name))
                    continue;

                moons.Add(new MenuMoonRecord
                {
                    Id = level.name,
                    DisplayName = string.IsNullOrWhiteSpace(level.PlanetName) ? level.name : level.PlanetName,
                    LevelIndex = i,
                    RoutePrice = ResolveRoutePrice(i),
                    RiskLabel = level.riskLevel ?? "",
                    RiskLevel = ParseRiskLevel(level.riskLevel),
                    Description = TryReadStringMember(level, "LevelDescription", "levelDescription", "sceneName") ?? "",
                    MinScrap = ReadIntMember(level, "minScrap"),
                    MaxScrap = ReadIntMember(level, "maxScrap"),
                    MinTotalScrapValue = level.minTotalScrapValue,
                    MaxTotalScrapValue = level.maxTotalScrapValue
                });
            }

            return moons.OrderBy(moon => moon.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        var exported = JsonFileReader.Read<List<MoonDefinition>>(OPPaths.MoonExportPath);
        return exported == null
            ? new List<MenuMoonRecord>()
            : exported
                .Where(moon => !string.IsNullOrWhiteSpace(moon.Id))
                .Select(moon => new MenuMoonRecord
                {
                    Id = moon.Id,
                    DisplayName = string.IsNullOrWhiteSpace(moon.DisplayName) ? moon.Id : moon.DisplayName,
                    LevelIndex = moon.LevelIndex,
                    RoutePrice = moon.RoutePrice,
                    RiskLevel = moon.RiskLevel,
                    RiskLabel = FormatRiskLabel(moon.RiskLevel)
                })
                .OrderBy(moon => moon.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public List<MenuItemRecord> LoadItems()
    {
        var runtimeItems = StartOfRound.Instance?.allItemsList?.itemsList;
        if (runtimeItems != null)
        {
            return runtimeItems
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.name))
                .Select(item => new MenuItemRecord
                {
                    Id = item.name,
                    DisplayName = string.IsNullOrWhiteSpace(item.itemName) ? item.name : item.itemName,
                    Value = item.creditsWorth,
                    Weight = item.weight,
                    IsScrap = item.isScrap,
                    MinScrapValue = item.minValue,
                    MaxScrapValue = item.maxValue,
                    RequiresBattery = item.requiresBattery,
                    Conductive = item.isConductiveMetal,
                    TwoHanded = item.twoHanded
                })
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var exported = JsonFileReader.Read<List<ItemDefinition>>(OPPaths.ItemExportPath);
        return exported == null
            ? new List<MenuItemRecord>()
            : exported
                .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                .Select(item => new MenuItemRecord
                {
                    Id = item.Id,
                    DisplayName = string.IsNullOrWhiteSpace(item.DisplayName) ? item.Id : item.DisplayName,
                    Value = item.CreditsWorth,
                    Weight = item.Weight,
                    IsScrap = item.IsScrap,
                    RequiresBattery = item.RequiresBattery,
                    Conductive = item.IsConductiveMetal
                })
                .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public List<string> LoadEnemyIds()
    {
        var registry = new OverseerProtocol.GameAbstractions.Overrides.EnemyTypeRegistry();
        registry.BuildRegistry();
        if (registry.EnemyIds.Any())
            return registry.EnemyIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();

        var exported = JsonFileReader.Read<List<EnemyDefinition>>(OPPaths.EnemyExportPath);
        return exported == null
            ? new List<string>()
            : exported
                .Where(enemy => !string.IsNullOrWhiteSpace(enemy.Id))
                .Select(enemy => enemy.Id)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    public List<MenuSpawnRecord> LoadSpawns()
    {
        var profiles = new OverseerProtocol.GameAbstractions.Catalogs.MoonSpawnCatalogReader().ReadAllSpawnProfiles();
        if (profiles.Count == 0)
            profiles = JsonFileReader.Read<List<MoonSpawnProfile>>(OPPaths.SpawnProfileExportPath) ?? new List<MoonSpawnProfile>();

        return profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.MoonId))
            .Select(profile => new MenuSpawnRecord
            {
                MoonId = profile.MoonId,
                InsideEnemies = FormatPool(profile.InsideEnemies),
                OutsideEnemies = FormatPool(profile.OutsideEnemies),
                DaytimeEnemies = FormatPool(profile.DaytimeEnemies)
            })
            .OrderBy(profile => profile.MoonId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string FormatPool(List<SpawnEntry>? entries)
    {
        if (entries == null || entries.Count == 0)
            return "";

        return string.Join(
            ", ",
            entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.EnemyId))
                .Select(entry => entry.EnemyId + ":" + entry.Rarity.ToString(CultureInfo.InvariantCulture)));
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

    private static string FormatRiskLabel(int riskLevel) =>
        riskLevel switch
        {
            1 => "D",
            2 => "C",
            3 => "B",
            4 => "A",
            5 => "S",
            _ => "None"
        };

    private static string? TryReadStringMember(object instance, params string[] names)
    {
        var type = instance.GetType();
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
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
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
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

internal sealed class MenuMoonRecord
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int LevelIndex { get; set; }
    public int RoutePrice { get; set; }
    public int RiskLevel { get; set; }
    public string RiskLabel { get; set; } = "";
    public string Description { get; set; } = "";
    public int MinScrap { get; set; }
    public int MaxScrap { get; set; }
    public int MinTotalScrapValue { get; set; }
    public int MaxTotalScrapValue { get; set; }
}

internal sealed class MenuItemRecord
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Value { get; set; }
    public float Weight { get; set; }
    public bool IsScrap { get; set; }
    public int MinScrapValue { get; set; }
    public int MaxScrapValue { get; set; }
    public bool RequiresBattery { get; set; }
    public bool Conductive { get; set; }
    public bool TwoHanded { get; set; }
}

internal sealed class MenuSpawnRecord
{
    public string MoonId { get; set; } = "";
    public string InsideEnemies { get; set; } = "";
    public string OutsideEnemies { get; set; } = "";
    public string DaytimeEnemies { get; set; } = "";
}
