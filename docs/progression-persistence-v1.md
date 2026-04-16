# Progression Persistence V1

Progression persistence is intentionally a data contract first. V1 creates and normalizes a save file, but it does not yet apply gameplay perks.

Save path:

```text
BepInEx/plugins/OverseerProtocol/overseer-data/saves/progression.json
```

Config gate:

```ini
[Progression]
EnableProgressionStorage = true
```

## Domains

The save file separates progression into:

- `ship`: Shared ship progression and future ship perk ranks.
- `players`: Per-player progression and future player perk ranks.
- `activePreset`: Last known preset context for the save.

## Current Contract

```json
{
  "schemaVersion": 1,
  "saveId": "default",
  "activePreset": "default",
  "lastUpdatedUtc": "2026-04-16T00:00:00.0000000Z",
  "ship": {
    "level": 0,
    "experience": 0,
    "unspentPoints": 0,
    "perkRanks": {}
  },
  "players": []
}
```

## Perk Catalog Shape

Perk definitions are modeled separately from saved ranks:

- `playerPerks`: Sprint, jump, carry, ladder, resistance, and similar player stats.
- `shipPerks`: Scanner, battery, travel discount, dropship, loot saver, and similar ship stats.

The current implementation only defines the model. Runtime perk application should come after item/spawn/moon overrides and terminal/admin tooling are stable.
