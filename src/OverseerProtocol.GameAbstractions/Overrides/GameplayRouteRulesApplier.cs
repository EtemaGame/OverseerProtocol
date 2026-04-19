using System;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Rules;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class GameplayRouteRulesApplier
{
    private const float NoOpMultiplier = 1f;
    private const float Epsilon = 0.0001f;

    public void Apply(GameplayRouteRulesDefinition rules)
    {
        if (rules == null)
        {
            OPLog.Info("Gameplay", "No gameplay route rules were provided.");
            return;
        }

        OPLog.Info("Gameplay", "Route multiplier application started.");
        ApplyEconomyRules(rules);
        OPLog.Info("Gameplay", "Route multiplier application completed.");
    }

    private static void ApplyEconomyRules(GameplayRouteRulesDefinition rules)
    {
        var routeNodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        if (routeNodes == null || routeNodes.Length == 0)
        {
            OPLog.Warning("Gameplay", "No TerminalNode route entries available. Route multipliers were not applied.");
            return;
        }

        var travelDiscountMultiplier = NormalizeMultiplier(rules.Economy?.TravelDiscountMultiplier ?? NoOpMultiplier);
        OPLog.Info("Gameplay", $"Active economy rule travelDiscountMultiplier={travelDiscountMultiplier:0.###}");
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
                OPLog.Warning("Gameplay", $"Route node {node.name} points to invalid moon index {node.buyRerouteToMoon}. Skipping per-moon route multiplier lookup.");
                moonMultiplier = NoOpMultiplier;
            }

            multiplier *= moonMultiplier;

            if (IsNoOp(multiplier))
            {
                skippedNoOpCount++;
                continue;
            }

            var scaled = (int)Math.Round(node.itemCost * multiplier, MidpointRounding.AwayFromZero);
            node.itemCost = Math.Max(0, scaled);
            appliedCount++;
            OPLog.Info(
                "Gameplay",
                $"Route node mutated by gameplay multipliers: node={node.name}, moonIndex={node.buyRerouteToMoon}, cost {originalCost} -> {node.itemCost}, travelMultiplier={travelDiscountMultiplier:0.###}, moonMultiplier={moonMultiplier:0.###}, effectiveMultiplier={multiplier:0.###}");
        }

        if (appliedCount > 0)
            OPLog.Info("Gameplay", $"Applied route multipliers to {appliedCount} terminal route nodes. noOp={skippedNoOpCount}, invalidLevel={skippedInvalidLevelCount}");
        else
            OPLog.Info("Gameplay", $"Route multipliers were all no-op. noOp={skippedNoOpCount}, invalidLevel={skippedInvalidLevelCount}");
    }

    private static float GetMoonRoutePriceMultiplier(GameplayRouteRulesDefinition rules, int levelIndex)
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
            return NoOpMultiplier;
        }

        OPLog.Info("Gameplay", $"Per-moon route rule found: moon={level.name}, routePriceMultiplier={moonRule.RoutePriceMultiplier:0.###}");
        return NormalizeMultiplier(moonRule.RoutePriceMultiplier);
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
