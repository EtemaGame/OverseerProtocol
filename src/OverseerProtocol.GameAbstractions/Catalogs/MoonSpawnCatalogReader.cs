using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.GameAbstractions.Catalogs;

public class MoonSpawnCatalogReader
{
    public List<MoonSpawnProfile> ReadAllSpawnProfiles()
    {
        var profiles = new List<MoonSpawnProfile>();

        if (StartOfRound.Instance == null)
        {
            OPLog.Warning("Spawns", "StartOfRound.Instance is null. Cannot read spawn profiles.");
            return profiles;
        }

        var levels = StartOfRound.Instance.levels;
        if (levels == null) return profiles;

        OPLog.Info("Spawns", $"Mapping spawn profiles for {levels.Length} levels...");

        foreach (var level in levels)
        {
            if (level == null) continue;

            var profile = new MoonSpawnProfile
            {
                MoonId = level.name,
                InsideEnemies = MapPool(level.Enemies, "Inside", level.name),
                OutsideEnemies = MapPool(level.OutsideEnemies, "Outside", level.name),
                DaytimeEnemies = MapPool(level.DaytimeEnemies, "Daytime", level.name)
            };

            profiles.Add(profile);
        }

        return profiles;
    }

    private List<SpawnEntry> MapPool(List<SpawnableEnemyWithRarity> pool, string poolName, string moonId)
    {
        var entries = new List<SpawnEntry>();
        if (pool == null) return entries;

        foreach (var entry in pool)
        {
            if (entry == null || entry.enemyType == null)
            {
                OPLog.Warning("Spawns", $"Found null entry or enemyType in {poolName} pool of {moonId}. Skipping.");
                continue;
            }

            // Cross-reference validation: In our catalog, Id = type.name
            if (string.IsNullOrEmpty(entry.enemyType.name))
            {
                OPLog.Warning("Spawns", $"EnemyType in {poolName} pool of {moonId} has no name/ID. Skipping.");
                continue;
            }

            entries.Add(new SpawnEntry
            {
                EnemyId = entry.enemyType.name,
                Rarity = entry.rarity
            });
        }

        return entries;
    }
}
