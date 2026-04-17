# Structured Validation V1

OverseerProtocol validates `.cfg` tuning before runtime mutation. Validation is conservative: broken references are reported and skipped, while missing critical runtime catalogs abort the affected flow.

## Logging Category

All validation issues are logged under `Validation`:

- `Info`: non-problematic notes.
- `Warning`: recoverable invalid data. The affected entry is skipped or normalized.
- `Error`: critical validation failure. The affected tuning flow is not applied.

Internally validators still consume normalized override collections, but the user edits BepInEx `.cfg`.

## Spawn Tuning Rules

- `MoonId` must exist in the runtime moon catalog.
- `EnemyId` must exist in the runtime enemy registry discovered from active level pools.
- Missing runtime moon or enemy catalogs are validation errors.
- Unknown moon IDs skip the whole moon block.
- Unknown enemy IDs skip only that spawn entry.
- `rarity` is clamped to `0..1000`.
- Invalid `Entries` text is logged and skipped without rewriting the config file.

## Item Tuning Rules

- `ItemId` must resolve to a runtime `Item`.
- Missing runtime item catalog is a validation error.
- `creditsWorth` below `0` is clamped to `0`.
- `weight` must be finite; invalid weights are skipped and negative values clamp to `0`.

## Moon Tuning Rules

- `MoonId` must resolve to a runtime `SelectableLevel`.
- `riskLevel` is clamped to `0..5`.
- `riskLabel` can replace the generated risk label when non-empty and at most 32 chars.
- `routePrice` below `0` is clamped to `0`.
- If no terminal route node exists for a moon, route price application may be a no-op and logs a warning.

## Runtime Controls

- `StrictValidation = true`: warnings abort the affected tuning flow.
- `AbortOnInvalidOverrideBlock = true`: warnings abort the affected tuning flow.
- `DryRunOverrides = true`: validation runs, but item tuning, spawn tuning, moon tuning, runtime multipliers, and runtime rules do not mutate runtime state.
