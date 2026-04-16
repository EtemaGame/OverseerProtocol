# Runtime Rules V1

Runtime rules are a data contract for economy, ship, weather, and moon-specific balancing.

Default path:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/rules/runtime-rules.json
```

Preset path:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-id>/rules/runtime-rules.json
```

Config gate:

```ini
[RuntimeRules]
EnableRuntimeRulesLoading = true
```

## Contract

```json
{
  "schemaVersion": 1,
  "economy": {
    "quotaMultiplier": 1,
    "deadlineMultiplier": 1,
    "travelDiscountMultiplier": 1,
    "scrapValueMultiplier": 1,
    "preserveShipLootOnTeamWipe": false
  },
  "ship": {
    "landingSpeedMultiplier": 1,
    "dropshipSpeedMultiplier": 1,
    "scannerDistanceMultiplier": 1,
    "batteryCapacityMultiplier": 1
  },
  "weather": {
    "clearRewardMultiplier": 1,
    "rainyRewardMultiplier": 1,
    "stormyRewardMultiplier": 1,
    "foggyRewardMultiplier": 1,
    "floodedRewardMultiplier": 1,
    "eclipsedRewardMultiplier": 1
  },
  "moonRules": {
    "ExperimentationLevel": {
      "routePriceMultiplier": 1,
      "scrapValueMultiplier": 1,
      "spawnRarityMultiplier": 1,
      "weatherRewardMultiplier": 1
    }
  }
}
```

## Current Status

V1 creates, loads, and normalizes this file. Most fields are not yet runtime-applied.

Already active elsewhere:

- Global route prices can be changed with `RoutePriceMultiplier`.
- Per-moon route prices can be changed with `moons.override.json`.

Future runtime hooks should consume this contract for quota, deadline, loot retention, ship timings, weather reward tuning, and moon-specific rule composition.
