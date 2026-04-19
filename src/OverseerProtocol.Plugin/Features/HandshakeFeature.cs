using OverseerProtocol.Configuration;
using OverseerProtocol.Core.Logging;
using OverseerProtocol.Data.Models.Sync;

namespace OverseerProtocol.Features;

public sealed class HandshakeFeature
{
    public ProtocolHandshakeDefinition BuildCurrent()
    {
        var fingerprints = new FingerprintFeature().ComputeCurrent();
        var lobbyRules = new LobbyRulesFeature().LoadOrCreate();

        var handshake = new ProtocolHandshakeDefinition
        {
            OverseerVersion = global::OverseerProtocol.Plugin.ModVersion,
            ActivePreset = fingerprints.ActivePreset,
            PresetFingerprint = fingerprints.PresetFingerprint,
            ConfigFingerprint = fingerprints.ConfigFingerprint,
            Rules = new LobbyHandshakeRules
            {
                MaxPlayers = lobbyRules.MaxPlayers,
                AllowLateJoin = lobbyRules.AllowLateJoin,
                LateJoinMode = lobbyRules.LateJoinMode,
                RequireMatchingOverseerVersion = lobbyRules.RequireMatchingOverseerVersion,
                RequireMatchingPreset = lobbyRules.RequireMatchingPreset
            },
            EnabledFeatures =
            {
                OPConfig.EnableItemOverrides.Value ? "item-tuning" : "item-tuning:disabled",
                OPConfig.EnableSpawnOverrides.Value ? "spawn-tuning" : "spawn-tuning:disabled",
                OPConfig.EnableMoonOverrides.Value ? "moon-tuning" : "moon-tuning:disabled",
                OPConfig.EnableRuntimeMultipliers.Value ? "runtime-multipliers" : "runtime-multipliers:disabled",
                OPConfig.EnableProgressionStorage.Value ? "progression-storage" : "progression-storage:disabled",
                OPConfig.EnablePerkCatalog.Value ? "perk-catalog" : "perk-catalog:disabled",
                OPConfig.EnableMultiplayer.Value ? "multiplayer" : "multiplayer:disabled",
                OPConfig.RequireSameConfigHash.Value ? "config-hash-required" : "config-hash-optional"
            }
        };

        OPLog.Info(
            "Handshake",
            $"Built local handshake: version={handshake.OverseerVersion}, preset={handshake.ActivePreset}, presetFingerprint={handshake.PresetFingerprint}, configFingerprint={handshake.ConfigFingerprint}, maxPlayers={handshake.Rules.MaxPlayers}, lateJoin={handshake.Rules.AllowLateJoin}, mode={handshake.Rules.LateJoinMode}, featureCount={handshake.EnabledFeatures.Count}");

        foreach (var feature in handshake.EnabledFeatures)
            OPLog.Info("Handshake", $"Handshake feature flag: {feature}");

        return handshake;
    }
}
