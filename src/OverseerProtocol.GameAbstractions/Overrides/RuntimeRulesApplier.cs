using System;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Rules;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class RuntimeRulesApplier
{
    private const float NoOpMultiplier = 1f;
    private const float Epsilon = 0.0001f;

    public void Apply(RuntimeRulesDefinition rules)
    {
        if (rules == null)
        {
            OPLog.Info("RuntimeRules", "No runtime rules were provided.");
            return;
        }

        OPLog.Info("RuntimeRules", "Runtime rules application started.");
        ApplyEconomyRules(rules);
        LogReservedRules(rules);
        OPLog.Info("RuntimeRules", "Runtime rules application completed.");
    }

    private static void ApplyEconomyRules(RuntimeRulesDefinition rules)
    {
        var routeNodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        if (routeNodes == null || routeNodes.Length == 0)
        {
            OPLog.Warning("RuntimeRules", "No TerminalNode route entries available. Route runtime rules were not applied.");
            return;
        }

        var travelDiscountMultiplier = NormalizeMultiplier(rules.Economy?.TravelDiscountMultiplier ?? NoOpMultiplier);
        OPLog.Info("RuntimeRules", $"Active economy rule travelDiscountMultiplier={travelDiscountMultiplier:0.###}");
        var appliedCount = 0;
        var skippedNoOpCount = 0;
        var skippedInvalidLevelCount = 0;

        foreach (var node in routeNodes)
        {
            if (node == null || node.buyRerouteToMoon < 0)
                continue;

            var originalCost = node.itemCost;
            var multiplier = travelDiscountMultiplier;
            var moonMultiplier = GetMoonRoutePriceMultiplier(rules, node.buyRerouteToMoon);
            if (moonMultiplier < 0f)
            {
                skippedInvalidLevelCount++;
                OPLog.Warning("RuntimeRules", $"Route node {node.name} points to invalid moon index {node.buyRerouteToMoon}. Skipping per-moon runtime rule lookup.");
                moonMultiplier = NoOpMultiplier;
            }

            multiplier *= moonMultiplier;

            if (IsNoOp(multiplier))
            {
                skippedNoOpCount++;
                OPLog.Info("RuntimeRules", $"Route node no-op: node={node.name}, moonIndex={node.buyRerouteToMoon}, cost={originalCost}, multiplier={multiplier:0.###}");
                continue;
            }

            var scaled = (int)Math.Round(node.itemCost * multiplier, MidpointRounding.AwayFromZero);
            node.itemCost = Math.Max(0, scaled);
            appliedCount++;
            OPLog.Info(
                "RuntimeRules",
                $"Route node mutated by runtime rules: node={node.name}, moonIndex={node.buyRerouteToMoon}, cost {originalCost} -> {node.itemCost}, travelMultiplier={travelDiscountMultiplier:0.###}, moonMultiplier={moonMultiplier:0.###}, effectiveMultiplier={multiplier:0.###}");
        }

        if (appliedCount > 0)
            OPLog.Info("RuntimeRules", $"Applied runtime route/economy rules to {appliedCount} terminal route nodes. noOp={skippedNoOpCount}, invalidLevel={skippedInvalidLevelCount}");
        else
            OPLog.Info("RuntimeRules", $"Runtime route/economy rules were all no-op. noOp={skippedNoOpCount}, invalidLevel={skippedInvalidLevelCount}");
    }

    private static float GetMoonRoutePriceMultiplier(RuntimeRulesDefinition rules, int levelIndex)
    {
        if (StartOfRound.Instance?.levels == null ||
            levelIndex < 0 ||
            levelIndex >= StartOfRound.Instance.levels.Length)
        {
            return -1f;
        }

        var level = StartOfRound.Instance.levels[levelIndex];
        if (level == null || string.IsNullOrWhiteSpace(level.name))
            return NoOpMultiplier;

        if (rules.MoonRules == null ||
            !rules.MoonRules.TryGetValue(level.name, out var moonRule) ||
            moonRule == null)
        {
            OPLog.Info("RuntimeRules", $"No per-moon route rule for moon={level.name}. Using multiplier 1.");
            return NoOpMultiplier;
        }

        OPLog.Info("RuntimeRules", $"Per-moon route rule found: moon={level.name}, routePriceMultiplier={moonRule.RoutePriceMultiplier:0.###}");
        return NormalizeMultiplier(moonRule.RoutePriceMultiplier);
    }

    private static void LogReservedRules(RuntimeRulesDefinition rules)
    {
        if (!IsNoOp(rules.Economy?.QuotaMultiplier ?? NoOpMultiplier))
            OPLog.Warning("RuntimeRules", "quotaMultiplier is reserved until quota runtime hooks are verified.");

        if (!IsNoOp(rules.Economy?.DeadlineMultiplier ?? NoOpMultiplier))
            OPLog.Warning("RuntimeRules", "deadlineMultiplier is reserved until deadline runtime hooks are verified.");

        if (!IsNoOp(rules.Economy?.ScrapValueMultiplier ?? NoOpMultiplier))
            OPLog.Warning("RuntimeRules", "scrapValueMultiplier is reserved until scrap value runtime hooks are verified.");

        if (rules.Economy?.PreserveShipLootOnTeamWipe == true)
            OPLog.Warning("RuntimeRules", "preserveShipLootOnTeamWipe is reserved until team wipe/ship loot hooks are verified.");

        LogReservedMultiplier(rules.Ship?.LandingSpeedMultiplier ?? NoOpMultiplier, "ship.landingSpeedMultiplier", "landing/dropship timing hooks");
        LogReservedMultiplier(rules.Ship?.DropshipSpeedMultiplier ?? NoOpMultiplier, "ship.dropshipSpeedMultiplier", "dropship timing hooks");
        LogReservedMultiplier(rules.Ship?.ScannerDistanceMultiplier ?? NoOpMultiplier, "ship.scannerDistanceMultiplier", "scanner hooks");
        LogReservedMultiplier(rules.Ship?.BatteryCapacityMultiplier ?? NoOpMultiplier, "ship.batteryCapacityMultiplier", "battery hooks");

        LogReservedMultiplier(rules.Weather?.ClearRewardMultiplier ?? NoOpMultiplier, "weather.clearRewardMultiplier", "weather reward hooks");
        LogReservedMultiplier(rules.Weather?.RainyRewardMultiplier ?? NoOpMultiplier, "weather.rainyRewardMultiplier", "weather reward hooks");
        LogReservedMultiplier(rules.Weather?.StormyRewardMultiplier ?? NoOpMultiplier, "weather.stormyRewardMultiplier", "weather reward hooks");
        LogReservedMultiplier(rules.Weather?.FoggyRewardMultiplier ?? NoOpMultiplier, "weather.foggyRewardMultiplier", "weather reward hooks");
        LogReservedMultiplier(rules.Weather?.FloodedRewardMultiplier ?? NoOpMultiplier, "weather.floodedRewardMultiplier", "weather reward hooks");
        LogReservedMultiplier(rules.Weather?.EclipsedRewardMultiplier ?? NoOpMultiplier, "weather.eclipsedRewardMultiplier", "weather reward hooks");

        if (rules.MoonRules == null)
            return;

        foreach (var pair in rules.MoonRules)
        {
            if (pair.Value == null)
                continue;

            LogReservedMultiplier(pair.Value.ScrapValueMultiplier, $"moonRules.{pair.Key}.scrapValueMultiplier", "scrap value hooks");
            LogReservedMultiplier(pair.Value.SpawnRarityMultiplier, $"moonRules.{pair.Key}.spawnRarityMultiplier", "per-moon spawn runtime hooks");
            LogReservedMultiplier(pair.Value.WeatherRewardMultiplier, $"moonRules.{pair.Key}.weatherRewardMultiplier", "weather reward hooks");
        }
    }

    private static void LogReservedMultiplier(float value, string path, string missingHook)
    {
        if (IsNoOp(value))
            return;

        OPLog.Warning("RuntimeRules", $"{path}={value:0.###} is reserved until {missingHook} are verified.");
    }

    private static bool IsNoOp(float value) =>
        Math.Abs(value - NoOpMultiplier) < Epsilon;

    private static float NormalizeMultiplier(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return NoOpMultiplier;

        if (value < 0f)
            return 0f;

        if (value > 10f)
            return 10f;

        return value;
    }
}
