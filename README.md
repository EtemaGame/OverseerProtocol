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
Once the state is organized, OverseerProtocol allows for runtime modifications through an external override layer.
- **Item Overrides**: Modify weights, store prices, and properties safely.
- **Spawn Overrides**: Reconfigure enemy pools and rarities per moon.
- **Safety First**: Every modification includes strict validation to prevent crashes.

## 🚀 Development Status (Foundation V1)

The official project roadmap lives in [`ROADMAP.md`](ROADMAP.md). It is the source of truth for phase order, gates, non-goals, and the distinction between runtime features, experimental scaffolds, and data-only contracts.

Reference setup instructions live in [`docs/references-setup.md`](docs/references-setup.md).

Latest implementation review: [`docs/implementation-review-2026-04-16.md`](docs/implementation-review-2026-04-16.md).

- [x] **Infrastructure**: BepInEx loading, Harmony patching, stable lifecycle triggers.
- [x] **Data Layer**: Clean exports for items, moons, and enemies.
- [x] **Item Overrides (V1)**: Runtime modification of weight and store prices.
- [x] **Spawn Overrides (V1)**: Pool replacement and rarity tuning per moon.
- [x] **Validation Layer**: Cross-reference resolution and safety checks.
- [x] **Hybrid Config**: `.cfg` for simple toggles + JSON for complex profiles.
- [x] **Moon Economy V2**: Route price resolution and raw TerminalNode economy export.
- [x] **Moon Overrides (V1)**: Risk labels and route prices through `moons.override.json`.
- [x] **Presets V1**: Built-in preset manifests, safe multipliers, override templates, and rules templates.
- [x] **Semantic Difficulty V1**: Aggression profiles layered over spawn rarity multipliers.
- [x] **Runtime Snapshot V1**: In-memory vanilla baseline for reset/reload workflows.
- [x] **Runtime Rules V1**: Data contract plus active route/travel-discount rule application.
- [x] **Progression Persistence V1**: Data-only player/ship progression save model with ship debug commands.
- [x] **Perk Catalog V1**: Seeded player and ship perk definition model.
- [x] **Admin Commands V1**: Command service for export, reload, reset, fingerprints, rules, perks, progression, multiplayer, and handshake summaries.
- [x] **Admin Terminal Hook V1**: Experimental terminal hook, disabled by default.
- [x] **Handshake Contract V1**: Host/client compatibility payload with preset/config fingerprints and local comparison service.
- [x] **Experimental Multiplayer V1**: Disabled-by-default scaffold for expanded lobby diagnostics, late-join policy, spectator diagnostics, and reserved sync snapshots.
- [x] **Reference Setup V1**: Helper script for BepInEx/Harmony download and local game assembly copy.

## Current Verification Status

- BepInEx and Harmony references are present under `references/bepinex`.
- Lethal Company game assemblies are still required under `references/game` before a real build can validate game symbols.
- JSON sample validation passes with local Node.
- `dotnet build` is pending because `dotnet` is not currently available in PATH.

---

*OverseerProtocol is the foundation for the next generation of data-driven Lethal Company experiences. It transforms a survival horror game into a managed, monitorable environment.*

---

- License: MIT License.
