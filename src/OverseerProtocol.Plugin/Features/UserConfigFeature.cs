using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Enemies;
using OverseerProtocol.Data.Models.Economy;
using OverseerProtocol.Data.Models.Items;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.Data.Models.Spawns;
using OverseerProtocol.Data.Models.UserConfig;

namespace OverseerProtocol.Features;

public sealed class UserConfigFeature
{
    public void EnsureUserFiles()
    {
        OPLog.Info("UserConfig", "Ensuring human-editable config files: items.json, moons/<MoonId>.json, utility-catalog.json.");

        var items = JsonFileReader.Read<List<ItemDefinition>>(OPPaths.ItemExportPath) ?? new List<ItemDefinition>();
        var moons = JsonFileReader.Read<List<MoonDefinition>>(OPPaths.MoonExportPath) ?? new List<MoonDefinition>();
        var enemies = JsonFileReader.Read<List<EnemyDefinition>>(OPPaths.EnemyExportPath) ?? new List<EnemyDefinition>();
        var spawnProfiles = JsonFileReader.Read<List<MoonSpawnProfile>>(OPPaths.SpawnProfileExportPath) ?? new List<MoonSpawnProfile>();
        var economyProfiles = JsonFileReader.Read<List<MoonEconomyProfile>>(OPPaths.MoonEconomyExportPath) ?? new List<MoonEconomyProfile>();

        WriteUtilityCatalog(items, moons, enemies, spawnProfiles);
        WriteItemsConfig(items);
        WriteMoonConfigs(moons, spawnProfiles, economyProfiles);

        OPLog.Info("UserConfig", "Human-editable config files are ready.");
    }

    private static void WriteUtilityCatalog(
        List<ItemDefinition> items,
        List<MoonDefinition> moons,
        List<EnemyDefinition> enemies,
        List<MoonSpawnProfile> spawnProfiles)
    {
        var catalog = new UtilityCatalogFile
        {
            Items = items.OrderBy(item => item.Id, StringComparer.Ordinal).ToList(),
            Moons = moons.OrderBy(moon => moon.LevelIndex).ToList(),
            Enemies = enemies.OrderBy(enemy => enemy.Id, StringComparer.Ordinal).ToList(),
            MoonSpawnProfiles = spawnProfiles.OrderBy(profile => profile.MoonId, StringComparer.Ordinal).ToList()
        };

        JsonFileWriter.Write(OPPaths.UtilityCatalogPath, catalog);
        OPLog.Info("UserConfig", $"Wrote utility catalog: items={catalog.Items.Count}, moons={catalog.Moons.Count}, enemies={catalog.Enemies.Count}, spawnProfiles={catalog.MoonSpawnProfiles.Count}, path={OPPaths.UtilityCatalogPath}");
    }

    private static void WriteItemsConfig(List<ItemDefinition> exportedItems)
    {
        var existing = JsonFileReader.Read<UserItemConfigFile>(OPPaths.ItemsConfigPath);
        if (existing == null && File.Exists(OPPaths.ItemsConfigPath))
        {
            OPLog.Warning("UserConfig", $"Existing item tuning file could not be read and will not be overwritten: {OPPaths.ItemsConfigPath}");
            return;
        }

        existing ??= new UserItemConfigFile();
        var existingById = existing.Items
            .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var result = new UserItemConfigFile
        {
            GeneratedUtc = string.IsNullOrWhiteSpace(existing.GeneratedUtc)
                ? DateTime.UtcNow.ToString("O")
                : existing.GeneratedUtc,
            Notes = string.IsNullOrWhiteSpace(existing.Notes)
                ? new UserItemConfigFile().Notes
                : existing.Notes
        };
        var exportedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var exported in exportedItems.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            exportedIds.Add(exported.Id);
            existingById.TryGetValue(exported.Id, out var previous);

            result.Items.Add(new UserItemConfig
            {
                Id = exported.Id,
                Enabled = previous?.Enabled ?? true,
                MissingFromRuntime = false,
                Observed = new ObservedItemConfig
                {
                    DisplayName = exported.DisplayName,
                    InternalName = exported.InternalName,
                    CreditsWorth = exported.CreditsWorth,
                    Weight = exported.Weight,
                    IsScrap = exported.IsScrap,
                    IsConductiveMetal = exported.IsConductiveMetal,
                    RequiresBattery = exported.RequiresBattery,
                    SpawnPrefabName = exported.SpawnPrefabName
                },
                Override = previous?.Override ?? new ItemOverrideConfig(),
                Store = previous?.Store ?? new ItemStoreConfig(),
                Battery = previous?.Battery ?? new ItemBatteryConfig(),
                Spawn = previous?.Spawn ?? new ItemSpawnConfig()
            });
        }

        foreach (var previous in existingById.Values.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (exportedIds.Contains(previous.Id))
                continue;

            previous.MissingFromRuntime = true;
            result.Items.Add(previous);
            OPLog.Warning("UserConfig", $"Item '{previous.Id}' exists in items.json but was not found in the current runtime exports.");
        }

        JsonFileWriter.Write(OPPaths.ItemsConfigPath, result);
        OPLog.Info("UserConfig", $"Wrote item tuning file: itemCount={result.Items.Count}, path={OPPaths.ItemsConfigPath}");
    }

    private static void WriteMoonConfigs(
        List<MoonDefinition> exportedMoons,
        List<MoonSpawnProfile> spawnProfiles,
        List<MoonEconomyProfile> economyProfiles)
    {
        var spawnByMoon = spawnProfiles
            .Where(profile => profile != null && !string.IsNullOrWhiteSpace(profile.MoonId))
            .ToDictionary(profile => profile.MoonId, profile => profile, StringComparer.Ordinal);
        var economyByMoon = economyProfiles
            .Where(profile => profile != null && !string.IsNullOrWhiteSpace(profile.MoonId))
            .ToDictionary(profile => profile.MoonId, profile => profile, StringComparer.Ordinal);

        foreach (var moon in exportedMoons.OrderBy(moon => moon.LevelIndex))
        {
            var path = OPPaths.GetMoonConfigPath(moon.Id);
            var previous = JsonFileReader.Read<UserMoonConfigFile>(path);
            if (previous == null && File.Exists(path))
            {
                OPLog.Warning("UserConfig", $"Existing moon tuning file could not be read and will not be overwritten: {path}");
                continue;
            }

            spawnByMoon.TryGetValue(moon.Id, out var spawnProfile);
            economyByMoon.TryGetValue(moon.Id, out var economyProfile);

            var config = new UserMoonConfigFile
            {
                GeneratedUtc = previous == null || string.IsNullOrWhiteSpace(previous.GeneratedUtc)
                    ? DateTime.UtcNow.ToString("O")
                    : previous.GeneratedUtc,
                MoonId = moon.Id,
                Enabled = previous?.Enabled ?? true,
                MissingFromRuntime = false,
                Notes = previous == null || string.IsNullOrWhiteSpace(previous.Notes)
                    ? new UserMoonConfigFile().Notes
                    : previous.Notes,
                Observed = new ObservedMoonConfig
                {
                    DisplayName = moon.DisplayName,
                    InternalName = moon.InternalName,
                    LevelIndex = moon.LevelIndex,
                    RoutePrice = economyProfile?.RoutePrice ?? moon.RoutePrice,
                    RiskLevel = moon.RiskLevel,
                    RiskLabel = GetRuntimeRiskLabel(moon.Id)
                },
                Override = previous?.Override ?? new MoonOverrideConfig(),
                Spawns = MergeSpawnPools(previous?.Spawns, spawnProfile),
                Scrap = previous?.Scrap ?? new MoonScrapConfig(),
                Items = previous?.Items ?? new MoonItemPoolConfig()
            };

            JsonFileWriter.Write(path, config);
            OPLog.Info("UserConfig", $"Wrote moon tuning file: moon={moon.Id}, path={path}");
        }

        MarkMissingMoonFiles(exportedMoons);
    }

    private static MoonSpawnPoolsConfig MergeSpawnPools(MoonSpawnPoolsConfig? previous, MoonSpawnProfile? observed)
    {
        return new MoonSpawnPoolsConfig
        {
            InsideEnemies = MergePool(previous?.InsideEnemies, observed?.InsideEnemies),
            OutsideEnemies = MergePool(previous?.OutsideEnemies, observed?.OutsideEnemies),
            DaytimeEnemies = MergePool(previous?.DaytimeEnemies, observed?.DaytimeEnemies)
        };
    }

    private static SpawnPoolConfig MergePool(SpawnPoolConfig? previous, List<SpawnEntry>? observedEntries)
    {
        var mode = string.IsNullOrWhiteSpace(previous?.Mode) ? "keep" : previous!.Mode.Trim();
        var keepMode = string.Equals(mode, "keep", StringComparison.OrdinalIgnoreCase);
        var entries = keepMode
            ? CloneEntries(observedEntries)
            : CloneEntries(previous?.Entries);

        return new SpawnPoolConfig
        {
            Mode = mode,
            Entries = entries
        };
    }

    private static List<SpawnEntry> CloneEntries(List<SpawnEntry>? entries)
    {
        var result = new List<SpawnEntry>();
        if (entries == null)
            return result;

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            result.Add(new SpawnEntry
            {
                EnemyId = entry.EnemyId,
                Rarity = entry.Rarity
            });
        }

        return result;
    }

    private static void MarkMissingMoonFiles(List<MoonDefinition> exportedMoons)
    {
        var exportedIds = new HashSet<string>(exportedMoons.Select(moon => moon.Id), StringComparer.Ordinal);
        if (!Directory.Exists(OPPaths.MoonConfigRoot))
            return;

        foreach (var path in Directory.GetFiles(OPPaths.MoonConfigRoot, "*.json"))
        {
            var config = JsonFileReader.Read<UserMoonConfigFile>(path);
            if (config == null)
            {
                OPLog.Warning("UserConfig", $"Moon tuning file could not be read while checking missing moons. Leaving it unchanged: {path}");
                continue;
            }

            if (string.IsNullOrWhiteSpace(config.MoonId) || exportedIds.Contains(config.MoonId))
                continue;

            config.MissingFromRuntime = true;
            JsonFileWriter.Write(path, config);
            OPLog.Warning("UserConfig", $"Moon config '{config.MoonId}' exists but was not found in the current runtime exports. Marked missingFromRuntime=true.");
        }
    }

    private static string GetRuntimeRiskLabel(string moonId)
    {
        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
            return "";

        foreach (var level in levels)
        {
            if (level != null && string.Equals(level.name, moonId, StringComparison.Ordinal))
                return level.riskLevel ?? "";
        }

        return "";
    }
}
