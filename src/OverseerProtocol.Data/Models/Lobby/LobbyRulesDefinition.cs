namespace OverseerProtocol.Data.Models.Lobby;

public sealed class LobbyRulesDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public int MaxPlayers { get; set; } = 4;
    public bool EnableExpandedLobby { get; set; }
    public bool AllowLateJoin { get; set; }
    public string LateJoinMode { get; set; } = "Disabled";
    public bool EnableSpectatorMode { get; set; }
    public bool RequireMatchingOverseerVersion { get; set; } = true;
    public bool RequireMatchingPreset { get; set; } = true;
    public bool SyncPresetToClients { get; set; } = true;
    public bool SyncOverridesToClients { get; set; }
}
