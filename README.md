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

- [x] **Infrastructure**: BepInEx loading, Harmony patching, stable lifecycle triggers.
- [x] **Data Layer**: Clean exports for items, moons, and enemies.
- [x] **Item Overrides (V1)**: Runtime modification of weight and store prices.
- [x] **Spawn Overrides (V1)**: Pool replacement and rarity tuning per moon.
- [x] **Validation Layer**: Cross-reference resolution and safety checks.
- [x] **Hybrid Config**: `.cfg` for simple toggles + JSON for complex profiles.
- [x] **Moon Economy V2**: Route price resolution and raw TerminalNode economy export.
- [x] **Presets V1**: Built-in Vanilla Plus and Hardcore preset manifests with safe multipliers.
- [x] **Semantic Difficulty V1**: Aggression profiles layered over spawn rarity multipliers.

---

*OverseerProtocol is the foundation for the next generation of data-driven Lethal Company experiences. It transforms a survival horror game into a managed, monitorable environment.*

---

- License: MIT License.
