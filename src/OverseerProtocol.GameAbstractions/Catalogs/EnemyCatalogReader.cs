using System.Collections.Generic;
using System.Linq;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Enemies;

namespace OverseerProtocol.GameAbstractions.Catalogs;

public class EnemyCatalogReader
{
    public List<EnemyDefinition> ReadAllEnemies()
    {
        var enemyMap = new Dictionary<string, EnemyDefinition>();

        if (StartOfRound.Instance == null)
        {
            OPLog.Warning("Enemies", "StartOfRound.Instance is null. Cannot read enemy catalog yet.");
            return new List<EnemyDefinition>();
        }

        var levels = StartOfRound.Instance.levels;
        if (levels == null) return new List<EnemyDefinition>();

        OPLog.Info("Enemies", $"Scanning {levels.Length} levels for enemy types...");

        foreach (var level in levels)
        {
            if (level == null) continue;

            // Pool 1: Inside Enemies
            ProcessPool(level.Enemies, enemyMap, observedInside: true);

            // Pool 2: Outside Enemies
            ProcessPool(level.OutsideEnemies, enemyMap, observedOutside: true);

            // Pool 3: Daytime Enemies
            ProcessPool(level.DaytimeEnemies, enemyMap, observedDaytime: true);
        }

        var result = enemyMap.Values.OrderBy(e => e.Id).ToList();
        OPLog.Info("Enemies", $"Found {result.Count} unique enemy types across all level pools.");
        
        return result;
    }

    private void ProcessPool(List<SpawnableEnemyWithRarity> pool, Dictionary<string, EnemyDefinition> map, 
        bool observedInside = false, bool observedOutside = false, bool observedDaytime = false)
    {
        if (pool == null) return;

        foreach (var entry in pool)
        {
            if (entry == null || entry.enemyType == null) continue;

            var type = entry.enemyType;
            string key = type.name;

            if (!map.TryGetValue(key, out var def))
            {
                def = new EnemyDefinition
                {
                    Id = type.name,
                    InternalName = type.enemyName,
                    DisplayName = type.enemyName,
                    PowerLevel = (int)type.PowerLevel,
                    MaxCount = type.MaxCount,
                    IsOutsideEnemy = type.isOutsideEnemy,
                    IsDaytimeEnemy = type.isDaytimeEnemy
                };
                map[key] = def;
            }

            // Aggregate observations
            if (observedInside) def.ObservedInside = true;
            if (observedOutside) def.ObservedOutside = true;
            if (observedDaytime) def.ObservedDaytime = true;
        }
    }
}
