# OverseerProtocol

**OverseerProtocol** is a modular framework designed for high-performance game data management and monitoring in **Lethal Company**. It provides a structured foundation for extracting, analyzing, and enforcing protocols on game assets and runtime states.

## 🏗 Modular Architecture

The project is split into four specialized assemblies to ensure clean separation of concerns:

- **`OverseerProtocol.Core`**: Common utilities, logging wrappers, and path management.
- **`OverseerProtocol.Data`**: Strongly-typed data models for game entities (Items, Moons, Enemies, Spawns).
- **`OverseerProtocol.GameAbstractions`**: Logic for reading and converting internal game data into consistent protocol formats.
- **`OverseerProtocol.Plugin`**: The BepInEx entry point that manages lifecycle hooks and feature coordination.

## 🚀 Key Features

- **Lifecycle-Aware Execution**: Hooks into the game state (via Harmony patches on `StartOfRound`) to ensure data is only accessed when global catalogs are fully initialized.
- **Data Export System**: Automatically exports internal game catalogs (like Items) to JSON formats for external analysis or protocol enforcement.
- **Modular Feature System**: Designed to allow independent features (like `DataExportFeature`) to be toggled and configured easily.

## 🛠 Developer Setup

To set up the development environment, you must populate the `references/` folder with the necessary game and BepInEx assemblies. 

1. **Clone the repository.**
2. **Sync References**: Use the provided PowerShell script to copy local assemblies from your game installation:
   ```powershell
   ./tools/sync-references.ps1
   ```
3. **Build**: Use `dotnet build` or your preferred IDE.
4. **Deploy**: Use the deployment script to copy the compiled assemblies to your game profile:
   ```powershell
   ./tools/deploy-dev.ps1
   ```

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

*Note: This mod is intended for research and advanced data management. Always respect the game's terms of service and other players.*
