# Structured Validation V1

OverseerProtocol validates user tuning files before runtime mutation. Validation is intentionally conservative: broken references are reported and skipped, while missing critical catalogs abort the affected tuning flow.

## Logging Category

All validation issues are logged under the `Validation` category with one of three severities:

- `Info`: Non-problematic validation notes.
- `Warning`: Recoverable invalid data. The affected entry is skipped or normalized.
- `Error`: Critical validation failure. The affected tuning flow is not applied.

Every issue includes a stable code and, when possible, a JSON-style path such as `overrides[0].insideEnemies[2].rarity`. Internally the validators still use normalized override collections, but the user edits `items.json` and `moons/*.json`.

## Spawn Tuning Rules

- `moonId` must exist in the exported moon catalog at `exports/moons/moons.json`.
- `enemyId` must exist in the exported enemy catalog at `exports/enemies/enemies.json`.
- `enemyId` must also resolve to a runtime `EnemyType` discovered from active level pools.
- Missing moon or enemy catalogs are validation errors and abort spawn tuning application.
- Unknown moon IDs skip the whole moon tuning block.
- Unknown enemy IDs skip only that spawn entry.
- Duplicate moon IDs are warnings; the first valid moon tuning block wins.
- Duplicate enemy IDs inside a pool are warnings but retained because repeated entries may be used for weighting experiments.
- `rarity` is clamped to the inclusive range `0..1000`.

## Item Tuning Rules

- `id` must exist in the exported item catalog at `exports/items/items.json`.
- `id` must also resolve to a runtime `Item` from `StartOfRound.Instance.allItemsList.itemsList`.
- Missing exported or runtime item catalogs are validation errors and abort item tuning application.
- Unknown item IDs skip the whole item tuning block.
- Duplicate item IDs are warnings; the first valid item tuning block wins.
- `creditsWorth` values below `0` are clamped to `0`.
- `weight` must be finite. Invalid weights are skipped; negative weights are clamped to `0`.
- An item tuning block with no supported runtime fields is skipped.

## Moon Tuning Rules

- `moonId` must exist in the exported moon catalog at `exports/moons/moons.json`.
- `moonId` must also resolve to a runtime `SelectableLevel`.
- Missing exported or runtime moon catalogs are validation errors and abort moon tuning application.
- Unknown moon IDs skip the whole moon tuning block.
- Duplicate moon IDs are warnings; the first valid moon tuning block wins.
- `riskLevel` is clamped to the inclusive range `0..5` and mapped to runtime labels `None`, `D`, `C`, `B`, `A`, `S`.
- `riskLabel` can be used instead of `riskLevel` for exact runtime text. Empty labels and labels longer than 32 characters are skipped.
- `routePrice` values below `0` are clamped to `0`.
- If a moon has no exported terminal route node, route price validation logs a warning and application may be a no-op for that field.

## Runtime Controls

The `.cfg` validation section controls how validation affects runtime mutation:

- `StrictValidation = false`: warnings are logged and valid normalized entries continue.
- `StrictValidation = true`: any warning aborts the affected tuning flow.
- `DryRunOverrides = true`: exports and validation run, but item tuning, spawn tuning, moon tuning, runtime multipliers, and runtime rules do not mutate the runtime catalogs.

## Safety Contract

Validation never crashes the game for malformed user data. It reports, sanitizes, skips, or aborts the specific tuning flow depending on severity.
