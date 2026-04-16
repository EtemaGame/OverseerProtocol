using System;
using System.Collections.Generic;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class RuntimeMultiplierApplier
{
    private const float NoOpMultiplier = 1f;
    private const float MultiplierEpsilon = 0.0001f;

    public void Apply(float itemWeightMultiplier, float spawnRarityMultiplier)
    {
        var normalizedItemWeightMultiplier = NormalizeMultiplier(itemWeightMultiplier, "ItemWeightMultiplier");
        var normalizedSpawnRarityMultiplier = NormalizeMultiplier(spawnRarityMultiplier, "SpawnRarityMultiplier");

        if (IsNoOp(normalizedItemWeightMultiplier) && IsNoOp(normalizedSpawnRarityMultiplier))
        {
            OPLog.Info("Overrides", "Runtime multipliers are all 1. No multiplier changes applied.");
            return;
        }

        if (!IsNoOp(normalizedItemWeightMultiplier))
            ApplyItemWeightMultiplier(normalizedItemWeightMultiplier);

        if (!IsNoOp(normalizedSpawnRarityMultiplier))
            ApplySpawnRarityMultiplier(normalizedSpawnRarityMultiplier);
    }

    private void ApplyItemWeightMultiplier(float multiplier)
    {
        if (StartOfRound.Instance == null ||
            StartOfRound.Instance.allItemsList == null ||
            StartOfRound.Instance.allItemsList.itemsList == null)
        {
            OPLog.Warning("Overrides", "Item catalog not available. Skipping item weight multiplier.");
            return;
        }

        var appliedCount = 0;
        foreach (var item in StartOfRound.Instance.allItemsList.itemsList)
        {
            if (item == null) continue;

            item.weight *= multiplier;
            appliedCount++;
        }

        OPLog.Info("Overrides", $"Applied item weight multiplier {multiplier:0.###} to {appliedCount} items.");
    }

    private void ApplySpawnRarityMultiplier(float multiplier)
    {
        if (StartOfRound.Instance == null || StartOfRound.Instance.levels == null)
        {
            OPLog.Warning("Overrides", "StartOfRound levels not available. Skipping spawn rarity multiplier.");
            return;
        }

        var appliedCount = 0;
        foreach (var level in StartOfRound.Instance.levels)
        {
            if (level == null) continue;

            appliedCount += ApplySpawnPool(level.Enemies, multiplier);
            appliedCount += ApplySpawnPool(level.OutsideEnemies, multiplier);
            appliedCount += ApplySpawnPool(level.DaytimeEnemies, multiplier);
        }

        OPLog.Info("Overrides", $"Applied spawn rarity multiplier {multiplier:0.###} to {appliedCount} spawn entries.");
    }

    private int ApplySpawnPool(List<SpawnableEnemyWithRarity> pool, float multiplier)
    {
        if (pool == null) return 0;

        var appliedCount = 0;
        foreach (var entry in pool)
        {
            if (entry == null) continue;

            var scaled = (int)Math.Round(entry.rarity * multiplier, MidpointRounding.AwayFromZero);
            entry.rarity = ClampRarity(scaled);
            appliedCount++;
        }

        return appliedCount;
    }

    private static int ClampRarity(int value)
    {
        if (value < SpawnOverrideValidator.MinRarity)
            return SpawnOverrideValidator.MinRarity;

        if (value > SpawnOverrideValidator.MaxRarity)
            return SpawnOverrideValidator.MaxRarity;

        return value;
    }

    private static bool IsNoOp(float multiplier) =>
        Math.Abs(multiplier - NoOpMultiplier) < MultiplierEpsilon;

    private static float NormalizeMultiplier(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            OPLog.Warning("Config", $"{name} is not a finite value. Falling back to 1.");
            return NoOpMultiplier;
        }

        if (value < 0f)
        {
            OPLog.Warning("Config", $"{name} is below 0. Clamped to 0.");
            return 0f;
        }

        if (value > 10f)
        {
            OPLog.Warning("Config", $"{name} is above 10. Clamped to 10.");
            return 10f;
        }

        return value;
    }
}
