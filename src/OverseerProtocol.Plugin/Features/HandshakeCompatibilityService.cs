using OverseerProtocol.Data.Models.Sync;

namespace OverseerProtocol.Features;

public sealed class HandshakeCompatibilityService
{
    public HandshakeCompatibilityResult Compare(ProtocolHandshakeDefinition host, ProtocolHandshakeDefinition client)
    {
        var result = new HandshakeCompatibilityResult();

        if (host == null || client == null)
        {
            result.Errors.Add("Host or client handshake is missing.");
            return result;
        }

        if (host.Rules.RequireMatchingOverseerVersion &&
            host.OverseerVersion != client.OverseerVersion)
        {
            result.Errors.Add($"OverseerProtocol version mismatch: host={host.OverseerVersion}, client={client.OverseerVersion}");
        }

        if (host.Rules.RequireMatchingPreset &&
            host.ActivePreset != client.ActivePreset)
        {
            result.Errors.Add($"Active preset mismatch: host={host.ActivePreset}, client={client.ActivePreset}");
        }

        if (host.Rules.RequireMatchingPreset &&
            host.PresetFingerprint != client.PresetFingerprint)
        {
            result.Errors.Add("Preset fingerprint mismatch.");
        }

        if (host.ConfigFingerprint != client.ConfigFingerprint)
            result.Warnings.Add("Config fingerprint mismatch.");

        if (host.Rules.MaxPlayers != client.Rules.MaxPlayers)
            result.Warnings.Add($"Max player rule mismatch: host={host.Rules.MaxPlayers}, client={client.Rules.MaxPlayers}");

        if (host.Rules.LateJoinMode != client.Rules.LateJoinMode)
            result.Warnings.Add($"Late join mode mismatch: host={host.Rules.LateJoinMode}, client={client.Rules.LateJoinMode}");

        return result;
    }
}
