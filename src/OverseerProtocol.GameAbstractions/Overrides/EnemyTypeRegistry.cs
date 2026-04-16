using System.Collections.Generic;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class EnemyTypeRegistry
{
    private readonly Dictionary<string, EnemyType> _registry = new();

    public void BuildRegistry()
    {
        _registry.Clear();

        if (StartOfRound.Instance == null || StartOfRound.Instance.levels == null)
        {
            OPLog.Warning("Registry", "StartOfRound levels not available. Registry cannot be built.");
            return;
        }

        OPLog.Info("Registry", "Scanning levels to build enemy type registry...");

        foreach (var level in StartOfRound.Instance.levels)
        {
            if (level == null) continue;

            ScanPool(level.Enemies, "Inside", level.name);
            ScanPool(level.OutsideEnemies, "Outside", level.name);
            ScanPool(level.DaytimeEnemies, "Daytime", level.name);
        }

        OPLog.Info("Registry", $"Registry built: Found {_registry.Count} unique enemy types.");
    }

    private void ScanPool(List<SpawnableEnemyWithRarity> pool, string poolName, string moonId)
    {
        if (pool == null) return;

        foreach (var entry in pool)
        {
            if (entry?.enemyType == null) continue;

            var type = entry.enemyType;
            var id = type.name;

            if (_registry.TryGetValue(id, out var existing))
            {
                // Registry Hardening: Log collisions if it's a different object or has inconsistent data
                if (existing != type)
                {
                    OPLog.Warning("Registry", $"Collision detected for EnemyId '{id}': Multiple EnemyType objects found. Using first encounter.");
                }
                continue;
            }

            _registry.Add(id, type);
            OPLog.Debug("Registry", $"Registered Enemy: {id} (Source: {moonId} {poolName})");
        }
    }

    public EnemyType? GetEnemy(string id)
    {
        return _registry.TryGetValue(id, out var type) ? type : null;
    }
}
