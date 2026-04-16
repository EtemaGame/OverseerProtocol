using System.Collections.Generic;
using System.Linq;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Economy;
using OverseerProtocol.Data.Models.Moons;
using UnityEngine;

namespace OverseerProtocol.GameAbstractions.Resolvers;

public class MoonRoutePriceResolver
{
    private Dictionary<int, int> _levelIndexToPriceMap = new();
    private Dictionary<int, List<RoutePriceNodeDefinition>> _levelIndexToRouteNodes = new();

    public void Initialize()
    {
        _levelIndexToPriceMap.Clear();
        _levelIndexToRouteNodes.Clear();

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
                foundCount++;

                if (!_levelIndexToRouteNodes.TryGetValue(levelIndex, out var nodes))
                {
                    nodes = new List<RoutePriceNodeDefinition>();
                    _levelIndexToRouteNodes[levelIndex] = nodes;
                }

                nodes.Add(new RoutePriceNodeDefinition
                {
                    TerminalNodeId = node.name,
                    BuyRerouteToMoon = levelIndex,
                    ItemCost = cost
                });

                // If multiple nodes point to the same moon, the one with a cost is likely the "route" one.
                // We keep the last one found or prioritize non-zero if needed, 
                // but usually there's only one "route [moon] [confirm]" flow.
                if (!_levelIndexToPriceMap.ContainsKey(levelIndex) || cost > 0)
                {
                    _levelIndexToPriceMap[levelIndex] = cost;
                }
            }
        }

        OPLog.Info("Economy", $"Resolved {_levelIndexToPriceMap.Count} moon route prices from {foundCount} TerminalNode route candidates.");
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

    public List<RoutePriceNodeDefinition> GetRouteNodes(int levelIndex)
    {
        if (!_levelIndexToRouteNodes.TryGetValue(levelIndex, out var nodes))
            return new List<RoutePriceNodeDefinition>();

        return nodes
            .Select(node => new RoutePriceNodeDefinition
            {
                TerminalNodeId = node.TerminalNodeId,
                BuyRerouteToMoon = node.BuyRerouteToMoon,
                ItemCost = node.ItemCost
            })
            .ToList();
    }

    public List<MoonEconomyProfile> BuildMoonEconomyProfiles(List<MoonDefinition> moons)
    {
        var profiles = new List<MoonEconomyProfile>();
        if (moons == null || moons.Count == 0)
            return profiles;

        for (var i = 0; i < moons.Count; i++)
        {
            var moon = moons[i];
            if (moon == null) continue;
            var levelIndex = moon.LevelIndex;

            var routeNodes = GetRouteNodes(levelIndex);
            profiles.Add(new MoonEconomyProfile
            {
                MoonId = moon.Id,
                InternalName = moon.InternalName,
                LevelIndex = levelIndex,
                RoutePrice = GetRoutePrice(levelIndex),
                HasRouteNode = routeNodes.Count > 0,
                RouteNodes = routeNodes
            });
        }

        return profiles;
    }
}
