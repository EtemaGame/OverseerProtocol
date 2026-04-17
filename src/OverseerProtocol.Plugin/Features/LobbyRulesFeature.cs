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
            MaxPlayers = OPConfig.LobbyMaxPlayers.Value,
            EnableExpandedLobby = OPConfig.LobbyEnableExpandedLobby.Value,
            AllowLateJoin = OPConfig.LobbyAllowLateJoin.Value,
            LateJoinMode = OPConfig.LobbyLateJoinMode.Value,
            EnableSpectatorMode = OPConfig.LobbyEnableSpectatorMode.Value,
            RequireMatchingOverseerVersion = OPConfig.LobbyRequireMatchingOverseerVersion.Value,
            RequireMatchingPreset = OPConfig.LobbyRequireMatchingPreset.Value,
            SyncPresetToClients = OPConfig.LobbySyncPresetToClients.Value,
            SyncOverridesToClients = OPConfig.LobbySyncOverridesToClients.Value
        };

        rules = Normalize(rules);
        OPLog.Info("LobbyRules", $"Loaded lobby rules from .cfg: maxPlayers={rules.MaxPlayers}, lateJoin={rules.AllowLateJoin}, mode={rules.LateJoinMode}");
        return rules;
    }

    private static LobbyRulesDefinition Normalize(LobbyRulesDefinition rules)
    {
        if (rules.SchemaVersion <= 0)
        {
            OPLog.Info("LobbyRules", $"Normalizing schemaVersion {rules.SchemaVersion} -> 1");
            rules.SchemaVersion = 1;
        }

        if (rules.MaxPlayers < 1)
        {
            OPLog.Warning("LobbyRules", $"maxPlayers={rules.MaxPlayers} is below 1. Clamping to 1.");
            rules.MaxPlayers = 1;
        }

        if (rules.MaxPlayers > 64)
        {
            OPLog.Warning("LobbyRules", $"maxPlayers={rules.MaxPlayers} is above 64. Clamping to 64.");
            rules.MaxPlayers = 64;
        }

        if (string.IsNullOrWhiteSpace(rules.LateJoinMode))
        {
            OPLog.Info("LobbyRules", "LateJoinMode was empty. Normalizing to Disabled.");
            rules.LateJoinMode = "Disabled";
        }

        rules.LateJoinMode = rules.LateJoinMode.Trim();

        return rules;
    }
}
