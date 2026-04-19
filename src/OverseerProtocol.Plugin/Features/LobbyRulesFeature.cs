using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Lobby;

namespace OverseerProtocol.Features;

public sealed class LobbyRulesFeature
{
    public LobbyRulesDefinition LoadOrCreate()
    {
        var rules = new LobbyRulesDefinition
        {
            MaxPlayers = OPConfig.MaxPlayers.Value,
            EnableExpandedLobby = OPConfig.EnableMultiplayer.Value && OPConfig.MaxPlayers.Value > 4,
            AllowLateJoin = OPConfig.EnableMultiplayer.Value && OPConfig.EnableLateJoin.Value,
            LateJoinMode = ResolveLateJoinMode(),
            EnableSpectatorMode = OPConfig.EnableMultiplayer.Value && OPConfig.LateJoinOnMoonAsSpectator.Value,
            RequireMatchingOverseerVersion = OPConfig.RequireSameModVersion.Value,
            RequireMatchingPreset = false,
            SyncPresetToClients = false,
            SyncOverridesToClients = OPConfig.RequireSameConfigHash.Value
        };

        rules = Normalize(rules);
        OPLog.Info("Multiplayer", $"Loaded multiplayer rules from .cfg: maxPlayers={rules.MaxPlayers}, lateJoin={rules.AllowLateJoin}, mode={rules.LateJoinMode}, spectator={rules.EnableSpectatorMode}");
        return rules;
    }

    private static string ResolveLateJoinMode()
    {
        if (!OPConfig.EnableLateJoin.Value)
            return "Disabled";

        if (OPConfig.LateJoinOnMoonAsSpectator.Value)
            return "MoonSpectator";

        return OPConfig.LateJoinInOrbit.Value ? "Orbit" : "Disabled";
    }

    private static LobbyRulesDefinition Normalize(LobbyRulesDefinition rules)
    {
        if (rules.SchemaVersion <= 0)
        {
            OPLog.Info("Multiplayer", $"Normalizing schemaVersion {rules.SchemaVersion} -> 1");
            rules.SchemaVersion = 1;
        }

        if (rules.MaxPlayers < 1)
        {
            OPLog.Warning("Multiplayer", $"maxPlayers={rules.MaxPlayers} is below 1. Clamping to 1.");
            rules.MaxPlayers = 1;
        }

        if (rules.MaxPlayers > 64)
        {
            OPLog.Warning("Multiplayer", $"maxPlayers={rules.MaxPlayers} is above 64. Clamping to 64.");
            rules.MaxPlayers = 64;
        }

        if (string.IsNullOrWhiteSpace(rules.LateJoinMode))
        {
            OPLog.Info("Multiplayer", "LateJoinMode was empty. Normalizing to Disabled.");
            rules.LateJoinMode = "Disabled";
        }

        rules.LateJoinMode = rules.LateJoinMode.Trim();

        return rules;
    }
}
