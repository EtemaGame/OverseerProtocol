using BepInEx.Configuration;

namespace OverseerProtocol.Configuration;

public static class OPConfig
{
    public const string DefaultPreset = "default";
    public const int IntUnset = -1;
    public const float FloatUnset = -1f;

    public static ConfigFile ConfigFile { get; private set; } = null!;
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
    public static ConfigEntry<bool> LobbyEnableExpandedLobby { get; private set; } = null!;
    public static ConfigEntry<bool> LobbyAllowLateJoin { get; private set; } = null!;
    public static ConfigEntry<bool> LobbyEnableSpectatorMode { get; private set; } = null!;
    public static ConfigEntry<bool> LobbyRequireMatchingOverseerVersion { get; private set; } = null!;
    public static ConfigEntry<bool> LobbyRequireMatchingPreset { get; private set; } = null!;
    public static ConfigEntry<bool> LobbySyncPresetToClients { get; private set; } = null!;
    public static ConfigEntry<bool> LobbySyncOverridesToClients { get; private set; } = null!;
    public static ConfigEntry<bool> StrictValidation { get; private set; } = null!;
    public static ConfigEntry<bool> DryRunOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> AbortOnInvalidOverrideBlock { get; private set; } = null!;
    public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
    public static ConfigEntry<string> AdminCommandPrefix { get; private set; } = null!;
    public static ConfigEntry<string> AggressionProfile { get; private set; } = null!;
    public static ConfigEntry<string> LobbyLateJoinMode { get; private set; } = null!;
    public static ConfigEntry<float> ItemWeightMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> SpawnRarityMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> RoutePriceMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> TravelDiscountMultiplier { get; private set; } = null!;
    public static ConfigEntry<int> ExperimentalMaxPlayers { get; private set; } = null!;
    public static ConfigEntry<int> LobbyMaxPlayers { get; private set; } = null!;

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
        ConfigFile = config;

        EnableDataExport = config.Bind(
            "General",
            "EnableDataExport",
            true,
            "Exports vanilla catalogs for diagnostics. Exports are not configuration and are never required for runtime tuning.");

        ActivePreset = config.Bind(
            "General",
            "ActivePreset",
            DefaultPreset,
            "Built-in preset/profile name used as a base for runtime multipliers. User overrides in this .cfg win over preset defaults.");

        EnableItemOverrides = config.Bind(
            "General",
            "EnableItemOverrides",
            true,
            "Applies supported item edits from the [Items] entity catalog.");

        EnableMoonOverrides = config.Bind(
            "General",
            "EnableMoonOverrides",
            true,
            "Applies supported moon edits from the [Moons] entity catalog.");

        EnableSpawnOverrides = config.Bind(
            "General",
            "EnableSpawnOverrides",
            true,
            "Applies supported spawn pool edits from [Moons.InsideEnemies], [Moons.OutsideEnemies], and [Moons.DaytimeEnemies].");

        EnableRuntimeMultipliers = config.Bind(
            "Multipliers",
            "EnableRuntimeMultipliers",
            true,
            "Applies simple .cfg multipliers after explicit item/moon/spawn overrides.");

        EnableProgressionStorage = config.Bind(
            "Perks",
            "EnableProgressionStorage",
            true,
            "Creates and loads the progression save file used by future player and ship perks.");

        EnablePerkCatalog = config.Bind(
            "Perks",
            "EnablePerkCatalog",
            true,
            "Creates and loads perks.json definitions for future player and ship perk application.");

        EnableLobbyRulesLoading = config.Bind(
            "Multiplayer",
            "EnableMultiplayerRules",
            true,
            "Loads multiplayer rules from this .cfg.");

        LobbyMaxPlayers = config.Bind(
            "Multiplayer",
            "MaxPlayers",
            4,
            new ConfigDescription(
                "Requested max players. Experimental patches are still required above vanilla limits.",
                new AcceptableValueRange<int>(1, 64)));

        LobbyEnableExpandedLobby = config.Bind(
            "Multiplayer",
            "EnableExpandedLobby",
            false,
            "Allows the experimental expanded lobby patch to run when the multiplayer patch switch is enabled.");

        LobbyAllowLateJoin = config.Bind(
            "Multiplayer",
            "AllowLateJoin",
            false,
            "Lobby rule for late-join diagnostics. Full moon-state recovery is not implemented.");

        LobbyLateJoinMode = config.Bind(
            "Multiplayer",
            "LateJoinMode",
            "Disabled",
            new ConfigDescription(
                "Late join policy used by diagnostics.",
                new AcceptableValueList<string>("Disabled", "Lobby", "Orbit", "Moon")));

        LobbyEnableSpectatorMode = config.Bind(
            "Multiplayer",
            "EnableSpectatorMode",
            false,
            "Lobby rule for spectator-mode diagnostics. Runtime spectator control is reserved.");

        LobbyRequireMatchingOverseerVersion = config.Bind(
            "Multiplayer",
            "RequireMatchingOverseerVersion",
            true,
            "Reserved handshake policy: require matching OverseerProtocol version.");

        LobbyRequireMatchingPreset = config.Bind(
            "Multiplayer",
            "RequireMatchingPreset",
            true,
            "Reserved handshake policy: require matching active preset.");

        LobbySyncPresetToClients = config.Bind(
            "Multiplayer",
            "SyncPresetToClients",
            true,
            "Reserved sync policy: host preset should be sent to clients when sync exists.");

        LobbySyncOverridesToClients = config.Bind(
            "Multiplayer",
            "SyncOverridesToClients",
            false,
            "Reserved sync policy: host .cfg overrides should be sent to clients when sync exists.");

        EnableExperimentalMultiplayer = config.Bind(
            "Multiplayer",
            "EnableExperimentalPatches",
            false,
            "Master switch for experimental multiplayer patches. Disabled by default.");

        EnableExpandedLobbyPatch = config.Bind(
            "Multiplayer",
            "EnableExpandedLobbyPatch",
            false,
            "Experimental. Attempts reflection-based max player patching.");

        EnableLateJoinSafeMode = config.Bind(
            "Multiplayer",
            "EnableLateJoinSafeMode",
            false,
            "Experimental. Enables late-join policy evaluation and diagnostics; does not provide full moon state recovery.");

        EnableSpectatorModeScaffold = config.Bind(
            "Multiplayer",
            "EnableSpectatorModeScaffold",
            false,
            "Experimental. Enables spectator-mode diagnostics and command scaffolding only.");

        EnableHandshakeCompatibilityChecks = config.Bind(
            "Multiplayer",
            "EnableHandshakeCompatibilityChecks",
            true,
            "Enables local handshake compatibility diagnostics for future host/client sync.");

        EnableRuntimeRulesLoading = config.Bind(
            "Multipliers",
            "EnableRouteRules",
            true,
            "Applies travel discount and per-moon route multipliers.");

        EnableAdminTerminalCommands = config.Bind(
            "General",
            "EnableAdminTerminalCommands",
            false,
            "Experimental. Enables OverseerProtocol admin commands in the in-game Terminal.");

        AdminCommandPrefix = config.Bind(
            "General",
            "AdminCommandPrefix",
            "op",
            "Prefix used by OverseerProtocol admin terminal commands.");

        StrictValidation = config.Bind(
            "General",
            "StrictValidation",
            false,
            "When true, any validation warning aborts the affected tuning flow. Errors always abort.");

        DryRunOverrides = config.Bind(
            "General",
            "DryRunOverrides",
            false,
            "When true, .cfg tuning is loaded and validated but no runtime mutations are applied.");

        AbortOnInvalidOverrideBlock = config.Bind(
            "General",
            "AbortOnInvalidOverrideBlock",
            false,
            "Reserved policy flag for stricter validators. When true, invalid tuning blocks should abort the affected flow instead of being skipped.");

        AggressionProfile = config.Bind(
            "Multipliers",
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
                "Multiplies every runtime item weight after explicit item tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        SpawnRarityMultiplier = config.Bind(
            "Multipliers",
            "SpawnRarityMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime spawn pool rarity after explicit spawn tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        RoutePriceMultiplier = config.Bind(
            "Multipliers",
            "RoutePriceMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime moon route price after explicit moon tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        TravelDiscountMultiplier = config.Bind(
            "Multipliers",
            "TravelDiscountMultiplier",
            1f,
            new ConfigDescription(
                "Additional route price multiplier applied by runtime rules. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        ExperimentalMaxPlayers = config.Bind(
            "Multiplayer",
            "ExperimentalMaxPlayers",
            4,
            new ConfigDescription(
                "Upper bound used by experimental expanded lobby patching. MaxPlayers may lower this value.",
                new AcceptableValueRange<int>(1, 64)));
    }

    public static void Reload()
    {
        ConfigFile.Reload();
    }
}
