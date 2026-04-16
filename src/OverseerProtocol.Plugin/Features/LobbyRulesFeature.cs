using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Core.Paths;
using OverseerProtocol.Core.Serialization;
using OverseerProtocol.Data.Models.Lobby;

namespace OverseerProtocol.Features;

public sealed class LobbyRulesFeature
{
    public LobbyRulesDefinition LoadOrCreate()
    {
        var path = OPPaths.GetPresetLobbyRulesPath(OPConfig.ActivePresetName);
        var rules = JsonFileReader.Read<LobbyRulesDefinition>(path);

        if (rules == null)
        {
            rules = new LobbyRulesDefinition();
            JsonFileWriter.Write(path, rules);
            OPLog.Info("LobbyRules", $"Created lobby rules file at {path}");
        }

        rules = Normalize(rules);
        OPLog.Info("LobbyRules", $"Loaded lobby rules from {path}: maxPlayers={rules.MaxPlayers}, lateJoin={rules.AllowLateJoin}, mode={rules.LateJoinMode}");
        return rules;
    }

    private static LobbyRulesDefinition Normalize(LobbyRulesDefinition rules)
    {
        if (rules.SchemaVersion <= 0)
            rules.SchemaVersion = 1;

        if (rules.MaxPlayers < 1)
            rules.MaxPlayers = 1;

        if (rules.MaxPlayers > 64)
            rules.MaxPlayers = 64;

        if (string.IsNullOrWhiteSpace(rules.LateJoinMode))
            rules.LateJoinMode = "Disabled";

        rules.LateJoinMode = rules.LateJoinMode.Trim();

        return rules;
    }
}
