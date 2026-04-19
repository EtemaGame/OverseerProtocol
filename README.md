# OverseerProtocol

**OverseerProtocol** is a systemic, data-driven framework for **Lethal Company** designed for stability, deep observability, and maintainable game reconfiguration.

Inspired by the legacy of AdvancedCompany but built without technical debt, it focuses on the mantra: **Observe &rarr; Organize &rarr; Override**.

## 👁️ The Vision

OverseerProtocol acts as a sophisticated bridge between the chaotic runtime of Lethal Company and a structured data layer. It provides a "Single Source of Truth" for game mechanics, allowing for precise monitoring and controlled modifications without polluting the base game state.

## 🏗️ Technical Pillars

### 1. Unified Identity (Observe)
We deep-scan the game's internal registries (Items, Moons, Enemies, Spawns) and normalize them into a predictable format. Every entity is assigned a stable internal `Id` used for matching, ensuring consistency across modded environments.

### 2. Digital Export (Organize)
Snapshots of the game's vanilla state are exported as structured JSON. This "vanilla photo" allows for:
- **Game Analysis**: Deep insight into spawn rates and economic profiles.
- **Contract Documentation**: Clear visibility into what the game exposes to modders.

### 3. Controlled Overrides (Override)
Once the state is organized, OverseerProtocol applies user tuning from the standard BepInEx config file:
- **Items**: `Items.<ItemId>` sections expose one field per editable property: enable switch, value, weight, store membership, battery flag, and scrap value range.
- **Moons**: `Moons.<MoonId>` sections expose one field per editable property: enable switch, route price, tier, description, scrap counts, enemy pools, and route multiplier.
- **Spawns**: moon enemy pools live beside each moon as `InsideEnemies`, `OutsideEnemies`, and `DaytimeEnemies` fields with matching enable toggles.
- **Safety First**: Every supported runtime modification includes validation to prevent crashes.

## 🚀 Development Status (Foundation V1)

The official project roadmap lives in [`ROADMAP.md`](ROADMAP.md). It is the source of truth for phase order, gates, non-goals, and the distinction between runtime features, experimental scaffolds, and data-only contracts.

Reference setup instructions live in [`docs/references-setup.md`](docs/references-setup.md).

User tuning instructions live in [`docs/user-tuning-v1.md`](docs/user-tuning-v1.md). Multiplayer details live in [`docs/multiplayer-v1.md`](docs/multiplayer-v1.md).

Latest implementation review: [`docs/implementation-review-2026-04-16.md`](docs/implementation-review-2026-04-16.md).

- [x] **Infrastructure**: BepInEx loading, Harmony patching, stable lifecycle triggers.
- [x] **Data Layer**: Clean exports for items, moons, and enemies.
- [x] **Item Tuning (V1)**: Runtime modification of item weight and value through BepInEx `.cfg`.
- [x] **Spawn Tuning (V1)**: Pool replacement and rarity tuning through BepInEx `.cfg` sections.
- [x] **Validation Layer**: Cross-reference resolution and safety checks.
- [x] **BepInEx Config**: `.cfg` is the sole user-editable source of truth for runtime tuning.
- [x] **Moon Economy V2**: Route price resolution and raw TerminalNode economy export.
- [x] **Moon Tuning (V1)**: Risk labels and route prices through BepInEx `.cfg`.
- [x] **Advanced Presets**: Optional built-in presets hidden under `[Advanced]`.
- [x] **Runtime Snapshot V1**: In-memory vanilla baseline for reset/reload workflows.
- [x] **Gameplay Route Rules V1**: `[Gameplay].TravelDiscountMultiplier` plus per-moon `RouteMultiplier` fields.
- [x] **Progression Persistence V1**: Data-only player/ship progression save model surfaced in the Overseer panel.
- [x] **Perk Catalog V1**: Seeded player and ship perk definition model.
- [x] **Interiors V1**: Runtime interior catalog plus per-moon `InteriorWeights` tuning.
- [x] **Admin Commands V1**: Minimal always-on terminal command surface for `op help` and `op reload`.
- [x] **Overseer Panel V1**: InputUtils-backed read-only HUD panel for multiplayer, player progression, and host-only ship perk state.
- [x] **LethalConfig Bridge V1**: Optional in-game editor registration for dynamic item, moon, interior, and utility config entries when LethalConfig is installed.
- [x] **Admin Terminal Hook V1**: Terminal hook for the small `op` command surface.
- [x] **Handshake Contract V1**: Host/client compatibility payload with preset/config fingerprints and local comparison service.
- [x] **Multiplayer V1**: Configurable max players, late-join policy, spectator-gated moon joins, and HUD/panel status.
- [x] **Reference Setup V1**: Helper script for BepInEx/Harmony download and local game assembly copy.

## Current Verification Status

- BepInEx and Harmony references are present under `references/bepinex`.
- Lethal Company game assemblies are available locally for build-time symbol validation.
- User tuning is configured through `BepInEx/config/com.overseerprotocol.core.cfg`.
- JSON exports remain diagnostic and non-authoritative.

---

*OverseerProtocol is the foundation for the next generation of data-driven Lethal Company experiences. It transforms a survival horror game into a managed, monitorable environment.*

---

- License: MIT License.
