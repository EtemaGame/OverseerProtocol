using BepInEx.Configuration;

namespace OverseerProtocol.Configuration;

public static class OPConfig
{
    public const string DefaultPreset = "default";

    public static ConfigEntry<bool> EnableDataExport { get; private set; } = null!;
    public static ConfigEntry<bool> EnableItemOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableMoonOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableSpawnOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableRuntimeMultipliers { get; private set; } = null!;
    public static ConfigEntry<bool> EnableProgressionStorage { get; private set; } = null!;
    public static ConfigEntry<bool> EnablePerkCatalog { get; private set; } = null!;
    public static ConfigEntry<bool> EnableLobbyRulesLoading { get; private set; } = null!;
    public static ConfigEntry<bool> EnableExperimentalMultiplayer { get; private set; } = null!;
    public static ConfigEntry<bool> EnableExpandedLobbyPatch { get; private set; } = null!;
    public static ConfigEntry<bool> EnableLateJoinSafeMode { get; private set; } = null!;
    public static ConfigEntry<bool> EnableSpectatorModeScaffold { get; private set; } = null!;
    public static ConfigEntry<bool> EnableHandshakeCompatibilityChecks { get; private set; } = null!;
    public static ConfigEntry<bool> EnableRuntimeRulesLoading { get; private set; } = null!;
    public static ConfigEntry<bool> EnableAdminTerminalCommands { get; private set; } = null!;
    public static ConfigEntry<bool> StrictValidation { get; private set; } = null!;
    public static ConfigEntry<bool> DryRunOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> AbortOnInvalidOverrideBlock { get; private set; } = null!;
    public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
    public static ConfigEntry<string> AdminCommandPrefix { get; private set; } = null!;
    public static ConfigEntry<string> AggressionProfile { get; private set; } = null!;
    public static ConfigEntry<float> ItemWeightMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> SpawnRarityMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> RoutePriceMultiplier { get; private set; } = null!;
    public static ConfigEntry<int> ExperimentalMaxPlayers { get; private set; } = null!;

    public static string ActivePresetName
    {
        get
        {
            var value = ActivePreset?.Value;
            return string.IsNullOrWhiteSpace(value) ? DefaultPreset : value.Trim();
        }
    }

    public static void Bind(ConfigFile config)
    {
        EnableDataExport = config.Bind(
            "General",
            "EnableDataExport",
            true,
            "Exports vanilla catalogs before runtime user tuning is applied.");

        ActivePreset = config.Bind(
            "General",
            "ActivePreset",
            DefaultPreset,
            "Preset/profile name for global multiplier defaults. Detailed tuning lives in overseer-data/items.json and overseer-data/moons/<MoonId>.json.");

        EnableItemOverrides = config.Bind(
            "Overrides",
            "EnableItemOverrides",
            true,
            "Applies supported item edits from overseer-data/items.json after export.");

        EnableMoonOverrides = config.Bind(
            "Overrides",
            "EnableMoonOverrides",
            true,
            "Applies supported moon edits from overseer-data/moons/<MoonId>.json after export.");

        EnableSpawnOverrides = config.Bind(
            "Overrides",
            "EnableSpawnOverrides",
            true,
            "Applies supported spawn pool edits from overseer-data/moons/<MoonId>.json after export.");

        EnableRuntimeMultipliers = config.Bind(
            "Multipliers",
            "EnableRuntimeMultipliers",
            true,
            "Applies simple .cfg multipliers after user JSON tuning.");

        EnableProgressionStorage = config.Bind(
            "Progression",
            "EnableProgressionStorage",
            true,
            "Creates and loads the progression save file used by future player and ship perks.");

        EnablePerkCatalog = config.Bind(
            "Progression",
            "EnablePerkCatalog",
            true,
            "Creates and loads perks.json definitions for future player and ship perk application.");

        EnableLobbyRulesLoading = config.Bind(
            "Lobby",
            "EnableLobbyRulesLoading",
            true,
            "Creates and loads lobby-rules.json for future expanded lobby, late join, and sync enforcement.");

        EnableExperimentalMultiplayer = config.Bind(
            "ExperimentalMultiplayer",
            "EnableExperimentalMultiplayer",
            false,
            "Master switch for experimental multiplayer scaffolding. Disabled by default.");

        EnableExpandedLobbyPatch = config.Bind(
            "ExperimentalMultiplayer",
            "EnableExpandedLobbyPatch",
            false,
            "Experimental. Attempts reflection-based max player patching from lobby-rules.json.");

        EnableLateJoinSafeMode = config.Bind(
            "ExperimentalMultiplayer",
            "EnableLateJoinSafeMode",
            false,
            "Experimental. Enables late-join policy evaluation and diagnostics; does not provide full moon state recovery.");

        EnableSpectatorModeScaffold = config.Bind(
            "ExperimentalMultiplayer",
            "EnableSpectatorModeScaffold",
            false,
            "Experimental. Enables spectator-mode diagnostics and command scaffolding only.");

        EnableHandshakeCompatibilityChecks = config.Bind(
            "ExperimentalMultiplayer",
            "EnableHandshakeCompatibilityChecks",
            true,
            "Enables local handshake compatibility diagnostics for future host/client sync.");

        EnableRuntimeRulesLoading = config.Bind(
            "RuntimeRules",
            "EnableRuntimeRulesLoading",
            true,
            "Creates and loads runtime-rules.json for future economy, ship, weather, and moon-specific rules.");

        EnableAdminTerminalCommands = config.Bind(
            "Admin",
            "EnableAdminTerminalCommands",
            false,
            "Experimental. Enables OverseerProtocol admin commands in the in-game Terminal.");

        AdminCommandPrefix = config.Bind(
            "Admin",
            "AdminCommandPrefix",
            "op",
            "Prefix used by OverseerProtocol admin terminal commands.");

        StrictValidation = config.Bind(
            "Validation",
            "StrictValidation",
            false,
            "When true, any validation warning aborts the affected tuning flow. Errors always abort.");

        DryRunOverrides = config.Bind(
            "Validation",
            "DryRunOverrides",
            false,
            "When true, user tuning files are loaded and validated but no runtime mutations are applied.");

        AbortOnInvalidOverrideBlock = config.Bind(
            "Validation",
            "AbortOnInvalidOverrideBlock",
            false,
            "Reserved policy flag for stricter validators. When true, invalid tuning blocks should abort the affected flow instead of being skipped.");

        AggressionProfile = config.Bind(
            "SemanticDifficulty",
            "AggressionProfile",
            "Balanced",
            new ConfigDescription(
                "Semantic spawn pressure profile applied on top of SpawnRarityMultiplier.",
                new AcceptableValueList<string>("Calm", "Balanced", "Aggressive", "Nightmare")));

        ItemWeightMultiplier = config.Bind(
            "Multipliers",
            "ItemWeightMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime item weight after items.json tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        SpawnRarityMultiplier = config.Bind(
            "Multipliers",
            "SpawnRarityMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime spawn pool rarity after moons/*.json spawn tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        RoutePriceMultiplier = config.Bind(
            "Multipliers",
            "RoutePriceMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime moon route price after moons/*.json tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        ExperimentalMaxPlayers = config.Bind(
            "ExperimentalMultiplayer",
            "ExperimentalMaxPlayers",
            4,
            new ConfigDescription(
                "Upper bound used by experimental expanded lobby patching. lobby-rules.json may lower this value.",
                new AcceptableValueRange<int>(1, 64)));
    }
}
