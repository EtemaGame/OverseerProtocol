using System.Collections.Generic;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class SpawnOverrideApplier
{
    private readonly EnemyTypeRegistry _registry;

    public SpawnOverrideApplier(EnemyTypeRegistry registry)
    {
        _registry = registry;
    }

    public void Apply(SpawnOverrideCollection collection)
    {
        if (collection?.Overrides == null || collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No spawn overrides found to apply.");
            return;
        }

        if (StartOfRound.Instance == null || StartOfRound.Instance.levels == null)
        {
            OPLog.Warning("Overrides", "StartOfRound levels not available. Aborting spawn overrides.");
            return;
        }

        OPLog.Info("Overrides", $"Applying {collection.Overrides.Count} moon spawn overrides.");

        var appliedCount = 0;
        foreach (var moonOverride in collection.Overrides)
        {
            if (moonOverride == null) continue;
            if (string.IsNullOrWhiteSpace(moonOverride.MoonId)) continue;

            // Matching Logic: SelectableLevel.name against exported MoonDefinition.Id
            var level = System.Array.Find(StartOfRound.Instance.levels, l => l != null && l.name == moonOverride.MoonId);

            if (level == null)
            {
                OPLog.Warning("Overrides", $"Moon with ID '{moonOverride.MoonId}' not found in catalog. Skipping.");
                continue;
            }

            if (ApplyToLevel(level, moonOverride))
                appliedCount++;
        }

        OPLog.Info("Overrides", $"Successfully applied spawn overrides to {appliedCount} moons.");
    }

    private bool ApplyToLevel(SelectableLevel level, MoonSpawnOverride moonOverride)
    {
        var applied = false;

        // Behavior Contract: Property omitted/null -> keep vanilla. [] -> replace with empty.
        if (moonOverride.InsideEnemies != null)
        {
            OPLog.Debug("Overrides", $"Replacing Inside pool for {level.name} ({moonOverride.InsideEnemies.Count} entries)");
            level.Enemies = BuildSpawnList(moonOverride.InsideEnemies, "Inside", level.name);
            applied = true;
        }

        if (moonOverride.OutsideEnemies != null)
        {
            OPLog.Debug("Overrides", $"Replacing Outside pool for {level.name} ({moonOverride.OutsideEnemies.Count} entries)");
            level.OutsideEnemies = BuildSpawnList(moonOverride.OutsideEnemies, "Outside", level.name);
            applied = true;
        }

        if (moonOverride.DaytimeEnemies != null)
        {
            OPLog.Debug("Overrides", $"Replacing Daytime pool for {level.name} ({moonOverride.DaytimeEnemies.Count} entries)");
            level.DaytimeEnemies = BuildSpawnList(moonOverride.DaytimeEnemies, "Daytime", level.name);
            applied = true;
        }

        if (!applied)
            OPLog.Warning("Overrides", $"Moon override for '{level.name}' did not define any spawn pools. Nothing was changed.");

        return applied;
    }

    private List<SpawnableEnemyWithRarity> BuildSpawnList(List<SpawnEntry> entries, string poolName, string moonId)
    {
        var result = new List<SpawnableEnemyWithRarity>();
        
        foreach (var entry in entries)
        {
            if (entry == null) continue;
            if (string.IsNullOrWhiteSpace(entry.EnemyId)) continue;

            var type = _registry.GetEnemy(entry.EnemyId);
            if (type == null)
            {
                OPLog.Warning("Overrides", $"Enemy ID '{entry.EnemyId}' not found in registry. Skipping entry in {moonId} {poolName} pool.");
                continue;
            }

            result.Add(new SpawnableEnemyWithRarity(type, entry.Rarity));

        }

        return result;
    }
}
