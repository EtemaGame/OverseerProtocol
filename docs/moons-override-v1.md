# moons.override.json V1

`moons.override.json` lives beside the other override files:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/overrides/moons.override.json
```

Preset-specific moon overrides live under:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/presets/<preset-id>/overrides/moons.override.json
```

## Contract

Moon overrides are partial. Omitted or `null` fields keep the current runtime value.

Supported fields:

- `moonId`: required. Matches `MoonDefinition.id` / `SelectableLevel.name`.
- `riskLevel`: optional numeric shorthand from `0..5`.
- `riskLabel`: optional exact text for `SelectableLevel.riskLevel`; wins over `riskLevel` when both are present.
- `routePrice`: optional terminal reroute cost.

## Example

```json
{
  "overrides": [
    {
      "moonId": "ExperimentationLevel",
      "riskLevel": 2,
      "routePrice": 150
    },
    {
      "moonId": "TitanLevel",
      "riskLabel": "S+",
      "routePrice": 900
    }
  ]
}
```

## Route Price Behavior

Route prices are applied to every loaded `TerminalNode` whose `buyRerouteToMoon` points to the target moon's level index.

This is intentionally conservative:

- If no terminal route nodes exist for a moon, the override logs a warning and continues.
- Exported `moon-economy.json` is used for validation hints only.
- Runtime mutation still scans loaded `TerminalNode` objects so it can handle nodes discovered after catalog export.
