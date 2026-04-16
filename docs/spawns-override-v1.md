# spawns.override.json V1

`spawns.override.json` lives in `BepInEx/plugins/OverseerProtocol/overseer-data/overrides/`.

Spawn overrides are replacement-only in V1. If a pool property is present, OverseerProtocol replaces that moon's runtime pool with the entries in the file. If a pool property is omitted or `null`, the vanilla pool is kept. If a pool property is an empty array, the runtime pool is replaced with an empty pool.

## File Shape

```json
{
  "overrides": [
    {
      "moonId": "LevelInternalId",
      "insideEnemies": [
        {
          "enemyId": "EnemyInternalId",
          "rarity": 20
        }
      ],
      "outsideEnemies": [],
      "daytimeEnemies": null
    }
  ]
}
```

## Fields

- `overrides`: List of moon spawn override blocks.
- `moonId`: Stable internal moon ID. Use `id` from `exports/moons/moons.json`.
- `insideEnemies`: Optional replacement list for the indoor enemy pool.
- `outsideEnemies`: Optional replacement list for the outdoor enemy pool.
- `daytimeEnemies`: Optional replacement list for the daytime enemy pool.
- `enemyId`: Stable internal enemy ID. Use `id` from `exports/enemies/enemies.json`.
- `rarity`: Spawn rarity passed to Lethal Company's `SpawnableEnemyWithRarity`.

## V1 Safety Rules

- Unknown `moonId` values are skipped when they are absent from the exported moon catalog.
- Unknown `enemyId` values are skipped when they are absent from the exported enemy catalog.
- If the exported moon or enemy catalog cannot be loaded, spawn override application is aborted.
- Enemy IDs must also resolve to a live runtime `EnemyType`; unresolved entries are skipped.
- `rarity` is clamped to `0..1000`.
