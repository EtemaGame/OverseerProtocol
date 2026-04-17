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
            OPLog.Info("Overrides", "No spawn tuning entries found to apply.");
            return;
        }

        if (StartOfRound.Instance == null || StartOfRound.Instance.levels == null)
        {
            OPLog.Warning("Overrides", "StartOfRound levels not available. Aborting spawn tuning.");
            return;
        }

        OPLog.Info("Overrides", $"Applying {collection.Overrides.Count} moon spawn tuning entries.");

        var appliedCount = 0;
        var skippedCount = 0;
        foreach (var moonOverride in collection.Overrides)
        {
            if (moonOverride == null)
            {
                OPLog.Warning("Overrides", "Null moon spawn tuning entry was skipped.");
                skippedCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(moonOverride.MoonId))
            {
                OPLog.Warning("Overrides", "Moon spawn tuning entry with empty moonId was skipped.");
                skippedCount++;
                continue;
            }

            // Matching Logic: SelectableLevel.name against exported MoonDefinition.Id
            var level = System.Array.Find(StartOfRound.Instance.levels, l => l != null && l.name == moonOverride.MoonId);

            if (level == null)
            {
                OPLog.Warning("Overrides", $"Moon with ID '{moonOverride.MoonId}' not found in catalog. Skipping.");
                skippedCount++;
                continue;
            }

            if (ApplyToLevel(level, moonOverride))
                appliedCount++;
            else
                skippedCount++;
        }

        OPLog.Info("Overrides", $"Successfully applied spawn tuning to {appliedCount} moons. skipped={skippedCount}");
    }

    private bool ApplyToLevel(SelectableLevel level, MoonSpawnOverride moonOverride)
    {
        var applied = false;

        // Behavior Contract: Property omitted/null -> keep vanilla. [] -> replace with empty.
        if (moonOverride.InsideEnemies != null)
        {
            OPLog.Info("Overrides", $"Replacing Inside pool for {level.name}: oldCount={level.Enemies?.Count ?? 0}, newRequestedCount={moonOverride.InsideEnemies.Count}");
            level.Enemies = BuildSpawnList(moonOverride.InsideEnemies, "Inside", level.name);
            OPLog.Info("Overrides", $"Inside pool for {level.name} now has {level.Enemies.Count} entries.");
            applied = true;
        }
        else
        {
            OPLog.Info("Overrides", $"Inside pool omitted for {level.name}. Keeping vanilla/current pool.");
        }

        if (moonOverride.OutsideEnemies != null)
        {
            OPLog.Info("Overrides", $"Replacing Outside pool for {level.name}: oldCount={level.OutsideEnemies?.Count ?? 0}, newRequestedCount={moonOverride.OutsideEnemies.Count}");
            level.OutsideEnemies = BuildSpawnList(moonOverride.OutsideEnemies, "Outside", level.name);
            OPLog.Info("Overrides", $"Outside pool for {level.name} now has {level.OutsideEnemies.Count} entries.");
            applied = true;
        }
        else
        {
            OPLog.Info("Overrides", $"Outside pool omitted for {level.name}. Keeping vanilla/current pool.");
        }

        if (moonOverride.DaytimeEnemies != null)
        {
            OPLog.Info("Overrides", $"Replacing Daytime pool for {level.name}: oldCount={level.DaytimeEnemies?.Count ?? 0}, newRequestedCount={moonOverride.DaytimeEnemies.Count}");
            level.DaytimeEnemies = BuildSpawnList(moonOverride.DaytimeEnemies, "Daytime", level.name);
            OPLog.Info("Overrides", $"Daytime pool for {level.name} now has {level.DaytimeEnemies.Count} entries.");
            applied = true;
        }
        else
        {
            OPLog.Info("Overrides", $"Daytime pool omitted for {level.name}. Keeping vanilla/current pool.");
        }

        if (!applied)
            OPLog.Warning("Overrides", $"Moon spawn tuning for '{level.name}' did not define any spawn pools. Nothing was changed.");

        return applied;
    }

    private List<SpawnableEnemyWithRarity> BuildSpawnList(List<SpawnEntry> entries, string poolName, string moonId)
    {
        var result = new List<SpawnableEnemyWithRarity>();
        
        foreach (var entry in entries)
        {
            if (entry == null)
            {
                OPLog.Warning("Overrides", $"Null spawn entry skipped in {moonId} {poolName} pool.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.EnemyId))
            {
                OPLog.Warning("Overrides", $"Spawn entry with empty enemyId skipped in {moonId} {poolName} pool.");
                continue;
            }

            var type = _registry.GetEnemy(entry.EnemyId);
            if (type == null)
            {
                OPLog.Warning("Overrides", $"Enemy ID '{entry.EnemyId}' not found in registry. Skipping entry in {moonId} {poolName} pool.");
                continue;
            }

            result.Add(new SpawnableEnemyWithRarity(type, entry.Rarity));
            OPLog.Info("Overrides", $"Added spawn entry: moon={moonId}, pool={poolName}, enemy={entry.EnemyId}, rarity={entry.Rarity}");
        }

        return result;
    }
}
