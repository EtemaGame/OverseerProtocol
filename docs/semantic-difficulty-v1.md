# Semantic Difficulty V1

Semantic difficulty adds intent-based controls on top of numeric tuning.

## AggressionProfile

`AggressionProfile` lives in:

```text
BepInEx/config/com.overseerprotocol.core.cfg
```

```ini
[SemanticDifficulty]
AggressionProfile = Balanced
```

Allowed values:

- `Calm`: `0.75x` spawn rarity pressure.
- `Balanced`: `1.0x` spawn rarity pressure.
- `Aggressive`: `1.25x` spawn rarity pressure.
- `Nightmare`: `1.6x` spawn rarity pressure.

The semantic multiplier is applied on top of the active preset and `SpawnRarityMultiplier`.

Example:

```text
hardcore preset spawn multiplier 1.35
AggressionProfile = Aggressive
effective spawn multiplier = 1.35 * 1.25 = 1.6875
```

The final runtime rarity values are still clamped to `0..1000`.
