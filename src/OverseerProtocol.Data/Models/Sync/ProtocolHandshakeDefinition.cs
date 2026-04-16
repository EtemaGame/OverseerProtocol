using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Sync;

public sealed class ProtocolHandshakeDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public string OverseerVersion { get; set; } = "";
    public string GameVersion { get; set; } = "";
    public string ActivePreset { get; set; } = "default";
    public string PresetFingerprint { get; set; } = "";
    public string ConfigFingerprint { get; set; } = "";
    public LobbyHandshakeRules Rules { get; set; } = new();
    public List<string> EnabledFeatures { get; set; } = new();
}

public sealed class LobbyHandshakeRules
{
    public int MaxPlayers { get; set; } = 4;
    public bool AllowLateJoin { get; set; }
    public string LateJoinMode { get; set; } = "Disabled";
    public bool RequireMatchingOverseerVersion { get; set; } = true;
    public bool RequireMatchingPreset { get; set; } = true;
}

public sealed class HandshakeCompatibilityResult
{
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public bool IsCompatible => Errors.Count == 0;
}
