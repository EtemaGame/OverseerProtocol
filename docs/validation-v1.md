# Structured Validation V1

OverseerProtocol validates override files before runtime mutation. Validation is intentionally conservative: broken references are reported and skipped, while missing critical catalogs abort the affected override flow.

## Logging Category

All validation issues are logged under the `Validation` category with one of three severities:

- `Info`: Non-problematic validation notes.
- `Warning`: Recoverable invalid data. The affected entry is skipped or normalized.
- `Error`: Critical validation failure. The affected override flow is not applied.

Every issue includes a stable code and, when possible, a JSON-style path such as `overrides[0].insideEnemies[2].rarity`.

## Spawn Override Rules

- `moonId` must exist in the exported moon catalog at `exports/moons/moons.json`.
- `enemyId` must exist in the exported enemy catalog at `exports/enemies/enemies.json`.
- `enemyId` must also resolve to a runtime `EnemyType` discovered from active level pools.
- Missing moon or enemy catalogs are validation errors and abort spawn override application.
- Unknown moon IDs skip the whole moon override.
- Unknown enemy IDs skip only that spawn entry.
- Duplicate moon IDs are warnings; the first valid moon override wins.
- Duplicate enemy IDs inside a pool are warnings but retained because repeated entries may be used for weighting experiments.
- `rarity` is clamped to the inclusive range `0..1000`.

## Safety Contract

Validation never crashes the game for malformed user data. It reports, sanitizes, skips, or aborts the specific override flow depending on severity.
