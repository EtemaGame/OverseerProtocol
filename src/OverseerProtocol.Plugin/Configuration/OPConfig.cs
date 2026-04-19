using BepInEx.Configuration;

namespace OverseerProtocol.Configuration;

public static class OPConfig
{
    public const string DefaultPreset = "default";
    public const int IntUnset = -1;
    public const float FloatUnset = -1f;

    public static ConfigFile ConfigFile { get; private set; } = null!;

    public static ConfigEntry<string> AdminCommandPrefix { get; private set; } = null!;
    public static ConfigEntry<bool> StrictValidation { get; private set; } = null!;
    public static ConfigEntry<bool> DryRunOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> AbortOnInvalidOverrideBlock { get; private set; } = null!;

    public static ConfigEntry<bool> EnableItemOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableMoonOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableSpawnOverrides { get; private set; } = null!;
    public static ConfigEntry<bool> EnableRuntimeMultipliers { get; private set; } = null!;
    public static ConfigEntry<float> ItemWeightMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> SpawnRarityMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> RoutePriceMultiplier { get; private set; } = null!;
    public static ConfigEntry<float> TravelDiscountMultiplier { get; private set; } = null!;

    public static ConfigEntry<bool> EnableMultiplayer { get; private set; } = null!;
    public static ConfigEntry<int> MaxPlayers { get; private set; } = null!;
    public static ConfigEntry<bool> EnableLateJoin { get; private set; } = null!;
    public static ConfigEntry<bool> LateJoinInOrbit { get; private set; } = null!;
    public static ConfigEntry<bool> LateJoinOnMoonAsSpectator { get; private set; } = null!;
    public static ConfigEntry<bool> ShowLobbyStatusHud { get; private set; } = null!;
    public static ConfigEntry<bool> ShowLobbyHostMenu { get; private set; } = null!;
    public static ConfigEntry<bool> RequireSameModVersion { get; private set; } = null!;
    public static ConfigEntry<bool> RequireSameConfigHash { get; private set; } = null!;

    public static ConfigEntry<bool> ReduceVerboseLogs { get; private set; } = null!;
    public static ConfigEntry<bool> VerboseDiagnostics { get; private set; } = null!;
    public static ConfigEntry<bool> EnableDataExport { get; private set; } = null!;
    public static ConfigEntry<bool> DisableDiagnosticExportsAfterFirstRun { get; private set; } = null!;
    public static ConfigEntry<bool> EnableLethalConfigBridge { get; private set; } = null!;
    public static ConfigEntry<string> NetworkLogLevel { get; private set; } = null!;

    public static ConfigEntry<bool> EnableProgressionStorage { get; private set; } = null!;
    public static ConfigEntry<bool> EnablePerkCatalog { get; private set; } = null!;

    public static ConfigEntry<string> ActivePreset { get; private set; } = null!;
    public static ConfigEntry<string> LastLobbyPreset { get; private set; } = null!;

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

        AdminCommandPrefix = config.Bind(
            "General",
            "AdminCommandPrefix",
            "op",
            "Prefix used by OverseerProtocol terminal commands.");

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
            "When true, invalid tuning blocks abort the affected flow instead of being skipped.");

        EnableItemOverrides = config.Bind(
            "Gameplay",
            "EnableItemOverrides",
            true,
            "Applies supported item edits from per-item sections like [Items.Shovel].");

        EnableMoonOverrides = config.Bind(
            "Gameplay",
            "EnableMoonOverrides",
            true,
            "Applies supported moon edits from per-moon sections like [Moons.ExperimentationLevel].");

        EnableSpawnOverrides = config.Bind(
            "Gameplay",
            "EnableSpawnOverrides",
            true,
            "Applies supported enemy pool edits from each [Moons.<MoonId>] section.");

        EnableRuntimeMultipliers = config.Bind(
            "Gameplay",
            "EnableMultipliers",
            true,
            "Applies global gameplay multipliers after explicit item/moon/spawn overrides.");

        ItemWeightMultiplier = config.Bind(
            "Gameplay",
            "ItemWeightMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime item weight after explicit item tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        SpawnRarityMultiplier = config.Bind(
            "Gameplay",
            "SpawnRarityMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime spawn pool rarity after explicit spawn tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        RoutePriceMultiplier = config.Bind(
            "Gameplay",
            "RoutePriceMultiplier",
            1f,
            new ConfigDescription(
                "Multiplies every runtime moon route price after explicit moon tuning. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        TravelDiscountMultiplier = config.Bind(
            "Gameplay",
            "TravelDiscountMultiplier",
            1f,
            new ConfigDescription(
                "Additional route price multiplier applied to all route prices. 1 keeps current values.",
                new AcceptableValueRange<float>(0f, 10f)));

        EnableMultiplayer = config.Bind(
            "Multiplayer",
            "EnableMultiplayer",
            false,
            "Enables OverseerProtocol multiplayer patches and status UI.");

        MaxPlayers = config.Bind(
            "Multiplayer",
            "MaxPlayers",
            4,
            new ConfigDescription(
                "Requested lobby capacity.",
                new AcceptableValueRange<int>(1, 64)));

        EnableLateJoin = config.Bind(
            "Multiplayer",
            "EnableLateJoin",
            false,
            "Allows OverseerProtocol to evaluate late-join policy instead of vanilla always rejecting started games.");

        LateJoinInOrbit = config.Bind(
            "Multiplayer",
            "LateJoinInOrbit",
            true,
            "Allows late join while the ship is in orbit or lobby/ship phase.");

        LateJoinOnMoonAsSpectator = config.Bind(
            "Multiplayer",
            "LateJoinOnMoonAsSpectator",
            false,
            "Allows late join during an active moon as a spectator when player lifecycle hooks are safe for the current game version.");

        ShowLobbyStatusHud = config.Bind(
            "Multiplayer",
            "ShowLobbyStatusHud",
            true,
            "Shows a small Overseer multiplayer status label in-game.");

        ShowLobbyHostMenu = config.Bind(
            "Multiplayer",
            "ShowLobbyHostMenu",
            true,
            "Shows the OverseerProtocol lobby configuration menu before starting a hosted lobby.");

        RequireSameModVersion = config.Bind(
            "Multiplayer",
            "RequireSameModVersion",
            true,
            "Adds OverseerProtocol version compatibility to multiplayer status and handshake checks.");

        RequireSameConfigHash = config.Bind(
            "Multiplayer",
            "RequireSameConfigHash",
            false,
            "Adds config hash compatibility to multiplayer status and handshake checks.");

        ReduceVerboseLogs = config.Bind(
            "Utility",
            "ReduceVerboseLogs",
            true,
            "Reduces repetitive normal startup logs. Warnings and errors are always logged.");

        VerboseDiagnostics = config.Bind(
            "Utility",
            "VerboseDiagnostics",
            false,
            "Enables detailed startup diagnostics and test markers.");

        EnableDataExport = config.Bind(
            "Utility",
            "EnableDataExport",
            true,
            "Exports vanilla catalogs for diagnostics.");

        DisableDiagnosticExportsAfterFirstRun = config.Bind(
            "Utility",
            "DisableDiagnosticExportsAfterFirstRun",
            false,
            "Automatically disables diagnostic exports after a successful catalog export.");

        EnableLethalConfigBridge = config.Bind(
            "Utility",
            "EnableLethalConfigBridge",
            true,
            "When LethalConfig is installed, registers OverseerProtocol's dynamic item, moon, and interior config entries in its in-game menu.");

        NetworkLogLevel = config.Bind(
            "Utility",
            "NetworkLogLevel",
            "Normal",
            new ConfigDescription(
                "Controls Overseer multiplayer log verbosity.",
                new AcceptableValueList<string>("Quiet", "Normal", "Verbose")));

        EnableProgressionStorage = config.Bind(
            "Perks",
            "EnableProgressionStorage",
            true,
            "Creates and loads the progression save file used by player and ship perks.");

        EnablePerkCatalog = config.Bind(
            "Perks",
            "EnablePerkCatalog",
            true,
            "Creates and loads perks.json definitions.");

        ActivePreset = config.Bind(
            "Advanced",
            "ActivePreset",
            DefaultPreset,
            "Optional built-in preset/profile name used as a base for multipliers. Keep default for normal use.");

        LastLobbyPreset = config.Bind(
            "Advanced",
            "LastLobbyPreset",
            DefaultPreset,
            "Last lobby preset selected in the OverseerProtocol host menu.");
    }

    public static void Reload()
    {
        ConfigFile.Reload();
    }
}
