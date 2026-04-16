# Moon Economy V2

Moon economy export maps Lethal Company's terminal reroute data back to `MoonDefinition`.

## Source Mapping

The route price resolver scans all loaded `TerminalNode` objects with:

```text
Resources.FindObjectsOfTypeAll<TerminalNode>()
```

A node is treated as a moon route candidate when:

```text
TerminalNode.buyRerouteToMoon != -1
```

Mapping contract:

- `TerminalNode.buyRerouteToMoon`: level index in `StartOfRound.Instance.levels`.
- `TerminalNode.itemCost`: raw terminal route price.
- `TerminalNode.name`: exported as `terminalNodeId`.
- `MoonDefinition.id`: `SelectableLevel.name` at the same level index.
- `MoonDefinition.levelIndex`: Explicit exported index for the level in `StartOfRound.Instance.levels`.

If multiple route nodes target the same level index, OverseerProtocol keeps all raw candidates in `routeNodes` and resolves `routePrice` by preferring a non-zero `itemCost`.

## Export

Economy data is exported to:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/exports/economy/moon-economy.json
```

Each entry contains:

- `moonId`: Stable moon ID from `MoonDefinition`.
- `internalName`: Raw moon name from the level.
- `levelIndex`: Index used by terminal reroute nodes.
- `routePrice`: Resolved route price.
- `hasRouteNode`: Whether any terminal route node was found.
- `routeNodes`: Raw terminal node candidates with `terminalNodeId`, `buyRerouteToMoon`, and `itemCost`.
