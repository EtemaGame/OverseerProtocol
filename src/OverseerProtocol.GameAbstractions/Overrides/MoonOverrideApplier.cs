using System;
using System.Collections.Generic;
using System.Linq;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class MoonOverrideApplier
{
    public void Apply(MoonOverrideCollection collection)
    {
        if (collection?.Overrides == null || collection.Overrides.Count == 0)
        {
            OPLog.Info("Overrides", "No moon tuning entries found to apply.");
            return;
        }

        if (StartOfRound.Instance == null || StartOfRound.Instance.levels == null)
        {
            OPLog.Warning("Overrides", "StartOfRound levels not available. Aborting moon tuning.");
            return;
        }

        var routeNodes = Resources.FindObjectsOfTypeAll<TerminalNode>() ?? Array.Empty<TerminalNode>();
        var appliedMoonCount = 0;
        var routePriceMutationCount = 0;
        var skippedMoonCount = 0;

        OPLog.Info("Overrides", $"Applying {collection.Overrides.Count} moon tuning entries.");

        foreach (var moonOverride in collection.Overrides)
        {
            if (moonOverride == null || string.IsNullOrWhiteSpace(moonOverride.MoonId))
            {
                OPLog.Warning("Overrides", "Null moon tuning entry or empty moonId was skipped.");
                skippedMoonCount++;
                continue;
            }

            var levelIndex = FindLevelIndex(moonOverride.MoonId);
            if (levelIndex < 0)
            {
                OPLog.Warning("Overrides", $"Moon with ID '{moonOverride.MoonId}' not found in runtime catalog. Skipping.");
                skippedMoonCount++;
                continue;
            }

            var level = StartOfRound.Instance.levels[levelIndex];
            var applied = false;

            if (!string.IsNullOrWhiteSpace(moonOverride.RiskLabel))
            {
                OPLog.Info("Overrides", $"Overriding {level.name}.riskLevel via riskLabel: {level.riskLevel} -> {moonOverride.RiskLabel}");
                level.riskLevel = moonOverride.RiskLabel;
                applied = true;
            }
            else if (moonOverride.RiskLevel.HasValue)
            {
                var riskLabel = FormatRiskLevel(moonOverride.RiskLevel.Value);
                OPLog.Info("Overrides", $"Overriding {level.name}.riskLevel via riskLevel={moonOverride.RiskLevel.Value}: {level.riskLevel} -> {riskLabel}");
                level.riskLevel = riskLabel;
                applied = true;
            }
            else
            {
                OPLog.Info("Overrides", $"Moon tuning for '{level.name}' has no riskLevel/riskLabel change.");
            }

            if (moonOverride.RoutePrice.HasValue)
            {
                var updatedNodes = ApplyRoutePrice(routeNodes, levelIndex, moonOverride.RoutePrice.Value);
                routePriceMutationCount += updatedNodes;
                applied = applied || updatedNodes > 0;

                if (updatedNodes == 0)
                    OPLog.Warning("Overrides", $"No TerminalNode route price entries found for moon '{level.name}' at level index {levelIndex}.");
            }
            else
            {
                OPLog.Info("Overrides", $"Moon tuning for '{level.name}' has no routePrice change.");
            }

            if (applied)
                appliedMoonCount++;
            else
            {
                OPLog.Warning("Overrides", $"Moon tuning for '{level.name}' did not mutate any runtime fields.");
                skippedMoonCount++;
            }
        }

        OPLog.Info("Overrides", $"Successfully applied moon tuning to {appliedMoonCount} moons and {routePriceMutationCount} route nodes. skipped={skippedMoonCount}");
    }

    private static int FindLevelIndex(string moonId)
    {
        var levels = StartOfRound.Instance?.levels;
        if (levels == null)
            return -1;

        for (var i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null && levels[i].name == moonId)
                return i;
        }

        return -1;
    }

    private static int ApplyRoutePrice(IEnumerable<TerminalNode> routeNodes, int levelIndex, int routePrice)
    {
        var appliedCount = 0;

        foreach (var node in routeNodes.Where(node => node != null && node.buyRerouteToMoon == levelIndex))
        {
            OPLog.Info("Overrides", $"Overriding route node {node.name}.itemCost: {node.itemCost} -> {routePrice}");
            node.itemCost = routePrice;
            appliedCount++;
        }

        return appliedCount;
    }

    private static string FormatRiskLevel(int riskLevel)
    {
        switch (riskLevel)
        {
            case 1:
                return "D";
            case 2:
                return "C";
            case 3:
                return "B";
            case 4:
                return "A";
            case 5:
                return "S";
            default:
                return "None";
        }
    }
}
