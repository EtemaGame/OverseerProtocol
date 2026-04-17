using System;
using System.Collections.Generic;
using OverseerProtocol.Core.Validation;
using OverseerProtocol.Data.Models;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class SpawnOverrideValidator
{
    public const int MinRarity = 0;
    public const int MaxRarity = 1000;

    private readonly EnemyTypeRegistry _registry;

    public SpawnOverrideValidator(EnemyTypeRegistry registry)
    {
        _registry = registry;
    }

    public SpawnOverrideValidationResult Validate(SpawnOverrideCollection collection, SpawnOverrideReferenceCatalog references)
    {
        var report = new ValidationReport();
        var validatedCollection = new SpawnOverrideCollection();

        if (!references.HasMoonCatalog)
            report.Error("SPAWN_MOON_CATALOG_MISSING", "Spawn overrides require the runtime moon catalog.");

        if (!references.HasEnemyCatalog)
            report.Error("SPAWN_ENEMY_CATALOG_MISSING", "Spawn overrides require the runtime enemy registry.");

        if (report.HasErrors)
            return new SpawnOverrideValidationResult(validatedCollection, report);

        if (collection?.Overrides == null || collection.Overrides.Count == 0)
        {
            report.Info("SPAWN_OVERRIDES_EMPTY", "No spawn override entries were found.");
            return new SpawnOverrideValidationResult(validatedCollection, report);
        }

        var observedMoons = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < collection.Overrides.Count; i++)
        {
            var moonOverride = collection.Overrides[i];
            var moonPath = $"overrides[{i}]";

            if (moonOverride == null)
            {
                report.Warning("SPAWN_OVERRIDE_NULL", "Found null moon override entry. Skipping entry.", moonPath);
                continue;
            }

            if (string.IsNullOrWhiteSpace(moonOverride.MoonId))
            {
                report.Warning("SPAWN_MOON_ID_EMPTY", "Found moon override entry with empty MoonId. Skipping entry.", $"{moonPath}.moonId");
                continue;
            }

            if (!references.MoonIds.Contains(moonOverride.MoonId))
            {
                report.Warning("SPAWN_MOON_ID_UNKNOWN", $"MoonId '{moonOverride.MoonId}' does not exist in the runtime moon catalog. Skipping moon override.", $"{moonPath}.moonId");
                continue;
            }

            if (!observedMoons.Add(moonOverride.MoonId))
            {
                report.Warning("SPAWN_MOON_ID_DUPLICATE", $"Duplicate MoonId '{moonOverride.MoonId}' found. First entry wins; duplicate skipped.", $"{moonPath}.moonId");
                continue;
            }

            validatedCollection.Overrides.Add(new MoonSpawnOverride
            {
                MoonId = moonOverride.MoonId,
                InsideEnemies = ValidatePool(moonOverride.MoonId, "insideEnemies", moonOverride.InsideEnemies, references, report, moonPath),
                OutsideEnemies = ValidatePool(moonOverride.MoonId, "outsideEnemies", moonOverride.OutsideEnemies, references, report, moonPath),
                DaytimeEnemies = ValidatePool(moonOverride.MoonId, "daytimeEnemies", moonOverride.DaytimeEnemies, references, report, moonPath)
            });
        }

        return new SpawnOverrideValidationResult(validatedCollection, report);
    }

    private List<SpawnEntry>? ValidatePool(
        string moonId,
        string poolName,
        List<SpawnEntry>? entries,
        SpawnOverrideReferenceCatalog references,
        ValidationReport report,
        string moonPath)
    {
        if (entries == null)
            return null;

        var validatedEntries = new List<SpawnEntry>();
        var observedEnemies = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var entryPath = $"{moonPath}.{poolName}[{i}]";

            if (entry == null)
            {
                report.Warning("SPAWN_ENTRY_NULL", $"[{moonId}] {poolName} contains a null entry. Skipping entry.", entryPath);
                continue;
            }

            if (string.IsNullOrWhiteSpace(entry.EnemyId))
            {
                report.Warning("SPAWN_ENEMY_ID_EMPTY", $"[{moonId}] {poolName} contains an entry with empty EnemyId. Skipping entry.", $"{entryPath}.enemyId");
                continue;
            }

            if (!observedEnemies.Add(entry.EnemyId))
            {
                report.Warning("SPAWN_ENEMY_ID_DUPLICATE", $"[{moonId}] {poolName} contains duplicate EnemyId '{entry.EnemyId}'. Duplicate retained because spawn weighting may be intentional.", $"{entryPath}.enemyId");
            }

            if (!references.EnemyIds.Contains(entry.EnemyId))
            {
                report.Warning("SPAWN_ENEMY_ID_UNKNOWN", $"[{moonId}] {poolName} enemy '{entry.EnemyId}' does not exist in the runtime enemy registry. Skipping entry.", $"{entryPath}.enemyId");
                continue;
            }

            if (_registry.GetEnemy(entry.EnemyId) == null)
            {
                report.Warning("SPAWN_ENEMY_ID_UNRESOLVED", $"[{moonId}] {poolName} enemy '{entry.EnemyId}' does not exist in the scanned runtime registry. Skipping entry.", $"{entryPath}.enemyId");
                continue;
            }

            var rarity = ClampRarity(entry.Rarity, report, $"{entryPath}.rarity", moonId, poolName, entry.EnemyId);
            validatedEntries.Add(new SpawnEntry
            {
                EnemyId = entry.EnemyId,
                Rarity = rarity
            });
        }

        return validatedEntries;
    }

    private int ClampRarity(int rarity, ValidationReport report, string path, string moonId, string poolName, string enemyId)
    {
        if (rarity < MinRarity)
        {
            report.Warning("SPAWN_RARITY_CLAMPED", $"[{moonId}] {poolName} enemy '{enemyId}' rarity {rarity} is below {MinRarity}. Clamped to {MinRarity}.", path);
            return MinRarity;
        }

        if (rarity > MaxRarity)
        {
            report.Warning("SPAWN_RARITY_CLAMPED", $"[{moonId}] {poolName} enemy '{enemyId}' rarity {rarity} is above {MaxRarity}. Clamped to {MaxRarity}.", path);
            return MaxRarity;
        }

        return rarity;
    }
}
