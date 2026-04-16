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

        ApplyEconomyRules(rules);
        LogReservedRules(rules);
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
        var appliedCount = 0;

        foreach (var node in routeNodes)
        {
            if (node == null || node.buyRerouteToMoon == -1)
                continue;

            var multiplier = travelDiscountMultiplier;
            multiplier *= GetMoonRoutePriceMultiplier(rules, node.buyRerouteToMoon);

            if (IsNoOp(multiplier))
                continue;

            var scaled = (int)Math.Round(node.itemCost * multiplier, MidpointRounding.AwayFromZero);
            node.itemCost = Math.Max(0, scaled);
            appliedCount++;
        }

        if (appliedCount > 0)
            OPLog.Info("RuntimeRules", $"Applied runtime route/economy rules to {appliedCount} terminal route nodes.");
        else
            OPLog.Info("RuntimeRules", "Runtime route/economy rules were all no-op.");
    }

    private static float GetMoonRoutePriceMultiplier(RuntimeRulesDefinition rules, int levelIndex)
    {
        if (StartOfRound.Instance?.levels == null ||
            levelIndex < 0 ||
            levelIndex >= StartOfRound.Instance.levels.Length)
        {
            return NoOpMultiplier;
        }

        var level = StartOfRound.Instance.levels[levelIndex];
        if (level == null || string.IsNullOrWhiteSpace(level.name))
            return NoOpMultiplier;

        if (rules.MoonRules == null ||
            !rules.MoonRules.TryGetValue(level.name, out var moonRule) ||
            moonRule == null)
        {
            return NoOpMultiplier;
        }

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
