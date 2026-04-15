using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OverseerProtocol.Core.Logging;

namespace OverseerProtocol.GameAbstractions.Resolvers;

public class MoonRoutePriceResolver
{
    private Dictionary<int, int> _levelIndexToPriceMap = new();

    public void Initialize()
    {
        _levelIndexToPriceMap.Clear();

        // Use FindObjectsOfTypeAll to catch all TerminalNodes regardless of where they are referenced
        var allNodes = Resources.FindObjectsOfTypeAll<TerminalNode>();
        if (allNodes == null || allNodes.Length == 0)
        {
            OPLog.Warning("Economy", "No TerminalNodes found in memory. Route prices will be missing.");
            return;
        }

        int foundCount = 0;
        foreach (var node in allNodes)
        {
            // In Lethal Company, nodes that trigger a moon reroute have buyRerouteToMoon set to the level index.
            if (node.buyRerouteToMoon != -1)
            {
                int levelIndex = node.buyRerouteToMoon;
                int cost = node.itemCost;

                // If multiple nodes point to the same moon, the one with a cost is likely the "route" one.
                // We keep the last one found or prioritize non-zero if needed, 
                // but usually there's only one "route [moon] [confirm]" flow.
                if (!_levelIndexToPriceMap.ContainsKey(levelIndex) || cost > 0)
                {
                    _levelIndexToPriceMap[levelIndex] = cost;
                    foundCount++;
                }
            }
        }

        OPLog.Info("Economy", $"Resolved {foundCount} moon route prices from TerminalNodes.");
    }

    public int GetRoutePrice(int levelIndex)
    {
        if (_levelIndexToPriceMap.TryGetValue(levelIndex, out int price))
        {
            return price;
        }

        // Return a sentinel value or 0 if not found, but we prefer preserving the "not found" state.
        // For V1, if not found, we'll return 0 but log it.
        return 0; 
    }
}
